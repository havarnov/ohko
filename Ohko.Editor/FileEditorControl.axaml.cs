using System;
using System.IO;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Ohko.Editor;

public partial class FileEditorControl : UserControl
{
    private int _frameIdx = 0;

    public static readonly StyledProperty<AsepriteFrameConfigFile?> ConfigFileProperty =
        AvaloniaProperty.Register<FileEditorControl, AsepriteFrameConfigFile?>(nameof(ConfigFile), null);

    public static readonly StyledProperty<AsepriteFile?> AsepriteFileProperty =
        AvaloniaProperty.Register<FileEditorControl, AsepriteFile?>(nameof(AsepriteFile), null);

    public AsepriteFile? AsepriteFile
    {
        get => GetValue(AsepriteFileProperty);
        set
        {
            if (value is not null)
            {

                var flattenedFrame = value.GetFrame(_frameIdx).FlattenFrame();
                var width = value.Frames[_frameIdx].Size.Width;
                var height = value.Frames[_frameIdx].Size.Height;
                using var image = CreateImageSharpFromRgbaBytes(flattenedFrame, width, height);
                using var memoryStream = new MemoryStream();
                image.SaveAsync(memoryStream, new PngEncoder());
                memoryStream.Position = 0;
                var bitmap = new Bitmap(memoryStream);
                ZoomCanvas.Image = bitmap;
            }

            SetValue(AsepriteFileProperty, value);
        }
    }

    public FileEditorControl()
    {
        InitializeComponent();
        DataContext = this;
        AffectsRender<FileEditorControl>(AsepriteFileProperty);
    }

    public AsepriteFrameConfigFile? ConfigFile
    {
        get => GetValue(ConfigFileProperty);
        set
        {
            SetValue(ConfigFileProperty, value);

            if (value?.AsepriteFile is not null)
            {
                AsepriteFile = value?.AsepriteFile;
            }
        }
    }

    private static Image<Rgba32> CreateImageSharpFromRgbaBytes(AsepriteDotNet.Common.Rgba32[] rgbaData, int width, int height)
    {
        var bytes = new byte[width * height * 4];
        var count = 0;
        for (var i = 0; i < rgbaData.Length; i++)
        {
            bytes[count] = rgbaData[i].R;
            bytes[count + 1] = rgbaData[i].G;
            bytes[count + 2] = rgbaData[i].B;
            bytes[count + 3] = rgbaData[i].A;
            count += 4;
        }

        var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(bytes, width, height);
        return image;
    }

    private void ZoomInClickHandler(object? sender, RoutedEventArgs e)
    {
        ZoomCanvas.UpdateZoom(zoomIn: true);
    }

    private void ZoomOutClickHandler(object? sender, RoutedEventArgs e)
    {
        ZoomCanvas.UpdateZoom(zoomIn: false);
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
                    Title = "Open Aseprite File",
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType("aseprite"), new FilePickerFileType("ase")]
                });

            if (files.Count != 1)
            {
                return;
            }

            var file = AsepriteFileLoader.FromStream(files[0].Name, await files[0].OpenReadAsync());
            ConfigFile.SetAsepriteFile(file, files[0].TryGetLocalPath());
            AsepriteFile = file;
        }
        catch (Exception e)
        {
            // Nothing can be done :(
            Console.WriteLine(e);
        }
    }
}