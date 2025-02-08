using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingPanel : UserControl
{
    private IPublicAPI _publicApi;
    private SnippetManage _snippetManage;

    public SettingPanel(IPublicAPI contextApi, SnippetManage snippetManage)
    {
        _publicApi = contextApi;
        _snippetManage = snippetManage;
        InitializeComponent();
    }

    private void ButtonOpenManage_OnClick(object sender, RoutedEventArgs e)
    {
        var fw = new FormWindows(_publicApi, _snippetManage)
        {
            // Title = _publicApi.GetTranslation("snippets_plugin_manage_snippets"),
            // WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            // WindowState = WindowState.Normal,
            // ResizeMode = ResizeMode.NoResize,
            // ShowInTaskbar = false
        };
        fw.ShowDialog();
    }
}