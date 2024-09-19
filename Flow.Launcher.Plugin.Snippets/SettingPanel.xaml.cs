using System.Windows.Controls;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingPanel : UserControl
{
    public SettingPanel()
    {
        InitializeComponent();
    }

    public SettingPanel(IPublicAPI contextApi, Settings settings)
    {
    }
}