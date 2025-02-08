using System;
using System.Collections.ObjectModel;
using Flow.Launcher.Plugin.Snippets.Json;
using Flow.Launcher.Plugin.Snippets.Sqlite;

namespace Flow.Launcher.Plugin.Snippets;

public class Main_Test
{
    //TEST 
    private static string dbPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\snippets.db";

    public static void Main()
    {
        // test_json_settings();
        // test_sqlite_add();
        test_sqlite_query();
    }

    private static void test_sqlite_query()
    {
        var sm = new SqliteSnippetManage(dbPath);

        var snippetModels = sm.List(key: "key1");
        foreach (var snippetModel in snippetModels)
        {
            Console.WriteLine(snippetModel + " - " + snippetModel.UpdateTime);
        }
    }

    private static void test_sqlite_add()
    {
        var sm = new SqliteSnippetManage(dbPath);

        sm.Add(new SnippetModel
        {
            Key = "key1",
            Value = "value1"
        });
        sm.Add(new SnippetModel
        {
            Key = "key2",
            Value = "value2"
        });
    }

    private static void test_json_settings()
    {
        var snippets = new ObservableCollection<SnippetModel>();

        snippets.Add(new SnippetModel
        {
            Key = "key1",
            Value = "value1"
        });

        snippets.Add(new SnippetModel
        {
            Key = "key2",
            Value = "value2"
        });

        snippets.Add(new SnippetModel
        {
            Key = "key3",
            Value = "value3"
        });

        var sm = new JsonSettingSnippetManage(null);
        var v1 = sm.GetByKey("key1");
        Console.WriteLine(v1 == null);
        Console.WriteLine(v1);
    }
}