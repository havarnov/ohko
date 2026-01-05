using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using Avalonia;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Ohko.Editor;

public class UserDataModel : ReactiveObject
{
    public ObservableCollection<int> Frames
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public Rect? Rectangle
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Value
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}

public class EditorModel : ReactiveObject
{
    public int FrameCount => AsepriteFile?.Frames.Length ?? 0;

    private Dictionary<int, Bitmap> _bitmaps = [];

    public ObservableCollection<UserDataModel> UserData { get; } = [ new UserDataModel()
    {
        Frames = [0],
        Rectangle = new Rect(0, 0, 10, 10),
    }];

    private readonly ObservableCollection<Rect> _rectanglesCurrentFrame = new();

    public EditorModel()
    {
        RectanglesCurrentFrame = new ReadOnlyObservableCollection<Rect>(_rectanglesCurrentFrame);

        // Listen to additions/removals in UserData
        UserData.CollectionChanged += (s, e) =>
        {
            // For items added: subscribe to property changes
            if (e.NewItems != null)
            {
                foreach (UserDataModel item in e.NewItems)
                {
                    item.PropertyChanged += UserDataItem_PropertyChanged;
                }
            }

            // For items removed: unsubscribe to avoid memory leaks
            if (e.OldItems != null)
            {
                foreach (UserDataModel item in e.OldItems)
                {
                    item.PropertyChanged -= UserDataItem_PropertyChanged;
                }
            }

            // Recalculate rectangles for the current frame
            UpdateRectanglesForCurrentFrame();
        };

        // Subscribe existing items (if any)
        foreach (var item in UserData)
        {
            item.PropertyChanged += UserDataItem_PropertyChanged;
        }
    }

    private void UserDataItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserDataModel.Rectangle))
        {
            UpdateRectanglesForCurrentFrame();
        }
    }

    public ReadOnlyObservableCollection<Rect> RectanglesCurrentFrame { get; }

    public Bitmap? Bitmap
    {
        get
        {
            if (AsepriteFile is null)
            {
                return null;
            }

            if (_bitmaps.TryGetValue(SelectedFrameIdx, out var value))
            {
                return value;
            }

            var flattenedFrame = AsepriteFile.GetFrame(SelectedFrameIdx).FlattenFrame();
            var width = AsepriteFile.Frames[SelectedFrameIdx].Size.Width;
            var height = AsepriteFile.Frames[SelectedFrameIdx].Size.Height;
            using var image = CreateImageSharpFromRgbaBytes(flattenedFrame, width, height);
            using var memoryStream = new MemoryStream();
            image.SaveAsync(memoryStream, new PngEncoder());
            memoryStream.Position = 0;
            var bitmap = new Bitmap(memoryStream);
            _bitmaps[SelectedFrameIdx] = bitmap;

            return bitmap;
        }
    }

    public int SelectedFrameIdx
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(Bitmap));
            UpdateRectanglesForCurrentFrame();
        }
    }

    public AsepriteFile? AsepriteFile
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(FrameCount));
            this.RaisePropertyChanged(nameof(Bitmap));
            SelectedFrameIdx = 0;
            _bitmaps.Clear();
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

        var image = Image.LoadPixelData<Rgba32>(bytes, width, height);
        return image;
    }

    private void UpdateRectanglesForCurrentFrame()
    {
        _rectanglesCurrentFrame.Clear();

        var relevant = UserData
            .Where(u => u.Frames.Contains(SelectedFrameIdx))
            .Where(u => u.Rectangle.HasValue)
            .Select(u => u.Rectangle!.Value);

        foreach (var r in relevant)
        {
            _rectanglesCurrentFrame.Add(r);
        }
    }

    public void AddRect(Rect rect)
    {
        UserData.Add(new UserDataModel()
        {
            Rectangle = rect,
            Frames = [SelectedFrameIdx],
        });
    }
}