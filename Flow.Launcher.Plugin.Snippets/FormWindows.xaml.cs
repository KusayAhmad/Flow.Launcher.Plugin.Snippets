using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FormWindows : Window
{
    private IPublicAPI _publicAPI;
    private SnippetManage _snippetManage;
    private int _editIndex = -1;
    private DataTable _dataTable;

    public FormWindows(IPublicAPI publicApi,
        SnippetManage snippetManage,
        SnippetModel? selectKvp = null)
    {
        _publicAPI = publicApi;
        _snippetManage = snippetManage;


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

        ComboBoxFilterType.SelectedIndex = 0; // default key

        _dataTable = new DataTable();
        _dataTable.Columns.Add();
        _dataTable.Columns.Add();
        DataGrid.ItemsSource = _dataTable.DefaultView;

        _loadData();

        // _renderSelect();
        // DataContext = this;
        // if (_editIndex != -1)
        // {
        //     DataGrid.SelectedIndex = _editIndex;
        // }
        //
        // AddContextMenu();
    }

    private void _loadData()
    {
        _dataTable.Clear();

        List<SnippetModel> snippets;

        var filter = TbFilter.Text.Trim();
        if (string.IsNullOrEmpty(filter))
        {
            snippets = _snippetManage.List();
        }
        else
        {
            if (ComboBoxFilterType.SelectedIndex == 1)
            {
                snippets = _snippetManage.List(value: filter);
            }
            else
            {
                snippets = _snippetManage.List(key: filter);
            }
        }

        foreach (var snippet in snippets)
        {
            var row = _dataTable.NewRow();
            row[0] = snippet.Key;
            row[1] = snippet.Value;
            _dataTable.Rows.Add(row);
        }
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
            if (selectedIndex > _dataTable.Rows.Count) return;

            // var kvp = KeyValuePairs[selectedIndex];
            // KeyValuePairs.RemoveAt(selectedIndex);
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

        // for (var i = 0; i < KeyValuePairs.Count; i++)
        // {
        //     var keyValuePair = KeyValuePairs[i];
        //     if (keyValuePair.Key == vp.Key)
        //     {
        //         return i;
        //     }
        // }

        return -1;
    }

    private void _renderSelect()
    {
        /*if (_editIndex != -1)
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
        }*/
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
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
            _snippetManage.Add(new SnippetModel
            {
                Key = key,
                Value = value
            });

            TbKey.Text = "";
            TbValue.Text = "";
        }
        else
        {
            // update 
            /*var oldKvp = KeyValuePairs[_editIndex];

            if (string.Equals(key, oldKvp.Key))
            {
                // just update value
            }
            else
            {
                // update key and value
            }*/
        }

        _loadData();
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


    private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
    {
        ComboBoxFilterType.SelectedIndex = 0;
        TbFilter.Text = "";

        _DoFilter();
    }

    private void ButtonFilter_OnClick(object sender, RoutedEventArgs e)
    {
        _DoFilter();
    }

    private void OnTbFilter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _DoFilter();
        }
    }

    private void _DoFilter()
    {
        _editIndex = -1;
        // _renderSelect();
        DataGrid.UnselectAll();

        _loadData();
    }
}