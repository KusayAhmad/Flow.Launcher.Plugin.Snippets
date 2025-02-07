using System;
using System.Collections.Generic;
using System.Text;

namespace Flow.Launcher.Plugin.Snippets;

public class Main_Test
{
    public static void Main()
    {
        string[] terms = { "s1", "s2", "s3",  }; // 示例数组
        int length = terms.Length;

        if (length < 2)
        {
            Console.WriteLine("数组长度至少为2");
            return;
        }

        StringBuilder result = new StringBuilder();

        for (int i = 1; i < length; i++)
        {
            string left = string.Join("", terms, 0, i);
            string right = string.Join("", terms, i, length - i);
            result.AppendLine($"({left}, {right})");
        }

        Console.WriteLine(result.ToString());
    }
}