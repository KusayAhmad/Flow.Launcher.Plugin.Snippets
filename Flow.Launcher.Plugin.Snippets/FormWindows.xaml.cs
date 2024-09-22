using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FormWindows : Window
{
    private IPublicAPI _publicAPI;
    private Settings _settings;
    private int _editIndex = -1;

    public ObservableCollection<KeyValuePair<string, string>> KeyValuePairs { get; set; }

    public FormWindows(IPublicAPI publicApi, Settings settings, KeyValuePair<string, string>? selectKvp = null)
    {
        _publicAPI = publicApi;
        _settings = settings;

        // 将 Dictionary 转换为 ObservableCollection
        KeyValuePairs = new ObservableCollection<KeyValuePair<string, string>>(_settings.Snippets);
        _editIndex = FindEditIndex(selectKvp);

        InitializeComponent();

        PreviewKeyDown += (sender, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };

        BtnClose.Content = _publicAPI.GetTranslation("snippets_plugin_close");
        BtnSave.Content = _publicAPI.GetTranslation("snippets_plugin_save");

        _renderSelect();
        DataContext = this;
        if (_editIndex != -1)
        {
            DataGrid.SelectedIndex = _editIndex;
        }

        AddContextMenu();
    }

    private void AddContextMenu()
    {
        var deleteItem = new MenuItem
        {
            Header = _publicAPI.GetTranslation("snippets_plugin_delete_snippet")
        };
        deleteItem.Click += (o, args) =>
        {
            var selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex == -1) return;
            if (selectedIndex > KeyValuePairs.Count) return;

            var kvp = KeyValuePairs[selectedIndex];
            KeyValuePairs.RemoveAt(selectedIndex);

            _settings.Snippets.Remove(kvp.Key);
            _publicAPI.SavePluginSettings();
        };
        DataGrid.ContextMenu = new ContextMenu
        {
            Items =
            {
                deleteItem,
            }
        };
    }

    private int FindEditIndex(KeyValuePair<string, string>? selectKvp)
    {
        if (selectKvp == null) return -1;

        var vp = selectKvp.Value;

        for (var i = 0; i < KeyValuePairs.Count; i++)
        {
            var keyValuePair = KeyValuePairs[i];
            if (keyValuePair.Key == vp.Key)
            {
                return i;
            }
        }

        return -1;
    }

    private void _renderSelect()
    {
        if (_editIndex != -1)
        {
            // update
            BtnSwitch.Content = _publicAPI.GetTranslation("snippets_plugin_edit_item_key");

            var keyValuePair = KeyValuePairs[_editIndex];
            TbKey.Text = keyValuePair.Key;
            TbValue.Text = keyValuePair.Value;
        }
        else
        {
            // add
            BtnSwitch.Content = _publicAPI.GetTranslation("snippets_plugin_add_item_key");

            TbKey.Text = "";
            TbValue.Text = "";
        }
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }


    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        var key = TbKey.Text.Trim();
        var value = TbValue.Text.Trim();
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return;
        }

        if (_editIndex == -1)
        {
            // ADD
            KeyValuePairs.Add(new KeyValuePair<string, string>(key, value));
            _settings.Snippets[key] = value;
            _publicAPI.SavePluginSettings();

            TbKey.Text = "";
            TbValue.Text = "";
        }
        else
        {
            // update 
            var oldKvp = KeyValuePairs[_editIndex];

            if (string.Equals(key, oldKvp.Key))
            {
                // just update value 
                _settings.Snippets[key] = value;
            }
            else
            {
                // update key and value
                _settings.Snippets.Remove(oldKvp.Key);
                _settings.Snippets[key] = value;
            }

            KeyValuePairs[_editIndex] = new KeyValuePair<string, string>(key, value);
            _publicAPI.SavePluginSettings();
        }
    }

    private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            var selectedIndex = dataGrid.SelectedIndex;

            if (selectedIndex >= 0 && selectedIndex < dataGrid.Items.Count)
            {
                _editIndex = selectedIndex;
                _renderSelect();
            }
            else
            {
                _editIndex = -1;
                _renderSelect();
            }
        }
    }

    private void ButtonSwitch_OnClick(object sender, RoutedEventArgs e)
    {
        _editIndex = -1;
        _renderSelect();
        DataGrid.UnselectAll();
    }
}