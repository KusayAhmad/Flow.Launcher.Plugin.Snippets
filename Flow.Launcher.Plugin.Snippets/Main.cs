using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.Snippets
{
    public class Snippets : IPlugin, IPluginI18n
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
                        Action = c =>
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

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("snippets_plugin_title");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("snippets_plugin_description");
        }
    }
}