using System;

namespace Flow.Launcher.Plugin.Snippets;

public class SnippetModel
{
    public string Key { get; set; }
    public string Value { get; set; }
    public int Score { get; set; } = 0;

    public DateTime? UpdateTime { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}, Score: {Score}";
    }
}