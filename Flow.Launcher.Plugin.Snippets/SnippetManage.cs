using System.Collections.Generic;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public interface SnippetManage
{
    [CanBeNull]
    SnippetModel GetByKey(string key);

    List<SnippetModel> List([CanBeNull] string key = null, [CanBeNull] string value = null);

    bool Add(SnippetModel sm);

    bool RemoveByKey(string key);

    bool UpdateByKey(SnippetModel sm);

    void Clear();

    void ResetAllScore();

    void Close()
    {
    }
}