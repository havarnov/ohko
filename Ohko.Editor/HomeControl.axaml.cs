using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using AsepriteDotNet.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Ohko.Editor;

public partial class HomeControl : UserControl
{
    public MainWindowViewModel? MainWindowViewModel { get; set; }

    public HomeControl()
    {
        InitializeComponent();
        DataContext = this;
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

            var model = JsonSerializer.Deserialize<AsepriteFrameConfigFileDeserializationModel>(await File.ReadAllTextAsync(path));
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
            var editorModel = new EditorModel(path, model.AsepriteFilePath, userModels)
            {
                AsepriteFile = model.AsepriteFilePath is not null
                    ? AsepriteFileLoader.FromFile(model.AsepriteFilePath)
                    : null,
            };

            MainWindowViewModel?.Select(editorModel);
        }
        catch (Exception e)
        {
            // Nothing can be done :(
            Console.WriteLine(e);
        }
    }
}