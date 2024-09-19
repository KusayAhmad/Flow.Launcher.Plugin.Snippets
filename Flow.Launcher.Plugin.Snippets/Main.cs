using System.Collections.Generic;
using System.IO;
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
            var searchTerms = query.SearchTerms;
            string searchKey = null;
            var searchTermsLength = searchTerms.Length;
            if (searchTermsLength > 0)
                searchKey = searchTerms[0];

            var results = _settings.Snippets
                .Where(r =>
                {
                    if (string.IsNullOrEmpty(searchKey)) return true;
                    var mr = _context.API.FuzzySearch(searchKey, r.Key);
                    return mr.Success;
                })
                .Select(r =>
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
                })
                .ToList();

            if (results.Any())
                return results;
            if (searchTermsLength < 2)
                return results;

            return new List<Result>
            {
                _appendSnippets(query, searchTerms)
            };
        }


        private Result _appendSnippets(Query query, string[] searchTerms)
        {
            var name = searchTerms[0];
            var value = searchTerms[1];
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

        private void _add(string name, string value)
        {
            _settings.Snippets.Add(name, value);
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
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            Topmost = true
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
                    }
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
    }
}