using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        }

        public List<Result> Query(Query query)
        {
            var search = query.Search;

            if (!_settings.Snippets.Any())
                return _buildEmpty(query);

            // all data
            if (string.IsNullOrEmpty(search))
            {
                return _settings.Snippets.Select(r => _toResult(query, r))
                    .ToList();
            }

            // full match
            var fullMatch = _settings.Snippets.ContainsKey(search);
            if (fullMatch)
            {
                var value = _settings.Snippets[search];
                return new List<Result>
                {
                    _toResult(query, new KeyValuePair<string, string>(search, value))
                };
            }

            // fuzzy search
            var searchTerms = query.SearchTerms;
            if (searchTerms.Length < 2)
            {
                // only first 
                return _settings.Snippets
                    .Where(r =>
                    {
                        if (string.IsNullOrEmpty(search)) return true;
                        var mr = _context.API.FuzzySearch(search, r.Key);
                        return mr.Success;
                    })
                    .Select(r => _toResult(query, r))
                    .ToList();
            }

            // eq 2
            var firstSearchKey = query.FirstSearch;
            if (_settings.Snippets.TryGetValue(firstSearchKey, out var firstValue))
            {
                // update
                return new List<Result>
                {
                    _updateSnippets(query, firstSearchKey, query.SecondToEndSearch)
                };
            }

            var results = _settings.Snippets
                .Where(r =>
                {
                    if (string.IsNullOrEmpty(firstSearchKey)) return true;
                    var mr = _context.API.FuzzySearch(firstSearchKey, r.Key);
                    return mr.Success;
                })
                .Select(r => _toResult(query, r))
                .ToList();
            results.Add(_appendSnippets(query));
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


        private Result _appendSnippets(Query query)
        {
            var name = query.FirstSearch;
            var value = query.SecondToEndSearch;

            return new Result
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
            };
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