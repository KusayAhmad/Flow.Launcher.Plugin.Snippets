using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
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

            // Add results with variable support and enhanced scoring
            foreach (var snippet in snippets)
            {
                if (VariableHelper.HasVariables(snippet.Value))
                {
                    // Snippet contains variables
                    var variableInfo = VariableHelper.GetVariableInfo(snippet.Value, variables);
                    int variableCount = variableInfo.RequiredVariables.Count;
                    bool allProvided = variableInfo.HasAllRequiredVariables;
                    
                    // Calculate enhanced score
                    int enhancedScore = ScoreCalculator.CalculateScore(
                        snippet, 
                        baseKeyword, 
                        hasVariables: true,
                        variableCount: variableCount,
                        allVariablesProvided: allProvided
                    );
                    
                    if (allProvided)
                    {
                        // All variables available - create final result
                        var processedValue = VariableHelper.ReplaceVariables(snippet.Value, variables);
                        results.Add(_createVariableResult(query, snippet, processedValue, true, enhancedScore));
                    }
                    else
                    {
                        // Missing variables - show help
                        results.Add(_createVariableHelpResult(query, snippet, variableInfo, enhancedScore));
                    }
                }
                else
                {
                    // Regular snippet without variables - calculate enhanced score
                    int enhancedScore = ScoreCalculator.CalculateScore(snippet, baseKeyword);
                    results.Add(_modelToResult(query, snippet, enhancedScore));
                }
            }

            // Add option to create new snippet if no results found
            if (!results.Any() && query.SearchTerms.Length >= 2)
            {
                _appendSnippets(query, results);
            }

            return results;
        }

        private Result _modelToResult(Query query, SnippetModel sm, int? enhancedScore = null)
        {
            var key = sm.Key ?? string.Empty;
            var value = sm.Value ?? string.Empty;
            var scoreInfo = _buildScoreInfo(sm, enhancedScore);
            return new Result
            {
                Title = key + (sm.IsFavorite ? " ⭐" : ""),
                SubTitle = $"{value.Replace("\r\n", "  ").Replace("\n", "  ")} {scoreInfo}",
                IcoPath = IconPath,
                Score = enhancedScore ?? sm.Score,
                AutoCompleteText = $"{query.ActionKeyword} {key}",
                ContextData = sm,
                Preview = new Result.PreviewInfo
                {
                    Description = value,
                    PreviewImagePath = IconPath
                },
                Action = _ =>
                {
                    try
                    {
                        // copy to clipboard first
                        _context.API.CopyToClipboard(value, showDefaultNotification: false);

                        // Update usage statistics
                        _updateUsageStats(sm);

                        // after Flow Launcher hides, wait until Flow Launcher no longer has focus and paste into previous active window
                        if (_settings.AutoPasteEnabled)
                        {
                            Task.Run(() => PasteWhenFocusRestoredAsync(_context, _settings.PasteDelayMs));
                        }
                    }
                    catch (Exception ex)
                    {
                         InnerLogger.Logger.Error("Snippets Action", ex);
                    }

                    return true;
                }
            };
        }

        private Result _createVariableResult(Query query, SnippetModel sm, string processedValue, bool isProcessed, int enhancedScore)
        {
            var scoreInfo = _buildScoreInfo(sm, enhancedScore);
            return new Result
            {
                Title = sm.Key + (isProcessed ? " ✓" : "") + (sm.IsFavorite ? " ⭐" : ""),
                SubTitle = $"{processedValue.Replace("\r\n", "  ").Replace("\n", "  ")} {scoreInfo}",
                IcoPath = IconPath,
                Score = enhancedScore,
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
                    _updateUsageStats(sm);
                    return true;
                }
            };
        }

        private Result _createVariableHelpResult(Query query, SnippetModel sm, VariableInfo variableInfo, int enhancedScore)
        {
            var missingVars = string.Join(", ", variableInfo.MissingVariables);
            var example = string.Join(" ", variableInfo.MissingVariables.Select(v => $"{v}="));
            
            return new Result
            {
                Title = $"{sm.Key} (requires variables)" + (sm.IsFavorite ? " ⭐" : ""),
                SubTitle = $"Required variables: {missingVars}. Example: {query.ActionKeyword} {sm.Key} {example}",
                IcoPath = IconPath,
                Score = enhancedScore,
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

                // Toggle favorite
                menus.Add(new Result
                {
                    Title = sm.IsFavorite ? "Remove from Favorites ⭐" : "Add to Favorites ☆",
                    SubTitle = sm.IsFavorite 
                        ? "Remove this snippet from your favorites" 
                        : "Mark this snippet as favorite for higher priority",
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        sm.IsFavorite = !sm.IsFavorite;
                        _snippetManage.UpdateByKey(sm);
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

        /// <summary>
        /// Build score information string for display - compact format
        /// </summary>
        private string _buildScoreInfo(SnippetModel sm, int? calculatedScore)
        {
            var parts = new List<string>();
            
            // Show score (compact: ⚡score)
            if (calculatedScore.HasValue)
            {
                parts.Add($"⚡{calculatedScore.Value}");
            }
            else if (sm.Score > 0)
            {
                parts.Add($"⚡{sm.Score}");
            }
            
            // Show usage count (compact: ↻count)
            if (sm.UsageCount > 0)
            {
                parts.Add($"↻{sm.UsageCount}");
            }
            
            // Show last used time (compact: 🕐time)
            if (sm.LastUsedTime.HasValue)
            {
                var timeSince = DateTime.Now - sm.LastUsedTime.Value;
                string timeText;
                if (timeSince.TotalHours < 1)
                {
                    timeText = $"{(int)timeSince.TotalMinutes}m";
                }
                else if (timeSince.TotalDays < 1)
                {
                    timeText = $"{(int)timeSince.TotalHours}h";
                }
                else if (timeSince.TotalDays < 7)
                {
                    timeText = $"{(int)timeSince.TotalDays}d";
                }
                else if (timeSince.TotalDays < 30)
                {
                    timeText = $"{(int)(timeSince.TotalDays / 7)}w";
                }
                else
                {
                    timeText = $"{(int)(timeSince.TotalDays / 30)}mo";
                }
                parts.Add($"🕐{timeText}");
            }
            
            return parts.Count > 0 ? string.Join(" ", parts) : "";
        }

        /// <summary>
        /// Update usage statistics when a snippet is used
        /// </summary>
        private void _updateUsageStats(SnippetModel snippet)
        {
            snippet.UsageCount++;
            snippet.LastUsedTime = DateTime.Now;
            _snippetManage.UpdateByKey(snippet);
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

        
        // P/Invoke helpers to simulate Ctrl+V keypress and check foreground window
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;

        private static void SendCtrlV()
        {
            try
            {
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                InnerLogger.Logger.Error("SendCtrlV failed", ex);
            }
        }

        private static async Task PasteWhenFocusRestoredAsync(PluginInitContext context, int extraDelayMs = 50)
         {
            try
            {
              // Wait until Flow Launcher main window is no longer visible
              const int timeoutMs = 2000; // max wait time for focus to switch
              const int intervalMs = 100;
              var waited = 0;
                
              while (waited < timeoutMs && context.API.IsMainWindowVisible())
               {
                 await Task.Delay(intervalMs).ConfigureAwait(false);
                 waited += intervalMs;
                }

            // small extra delay to ensure target window is ready to accept input
            await Task.Delay(extraDelayMs).ConfigureAwait(false);
            SendCtrlV();
            
            }
             catch (Exception ex)
              {
                 InnerLogger.Logger.Error("Snippets Paste", ex);
               
                 // At minimum, the snippet is already in clipboard
                 // Optionally show a notification that auto-paste failed
             }
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