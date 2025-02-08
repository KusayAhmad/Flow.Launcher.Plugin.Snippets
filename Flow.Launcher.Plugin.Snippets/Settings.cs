using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Flow.Launcher.Plugin.Snippets;

public class Settings : BaseModel
{
    public StorageType StorageType { get; set; } = StorageType.Sqlite;
    public Dictionary<string, string> Snippets { get; set; } = new();
}

public enum StorageType
{
    JsonSetting,
    Sqlite
}