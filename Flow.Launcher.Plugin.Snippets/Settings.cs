using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Snippets;

public class Settings : BaseModel
{
    public Dictionary<string, string> Snippets { get; set; } = new();
}