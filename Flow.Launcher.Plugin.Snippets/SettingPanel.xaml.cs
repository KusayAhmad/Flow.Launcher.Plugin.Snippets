using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingPanel : UserControl
{
    private IPublicAPI _publicApi;
    private Settings _settings;

    public SettingPanel(IPublicAPI contextApi, Settings settings)
    {
        _publicApi = contextApi;
        _settings = settings;
        InitializeComponent();
    }

    private void ButtonOpenManage_OnClick(object sender, RoutedEventArgs e)
    {
        var fw = new FormWindows(_publicApi, _settings)
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