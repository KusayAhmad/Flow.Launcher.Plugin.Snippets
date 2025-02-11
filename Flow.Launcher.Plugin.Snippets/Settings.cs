using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Snippets;

public class Settings : BaseModel
{
    public StorageType StorageType { get; set; } = StorageType.JsonSetting;

    /// <summary>
    /// 1.x.x version snippets
    /// </summary>
    [Obsolete]
    public Dictionary<string, string> Snippets { get; set; }
}

public enum StorageType
{
    JsonSetting,
    Sqlite
}