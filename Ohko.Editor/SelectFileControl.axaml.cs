using System;
using AsepriteDotNet.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Ohko.Editor;

public partial class SelectFileControl : UserControl
{
    public MainWindowViewModel? MainWindowViewModel { get; set; }

    public SelectFileControl()
    {
        InitializeComponent();
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
                    FileTypeFilter = [new FilePickerFileType("aseprite"), new FilePickerFileType("ase")],
                });

            if (files.Count != 1)
            {
                return;
            }

            var file = AsepriteFileLoader.FromStream(
                files[0].Name,
                await files[0].OpenReadAsync());

            MainWindowViewModel?.Add(file);
        }
        catch (Exception e)
        {
            // Nothing can be done :(
            Console.WriteLine(e);
        }
    }
}