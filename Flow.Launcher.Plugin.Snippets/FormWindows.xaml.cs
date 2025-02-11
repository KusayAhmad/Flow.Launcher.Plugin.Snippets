using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class FormWindows : Window
{
    private static FormWindows _instance;

    public static void ShowWindows(IPublicAPI publicApi, SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        if (_instance == null)
        {
            _instance = new FormWindows(publicApi, snippetManage, selectSm);
            _instance.Show();
        }
        else
        {
            _instance._selectSm = selectSm;
            _instance.Activate();
        }
    }


    private IPublicAPI _publicAPI;

    private SnippetManage _snippetManage;

    // for edit
    [CanBeNull] private SnippetModel _selectSm;

    private readonly ObservableCollection<SnippetModel> _snippetsSource = new();

    public FormWindows(IPublicAPI publicApi,
        SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        _publicAPI = publicApi;
        _snippetManage = snippetManage;
        _selectSm = selectSm;

        InitializeComponent();

        Activated += (sender, args) =>
        {
            _reload();
        };

        Closed += (sender, args) => { _instance = null; };
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

        DataGrid.ItemsSource = _snippetsSource;

        DataContext = this;
        AddContextMenu();
    }

    private void _reload()
    {
        _loadData();
        var findIdx = _findBySelectData(_selectSm?.Key);
        if (findIdx != -1)
            DataGrid.SelectedIndex = findIdx;
    }

    private int _findBySelectData([CanBeNull] string findKey)
    {
        if (findKey == null) return -1;

        var findIdx = -1;
        for (var i = 0; i < _snippetsSource.Count; i++)
        {
            if (!string.Equals(findKey, _snippetsSource[i].Key)) continue;
            findIdx = i;
            break;
        }

        return findIdx;
    }

    private void _loadData()
    {
        _snippetsSource.Clear();

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
            _snippetsSource.Add(snippet);
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
            if (selectedIndex == -1 || selectedIndex >= _snippetsSource.Count) return;
            var sm = _snippetsSource[selectedIndex];
            _snippetsSource.RemoveAt(selectedIndex);
            _snippetManage.RemoveByKey(sm.Key);
            _selectSm = null;
            _renderSelect();
        };
        DataGrid.ContextMenu = new ContextMenu
        {
            Items =
            {
                deleteItem,
            }
        };
    }

    private void _renderSelect()
    {
        if (_selectSm != null)
        {
            // update
            BtnSwitch.Content = _publicAPI.GetTranslation("snippets_plugin_edit_item_key");

            TbKey.IsEnabled = false;
            TbKey.Text = _selectSm.Key;
            TbValue.Text = _selectSm.Value;
        }
        else
        {
            // add
            BtnSwitch.Content = _publicAPI.GetTranslation("snippets_plugin_add_item_key");
            TbKey.IsEnabled = true;
            TbKey.Text = "";
            TbValue.Text = "";
        }
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

        if (_selectSm == null)
        {
            // ADD
            var sm = _snippetManage.GetByKey(key);
            if (sm != null)
            {
                _publicAPI.ShowMsgError(
                    _publicAPI.GetTranslation("snippets_plugin_add_failed"),
                    _publicAPI.GetTranslation("snippets_plugin_add_failed_cause")
                );
                return;
            }

            _snippetManage.Add(new SnippetModel
            {
                Key = key,
                Value = value
            });

            TbKey.Text = "";
            TbValue.Text = "";
            _loadData();
        }
        else
        {
            // update
            var sm = new SnippetModel
            {
                Key = _selectSm.Key,
                Value = value,
                Score = _selectSm.Score
            };
            _snippetManage.UpdateByKey(sm);
            var findIdx = _findBySelectData(_selectSm?.Key);
            if (findIdx != -1)
            {
                _snippetsSource[findIdx] = sm;
                DataGrid.SelectedIndex = findIdx;
            }
        }
    }

    private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InnerLogger.Logger.Info($"DataGrid_OnSelectionChanged. {sender.GetType()}");
        if (sender is DataGrid dataGrid)
        {
            if (dataGrid.SelectedItem is SnippetModel sm)
            {
                _selectSm = sm;
            }

            _renderSelect();
        }
    }

    private void ButtonSwitch_OnClick(object sender, RoutedEventArgs e)
    {
        _selectSm = null;
        DataGrid.UnselectAll();
        _renderSelect();
    }


    private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
    {
        ComboBoxFilterType.SelectedIndex = 0;
        TbFilter.Text = "";

        _selectSm = null;
        DataGrid.UnselectAll();
        _loadData();
    }

    private void ButtonFilter_OnClick(object sender, RoutedEventArgs e)
    {
        _selectSm = null;
        DataGrid.UnselectAll();
        _loadData();
    }

    private void OnTbFilter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _selectSm = null;
            DataGrid.UnselectAll();
            _loadData();
        }
    }
}