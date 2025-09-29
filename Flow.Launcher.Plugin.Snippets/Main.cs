using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Snippets.Json;
using Flow.Launcher.Plugin.Snippets.Sqlite;
using Flow.Launcher.Plugin.Snippets.Util;
using static Flow.Launcher.Plugin.Snippets.Util.VariableHelper;

namespace Flow.Launcher.Plugin.Snippets
{
    public class Snippets : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IDisposable
    {
        public static readonly string IconPath = "Images\\Snippet.png";

        private PluginInitContext _context;
        private Settings _settings;
        private SnippetManage _snippetManage;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            InnerLogger.SetAsFlowLauncherLogger(_context.API, LoggerLevel.TRACE);

            if (_settings.StorageType == StorageType.Sqlite)
            {
                _snippetManage =
                    new SqliteSnippetManage(context.CurrentPluginMetadata.PluginSettingsDirectoryPath);
            }
            else
            {
                _snippetManage = new JsonSettingSnippetManage(context);
                _mergeOldSnippet(); // merge old snippets
            }
        }

        public List<Result> Query(Query query)
        {
            var search = query.Search;

            // all data
            if (string.IsNullOrEmpty(search))
            {
                return _snippetManage.List().Select(sm => _modelToResult(query, sm)).ToList();
            }

            // Parse variable arguments
            var queryParts = query.SearchTerms;
            var baseKeyword = queryParts.Length > 0 ? queryParts[0] : "";
            var variables = VariableHelper.ParseVariableArguments(queryParts, 1);

            // Search in snippets
            var snippets = _snippetManage.List(key: baseKeyword);
            var results = new List<Result>();

            // Add results with variable support
            foreach (var snippet in snippets)
            {
                if (VariableHelper.HasVariables(snippet.Value))
                {
                    // Snippet contains variables
                    var variableInfo = VariableHelper.GetVariableInfo(snippet.Value, variables);
                    
                    if (variableInfo.HasAllRequiredVariables)
                    {
                        // All variables available - create final result
                        var processedValue = VariableHelper.ReplaceVariables(snippet.Value, variables);
                        results.Add(_createVariableResult(query, snippet, processedValue, true));
                    }
                    else
                    {
                        // Missing variables - show help
                        results.Add(_createVariableHelpResult(query, snippet, variableInfo));
                    }
                }
                else
                {
                    // Regular snippet without variables
                    results.Add(_modelToResult(query, snippet));
                }
            }

            // Add option to create new snippet if no results found
            if (!results.Any() && query.SearchTerms.Length >= 2)
            {
                _appendSnippets(query, results);
            }

            return results;
        }

        private Result _modelToResult(Query query, SnippetModel sm)
        {
            return new Result
            {
                Title = sm.Key,
                SubTitle = sm.Value.Replace("\r\n", "  ").Replace("\n", "  "),
                IcoPath = IconPath,
                Score = sm.Score,
                AutoCompleteText = $"{query.ActionKeyword} {sm.Key}",
                ContextData = sm,
                Preview = new Result.PreviewInfo
                {
                    Description = sm.Value,
                    PreviewImagePath = IconPath
                },
                Action = _ =>
                {
                    _context.API.CopyToClipboard(sm.Value, showDefaultNotification: false);
                    return true;
                }
            };
        }

        private Result _createVariableResult(Query query, SnippetModel sm, string processedValue, bool isProcessed)
        {
            return new Result
            {
                Title = sm.Key + (isProcessed ? " ✓" : ""),
                SubTitle = processedValue.Replace("\r\n", "  ").Replace("\n", "  "),
                IcoPath = IconPath,
                Score = sm.Score + (isProcessed ? 10 : 0), // Higher priority for processed results
                AutoCompleteText = $"{query.ActionKeyword} {sm.Key}",
                ContextData = sm,
                Preview = new Result.PreviewInfo
                {
                    Description = processedValue,
                    PreviewImagePath = IconPath
                },
                Action = _ =>
                {
                    _context.API.CopyToClipboard(processedValue, showDefaultNotification: false);
                    return true;
                }
            };
        }

