using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.Snippets
{
    public class Snippets : IPlugin, IPluginI18n, IContextMenu, ISettingProvider
    {
        public static readonly string IconPath = "Images\\Snippets.png";

        private PluginInitContext _context;
        private Settings _settings;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            // InnerLogger.SetAsFlowLauncherLogger(context.API, LoggerLevel.TRACE);
        }

        public List<Result> Query(Query query)
        {
            var search = query.Search;

            // InnerLogger.Logger.Trace($"Search: {search}");

            // if (!_settings.Snippets.Any())
            //     return _buildEmpty(query);

            // all data
            if (string.IsNullOrEmpty(search))
            {
                return _settings.Snippets.Select(r => _toResult(query, r))
                    .ToList();
            }

            // full match
            // if (_settings.Snippets.TryGetValue(search, out var fullMatch))
            // {
            //     return new List<Result>
            //     {
            //         _toResult(query, new KeyValuePair<string, string>(search, fullMatch))
            //     };
            // }

            // fuzzy search

            var results = new List<Result>();
            foreach (var kvp in _settings.Snippets)
            {
                Result result;
                if (string.IsNullOrEmpty(search))
                    result = _toResult(query, kvp);
                else
                {
                    var mr = _context.API.FuzzySearch(search, kvp.Key);
                    if (mr.Success)
                    {
                        result = _toResult(query, kvp);
                        result.Score = mr.Score;
                    }
                    else
                        continue;
                }

                results.Add(result);
            }

            // InnerLogger.Logger.Debug($"FuzzySearch: {search}. results.size: {results.Count}");

            if (!results.Any() && query.SearchTerms.Length >= 2)
            {
                _appendSnippets(query, results);
            }

            return results;
        }

        private Result _toResult(Query query, KeyValuePair<string, string> r)
        {
            return new Result
            {
                Title = r.Key,
                SubTitle = r.Value,
                IcoPath = IconPath,
                AutoCompleteText = $"{query.ActionKeyword} {r.Key}",
                ContextData = r,
                Preview = new Result.PreviewInfo
                {
                    Description = r.Value,
                    PreviewImagePath = IconPath
                },
                Action = _ =>
                {
                    _context.API.CopyToClipboard(r.Value, showDefaultNotification: false);
                    return true;
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

        private void _add(string name, string value)
        {
            _settings.Snippets.Add(name, value);
            _context.API.SavePluginSettings();
        }

        private void _update(string name, string value)
        {
            _settings.Snippets[name] = value;
            _context.API.SavePluginSettings();
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var menus = new List<Result>();
            var contextData = selectedResult.ContextData;
            if (contextData is KeyValuePair<string, string> kvp)
            {
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_edit_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_edit_snippet_info"),
                        kvp.Key, kvp.Value),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        var fw = new FormWindows(_context.API, _settings, kvp)
                        {
                            // Title = _context.API.GetTranslation("snippets_plugin_manage_snippets"),
                            // WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            Topmost = true,
                            // WindowState = WindowState.Normal,
                            // ResizeMode = ResizeMode.NoResize,
                            // ShowInTaskbar = false
                        };
                        fw.ShowDialog();
                        return true;
                    }
                });
                menus.Add(new Result
                {
                    Title = _context.API.GetTranslation("snippets_plugin_delete_snippet"),
                    SubTitle = string.Format(_context.API.GetTranslation("snippets_plugin_delete_snippet_info"),
                        kvp.Key, kvp.Value),
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        _settings.Snippets.Remove(kvp.Key);
                        _context.API.SavePluginSettings();
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
            return new SettingPanel(_context.API, _settings);
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
    }
}