using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using ReactiveUI;

namespace Ohko.Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SaveMenuItemClickHandler(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SaveCommand.Execute(null);
        }
    }
}

public class MainWindowViewModel : ViewModelBase
{
    public ICommand SaveCommand { get; }

    public MainWindowViewModel()
    {
        TabItems.Add(new HomeUserControl
        {
            UserControlType = UserControlType.SelectFile,
            ViewModel = this,
        });

        new TextBlock();

        SaveCommand = ReactiveCommand.Create(Save);
    }

    public ObservableCollection<TabbedUserControl> TabItems { get; set; } = [];

    public TabbedUserControl? SelectedTab
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void Select(EditorModel editorModel)
    {
        var existing = TabItems.FirstOrDefault(i =>
            i is FileEditorTabbedUserControl { } control
            && control.EditorModel.Path == editorModel.Path);
        if (existing is not null)
        {
            SelectedTab = existing;
            return;
        }

        TabItems.Add(new FileEditorTabbedUserControl
        {
            EditorModel = editorModel,
            UserControlType = UserControlType.FileEditor,
        });

        SelectedTab = TabItems.LastOrDefault();
    }

    private void Save()
    {
        if (SelectedTab is FileEditorTabbedUserControl fileEditorTabbedUserControl)
        {
            fileEditorTabbedUserControl.EditorModel.Save();
        }
    }
}

public class FileEditorTabbedUserControl : TabbedUserControl
{
    public required EditorModel EditorModel
    {
        get;
        init
        {
            field = value;
            field.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(EditorModel.IsDirty))
                {
                    RaisePropertyChanged(nameof(TabName));
                }
            };
        }
    }

    public override string TabName =>
        Path.GetFileName(EditorModel.Path) + (EditorModel.IsDirty ? "*" : string.Empty) ?? "N/A";
}

public class HomeUserControl : TabbedUserControl
{
    public required MainWindowViewModel ViewModel { get; init; }
    public override string TabName { get; } = "Home";
}

public abstract class TabbedUserControl : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged(string prop)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public abstract string TabName { get; }
    public required UserControlType UserControlType { get; init; }
}

public enum UserControlType
{
    SelectFile,
    FileEditor,
}

public class ControlSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control? Build(object? param)
    {
        if (param is not TabbedUserControl tabbedUserControl)
        {
            throw new ArgumentNullException(nameof(param));
        }

        var control = AvailableTemplates[tabbedUserControl.UserControlType.ToString()].Build(param);

        switch (control, tabbedUserControl)
        {
            case (HomeControl selectFileControl, HomeUserControl homeUserControl):
                selectFileControl.MainWindowViewModel = homeUserControl.ViewModel;
                break;
            case (EditorUserControl editorUserControl, FileEditorTabbedUserControl fileEditorTabbedUserControl):
                editorUserControl.DataContext = new EditorViewModel(editorUserControl, fileEditorTabbedUserControl.EditorModel);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return control;
    }

    public bool Match(object? data)
    {
        return true;
    }
}