using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using AsepriteDotNet.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DynamicData;
using Ohko.Core;
using ReactiveUI;

namespace Ohko.Editor;

public class ConfigFile
{
    [JsonPropertyName("recentFiles")]
    public List<string>? RecentFiles { get; init; } = [];
}

public partial class HomeControl : UserControl
{
    public MainWindowViewModel? MainWindowViewModel { get; set; }

    private string _configFilePath;
    public ObservableCollection<string> RecentFiles { get; } = [];
    public ICommand SelectRecentFileCommand { get; }

    public HomeControl()
    {
        InitializeComponent();
        DataContext = this;
        SelectRecentFileCommand = ReactiveCommand.Create<string>(SelectRecentFile);

        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ohko.Editor",
            "config.json");

        if (File.Exists(_configFilePath))
        {
            var configFile = JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText(_configFilePath));
            if (configFile?.RecentFiles is { } recentFiles)
            {
                RecentFiles.AddRange(recentFiles);
            }
        }
    }

    private void SelectRecentFile(string selected)
    {
        var editorModel = CreateEditorModel(selected);
        MainWindowViewModel?.Select(editorModel);
    }

    private async void SelectFileButtonClickHandler(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
            {
                return;
            }

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Open Text File",
                    AllowMultiple = false,
                });

            if (files.Count != 1)
            {
                return;
            }

            var path = files[0].TryGetLocalPath();
            if (path is null)
            {
                throw new Exception("??");
            }

            var editorModel = CreateEditorModel(path);
            AddRecentFile(path);
            MainWindowViewModel?.Select(editorModel);
        }
        catch (Exception e)
        {
            // Nothing can be done :(
            Console.WriteLine(e);
        }
    }

    private void AddRecentFile(string path)
    {
        RecentFiles.Remove(path);
        RecentFiles.Insert(0, path);
        var configFile = JsonSerializer.Serialize(new ConfigFile()
        {
            RecentFiles = RecentFiles.ToList(),
        });
        var directory = Path.GetDirectoryName(_configFilePath) ?? throw new Exception("??");
        Directory.CreateDirectory(directory);
        File.WriteAllText(_configFilePath, configFile);
    }

    private EditorModel CreateEditorModel(string path)
    {
        var model = JsonSerializer.Deserialize<AsepriteFrameConfigFileDeserializationModel>(File.ReadAllText(path));
        if (model is null)
        {
            throw new Exception("??");
        }

        var userModels = new ObservableCollection<UserDataModel>(
            model.UserModels?
                .Select(u =>
                {
                    var frames =
                        u.Frames?
                            .Select(f =>
                            {
                                Rect? rect = null;
                                if (f.Rectangle is { } rectangle)
                                {
                                    rect = new Rect(
                                        new Point(rectangle.X, rectangle.Y),
                                        new Size(rectangle.Width, rectangle.Height));
                                }

                                return (f.FrameIndex, rect);
                            })
                            .ToList()
                        ?? [];
                    return new UserDataModel(u.Id, new ObservableCollection<(int, Rect?)>(frames))
                    {
                        Value = u.Value is null
                            ? null
                            : JsonSerializer.Serialize(u.Value),
                        Color = u.Color is null
                            ? Color.Parse("Lime")
                            : Color.Parse(u.Color),
                    };
                })
                .ToList()
            ?? []);
        return new EditorModel(path, model.AsepriteFilePath, userModels)
        {
            AsepriteFile = model.AsepriteFilePath is not null
                ? AsepriteFileLoader.FromFile(model.AsepriteFilePath)
                : null,
        };
    }
}