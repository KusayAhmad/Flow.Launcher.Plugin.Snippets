using System;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class FileUtil
{
    private static string _dataDirectory = null;

    public static string GetDataDirectory(bool reflection, Func<string> action)
    {
        if (_dataDirectory != null)
            return _dataDirectory;

        string flowDir = null;

        if (reflection)
        {
            try
            {
                // reflection to get DataLocation.DataDirectory
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Flow.Launcher.Infrastructure");

                if (assembly != null)
                {
                    var dataLocationType = assembly.GetType("Flow.Launcher.Infrastructure.UserSettings.DataLocation");

                    if (dataLocationType != null)
                    {
                        var method = dataLocationType.GetMethod("DataDirectory");
                        if (method != null)
                        {
                            var dataDir = method.Invoke(null, null) as string;
                            if (!string.IsNullOrEmpty(dataDir))
                            {
                                flowDir = dataDir;
                            }
                        }
                    }
                }
            }
            catch (Exception _)
            {
                // ignored
            }
        }

        if (string.IsNullOrEmpty(flowDir))
        {
            // default: C:\Users\<username>\AppData\Roaming\FlowLauncher
            flowDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlowLauncher");
        }

        //  <flowDir>\Settings\Plugins\<pluginName>
        _dataDirectory = Path.Combine(flowDir, "Settings", "Plugins", action());
        return _dataDirectory;
    }
}