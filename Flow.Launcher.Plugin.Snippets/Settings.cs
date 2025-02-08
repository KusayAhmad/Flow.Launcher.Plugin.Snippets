namespace Flow.Launcher.Plugin.Snippets;

public class Settings : BaseModel
{
    public StorageType StorageType { get; set; } = StorageType.Sqlite;
}

public enum StorageType
{
    JsonSetting,
    Sqlite
}