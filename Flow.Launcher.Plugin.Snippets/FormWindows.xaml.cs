using System.Collections.Generic;
using System.Windows;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FormWindows : Window
{
    private IPublicAPI _publicAPI;
    private Settings _settings;
    private KeyValuePair<string, string>? _editKvp;

    public FormWindows(IPublicAPI publicApi, Settings settings, KeyValuePair<string, string>? editKvp = null)
    {
        _publicAPI = publicApi;
        _settings = settings;
        _editKvp = editKvp;
        InitializeComponent();
    }
}