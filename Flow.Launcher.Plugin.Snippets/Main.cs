using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Snippets
{
    public class Snippets : IPlugin
    {
        public static readonly string IconPath = "Images\\Snippets.png";

        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
            // context.API.LogInfo("INIT");
        }

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }
    }
}