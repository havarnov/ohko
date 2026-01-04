using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using AsepriteDotNet.Aseprite;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Ohko.Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        TabItems.Add(new HomeUserControl
        {
            TabName = "home",
            UserControlType = UserControlType.SelectFile,
            ViewModel = this,
        });
    }

    public ObservableCollection<TabbedUserControl> TabItems { get; set; } = [];

    public void Select(AsepriteFrameConfigFile file)
    {
        TabItems.Add(new FileEditorTabbedUserControl
        {
            File = file,
            TabName = Path.GetFileName(file.Path) ?? "<??>",
            UserControlType = UserControlType.FileEditor,
        });
    }
}

public class FileEditorTabbedUserControl : TabbedUserControl
{
    public required AsepriteFrameConfigFile File { get; init; }
}

public class HomeUserControl : TabbedUserControl
{
    public required MainWindowViewModel ViewModel { get; init; }
}

public abstract class TabbedUserControl
{
    public required string TabName { init; get; }
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