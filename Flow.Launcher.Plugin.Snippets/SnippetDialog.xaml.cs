using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Flow.Launcher.Plugin.Snippets.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.Snippets;

public partial class SnippetDialog : Window, INotifyPropertyChanged
{
    private static SnippetDialog _dialog;

    public static void ShowWDialog(IPublicAPI publicApi, SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        if (_dialog == null)
        {
            _dialog = new SnippetDialog(publicApi, snippetManage, selectSm);
            _dialog.ShowDialog();
        }
        else
        {
            _dialog._selectSm = selectSm;
            _dialog.Activate();
        }
    }

    private IPublicAPI _publicAPI;

    private SnippetManage _snippetManage;

    // for edit
    [CanBeNull] private SnippetModel _selectSm;

    // public string TitleName { get; set; }

    private string _titleName;

    public string TitleName
    {
        get => _titleName;
        set
        {
            _titleName = value;
            OnPropertyChanged(nameof(TitleName));
        }
    }

    public SnippetDialog(IPublicAPI publicApi,
        SnippetManage snippetManage,
        [CanBeNull] SnippetModel selectSm = null)
    {
        _publicAPI = publicApi;
        _snippetManage = snippetManage;
        _selectSm = selectSm;

        InitializeComponent();
        DataContext = this;
        Closed += (sender, args) => { _dialog = null; };
        PreviewKeyDown += (sender, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };

        Activated += (sender, args) => { _renderView(); };
    }

    private void _renderView()
    {
        TitleName = _publicAPI.GetTranslation(_selectSm != null
            ? "snippets_plugin_edit_snippet"
            : "snippets_plugin_add");

        InnerLogger.Logger.Info($"TitleName = {TitleName}");

        if (_selectSm != null)
        {
            TbKey.IsEnabled = false;
            TbKey.Text = _selectSm.Key;
            TbValue.Text = _selectSm.Value;
            TbScore.Text = $"{_selectSm.Score}";
        }
        else
        {
            TbKey.IsEnabled = true;
            TbKey.Text = "";
            TbValue.Text = "";
            TbScore.Text = "0";
        }
    }

    private bool _doSave()
    {
        var key = TbKey.Text.Trim();
        var value = TbValue.Text;
        var scoreResult = int.TryParse(TbScore.Text, out var score);
        if (!scoreResult)
            score = 0;

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            return false;

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
                return false;
            }

            _snippetManage.Add(new SnippetModel
            {
                Key = key,
                Value = value,
                Score = score
            });
        }
        else
        {
            // update
            var sm = new SnippetModel
            {
                Key = _selectSm.Key,
                Value = value,
                Score = score
            };
            return _snippetManage.UpdateByKey(sm);
        }

        return true;
    }

    private void SaveAndCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (_doSave())
            Close();
    }

    private void SaveButtonClient(object sender, RoutedEventArgs e)
    {
        _doSave();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    // {
    //     if (EqualityComparer<T>.Default.Equals(field, value)) return false;
    //     field = value;
    //     OnPropertyChanged(propertyName);
    //     return true;
    // }
}