using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Snippets;

public class Main_Test
{
    public static void Main()
    {
        var list = new List<string>()
        {
            "aaa"
        };
        Console.WriteLine(string.Join(" ", list) + ".");
        var s = new Settings();
        Console.WriteLine(s.GetType().Assembly.GetName().Name);
    }
}