        private Result _createVariableHelpResult(Query query, SnippetModel sm, VariableInfo variableInfo)
        {
            var missingVars = string.Join(", ", variableInfo.MissingVariables);
            var example = string.Join(" ", variableInfo.MissingVariables.Select(v => $"{v}="));
            
            return new Result
            {
                Title = $"{sm.Key} (requires variables)",
                SubTitle = $"Required variables: {missingVars}. Example: {query.ActionKeyword} {sm.Key} {example}",
                IcoPath = IconPath,
                Score = sm.Score - 5, // Lower priority than completed results
                AutoCompleteText = $"{query.ActionKeyword} {sm.Key} {example}",
                ContextData = sm,
                Preview = new Result.PreviewInfo
                {
                    Description = $"Template: {sm.Value}\n\nRequired variables: {missingVars}\n\nExample: {query.ActionKeyword} {sm.Key} {example}",
                    PreviewImagePath = IconPath
                },
                Action = _ =>
                {
                    _context.API.ChangeQuery($"{query.ActionKeyword} {sm.Key} {example}", true);
                    return false;
                }
            };
        }

        private void _appendSnippets(Query query, List<Result> results)
        {
            var terms = query.SearchTerms;
            var length = terms.Length;

            for (var i = 1; i < length; i++)
            {
                var name = string.Join(" ", terms, 0, i);
                var value = string.Join(" ", terms, i, length - i);

                results.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_add"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_add_info"), name, value),
                    IcoPath = IconPath,
                    Action = c =>
                    {
                        _add(name, value);
                        _context.API.ChangeQuery($"{query.ActionKeyword} {name}", true);
                        return false;
                    }
                });
            }
        }

        private Result _updateSnippets(Query query, string name, string value)
        {
            return new Result
            {
                Title = _context.API.GetTranslation("snippets_plugin_update"),
                SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_update_info"), name, value),
                IcoPath = IconPath,
                Action = c =>
                {
                    _update(name, value);
                    _context.API.ChangeQuery($"{query.ActionKeyword} {name}", true);
                    return false;
                }
            };
        }

        private void _add(string key, string value)
        {
            _snippetManage.Add(new SnippetModel
            {
                Key = key,
                Value = value
            });
        }

        private void _update(string key, string value)
        {
            _snippetManage.UpdateByKey(new SnippetModel
            {
                Key = key,
                Value = value
            });
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var menus = new List<Result>();
            var contextData = selectedResult.ContextData;
            if (contextData is SnippetModel sm)
            {
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        sm.Key, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        FormWindows.ShowWindows(_context.API, _snippetManage, sm);
                        return true;
                    }
                });
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_delete_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_delete_snippet_info"),
                        sm.Key, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        _snippetManage.RemoveByKey(sm.Key);
                        return true;
                    },
                });

                // new edit
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        sm.Key, sm.Value.Replace("\r\n", "  ").Replace("\n", "  ")),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        SnippetDialog.ShowDialog(_context.API, _snippetManage, sm);
                        return true;
                    }
                });

                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_add"),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        SnippetDialog.ShowDialog(_context.API, _snippetManage);
                        return true;
                    },
                });
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_manage_snippets"),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        FormWindows.ShowWindows(_context.API, _snippetManage);
                        return true;
                    },
                });
            }

            return menus;
        }


        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("snippets_plugin_title");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("snippets_plugin_description");
        }

        public Control CreateSettingPanel()
        {
            return new SettingPanel(_context.API, _settings, _snippetManage);
        }

        public void Dispose()
        {
            _snippetManage.Close();
        }

        private List<Result> _buildEmpty(Query query)
        {
            return new List<Result>
            {
                new()
                {
                    Title = _context.API.GetTranslation("snippets_plugin_snippets_empty"),
                    SubTitle = _context.API.GetTranslation("snippets_plugin_snippets_empty_add"),
                    IcoPath = IconPath,
                    AutoCompleteText = $"{query.ActionKeyword} "
                }
            };
        }


        /// <summary>
        /// 1.x.x version snippets merge to 2.x.x version
        /// </summary>
        [Obsolete]
        private void _mergeOldSnippet()
        {
            // old version snippets in Settings.json
            var snippets = _settings.Snippets;
            if (snippets == null || !snippets.Any()) return;

            foreach (var snippet in snippets)
            {
                _snippetManage.Add(new SnippetModel
                {
                    Key = snippet.Key,
                    Value = snippet.Value
                });
            }

            // clear old snippets after merge
            _settings.Snippets = null;
            _context.API.SavePluginSettings();
        }
    }
}