using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

            var file = await AsepriteFrameConfigFile.FromPath(files[0].TryGetLocalPath() ?? throw new InvalidOperationException());

            MainWindowViewModel?.Select(file);
        }
        catch (Exception e)
        {
            // Nothing can be done :(
            Console.WriteLine(e);
        }
    }
}