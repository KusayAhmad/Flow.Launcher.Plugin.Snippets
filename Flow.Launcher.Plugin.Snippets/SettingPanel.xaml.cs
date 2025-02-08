using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SettingPanel : UserControl
{
    private IPublicAPI _publicApi;
    private Settings _settings;
    private SnippetManage _snippetManage;

    public SettingPanel(IPublicAPI contextApi, Settings settings, SnippetManage snippetManage)
    {
        _publicApi = contextApi;
        _settings = settings;
        _snippetManage = snippetManage;
        InitializeComponent();


        if (_settings.StorageType == StorageType.Sqlite)
        {
            RadioButtonSqlite.IsChecked = true;
        }
        else
        {
            RadioButtonJsonSetting.IsChecked = true;
        }
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

    private void RadioButtonSqlite_OnChecked(object sender, RoutedEventArgs e)
    {
        _settings.StorageType = StorageType.Sqlite;
        _publicApi.SavePluginSettings();
    }

    private void RadioButtonJsonSetting_OnChecked(object sender, RoutedEventArgs e)
    {
        _settings.StorageType = StorageType.JsonSetting;
        _publicApi.SavePluginSettings();
    }

    private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
    {
        _snippetManage.Clear();
    }

    private void ButtonImport_OnClick(object sender, RoutedEventArgs e)
    {
        // select .json file
        var dialog = new OpenFileDialog
        {
            Filter = "Json file (*.json)|*.json",
            Title = _publicApi.GetTranslation("snippets_plugin_select_json_file"),
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            var file = dialog.FileName;
            if (File.Exists(file))
            {
                // read file
                var json = File.ReadAllText(file);
                _snippetManage.Clear();
            }
        }
    }

    private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
    {
        var list = _snippetManage.List();
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        _publicApi.RestartApp();
    }
}