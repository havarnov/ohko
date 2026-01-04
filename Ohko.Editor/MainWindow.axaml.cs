using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

        SaveCommand = ReactiveCommand.Create(Save);
    }

    public ObservableCollection<TabbedUserControl> TabItems { get; set; } = [];

    public TabbedUserControl? SelectedTab
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void Select(AsepriteFrameConfigFile file)
    {
        TabItems.Add(new FileEditorTabbedUserControl
        {
            File = file,
            UserControlType = UserControlType.FileEditor,
        });
    }

    private void Save()
    {
        if (SelectedTab is FileEditorTabbedUserControl fileEditorTabbedUserControl)
        {
            fileEditorTabbedUserControl.File.Save();
        }
    }
}

public class FileEditorTabbedUserControl : TabbedUserControl
{
    private AsepriteFrameConfigFile _file = default!;

    public required AsepriteFrameConfigFile File
    {
        get => _file;
        init
        {
            _file = value;
            _file.PropertyChanged += FileOnPropertyChanged;
        }
    }

    private void FileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AsepriteFrameConfigFile.IsDirty))
            RaisePropertyChanged(nameof(TabName));
    }

    public override string TabName =>
        (File.IsDirty ? "*" : "") + Path.GetFileName(File.Path);
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
            case (FileEditorControl fileEditorControl, FileEditorTabbedUserControl fileEditorTabbedUserControl):
                fileEditorControl.ConfigFile = fileEditorTabbedUserControl.File;
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