using System;

namespace Flow.Launcher.Plugin.Snippets;

public class Main_Test
{
    public static void Main()
    {
        var s = new Settings();
        Console.WriteLine(s.GetType().Assembly.GetName().Name);
    }

    
}