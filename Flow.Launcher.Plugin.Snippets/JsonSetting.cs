using System.Collections.ObjectModel;

namespace Flow.Launcher.Plugin.Snippets;

public class JsonSetting
{
    public ObservableCollection<SnippetModel> SnippetList { get; set; } = new();
}