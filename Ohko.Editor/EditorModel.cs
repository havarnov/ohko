using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using Avalonia;
using Avalonia.Media.Imaging;
using DynamicData;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Ohko.Editor;

public class RectangleModel : ReactiveObject
{
    public required Rect Rect
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public required UserDataModel UserDataModel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}

public class UserDataModel : ReactiveObject
{
    public UserDataModel(Guid id, ObservableCollection<(int, Rect?)> frames)
    {
        Frames = frames;
        Frames.CollectionChanged += (_, __) =>
            this.RaisePropertyChanged(nameof(Frames));
        Id = id;
    }

    public Guid Id { get; }

    public ObservableCollection<(int, Rect?)> Frames
    {
        get => field;
        private set => this.RaiseAndSetIfChanged(ref field, value);
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

    public ObservableCollection<UserDataModel> UserDataModels { get; } =
    [
        new(
            Guid.NewGuid(),
            [(0, new Rect(0, 0, 10, 10))])
        {
            Value = """
                    {"her": 42}
                    """,
        },
        new(
            Guid.NewGuid(),
            [
                (0, new Rect(16, 16, 5, 10)),
                (1, new Rect(16, 16, 5, 10)),
                (2, new Rect(16, 16, 5, 8)),
            ])
        {
            Value = """
                    {"her": 43}
                    """,
        },
    ];

    private readonly ObservableCollection<RectangleModel> _rectanglesCurrentFrame = [];
    private readonly ObservableCollection<UserDataModel> _userDataModelsCurrentFrame = [];

    public EditorModel()
    {
        RectanglesCurrentFrame = new ReadOnlyObservableCollection<RectangleModel>(_rectanglesCurrentFrame);
        UserDataModelsCurrentFrame = new ReadOnlyObservableCollection<UserDataModel>(_userDataModelsCurrentFrame);

        // Listen to additions/removals in UserData
        UserDataModels.CollectionChanged += (s, e) =>
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
            UpdateForCurrentFrame();
        };

        // Subscribe existing items (if any)
        foreach (var item in UserDataModels)
        {
            item.PropertyChanged += UserDataItem_PropertyChanged;
        }
    }

    private void UserDataItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateForCurrentFrame();
    }

    public ReadOnlyObservableCollection<RectangleModel> RectanglesCurrentFrame { get; }
    public ReadOnlyObservableCollection<UserDataModel> UserDataModelsCurrentFrame { get; }

    public UserDataModel? SelectedUserDataModel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Guid? SelectedUserDataModelId
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

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
            UpdateForCurrentFrame();
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

    private void UpdateForCurrentFrame()
    {
        {
            var relevant = UserDataModels
                .Select(u =>
                {
                    (int, Rect?)? frame = u.Frames.FirstOrDefault(i => i.Item1 == SelectedFrameIdx);
                    if (frame?.Item2 is null)
                    {
                        return null;
                    }

                    return new RectangleModel()
                    {
                        Rect = frame.Value.Item2.Value,
                        UserDataModel = u,
                    };
                })
                .Where(i => i is not null)
                .Select(i => i!)
                .ToList();

            var removed = _rectanglesCurrentFrame.Except(relevant).ToList();
            var inserted = relevant.Except(_rectanglesCurrentFrame).ToList();
            _rectanglesCurrentFrame.RemoveMany(removed);
            _rectanglesCurrentFrame.AddRange(inserted);
        }

        {
            var relevant = UserDataModels
                .Where(u => u.Frames.Any(i => i.Item1 == SelectedFrameIdx))
                .ToList();
            var removed = _userDataModelsCurrentFrame.Except(relevant).ToList();
            var inserted = relevant.Except(_userDataModelsCurrentFrame).ToList();
            _userDataModelsCurrentFrame.RemoveMany(removed);
            _userDataModelsCurrentFrame.AddRange(inserted);
        }
    }

    public void AddRect(Rect rect)
    {
        if (SelectedUserDataModel is null)
        {
            return;
        }

        var index = SelectedUserDataModel.Frames
            .ToList()
            .FindIndex(f => f.Item1 == SelectedFrameIdx);

        if (index >= 0)
        {
            SelectedUserDataModel.Frames[index] = (SelectedFrameIdx, rect);
        }
        else
        {
            SelectedUserDataModel.Frames.Add((SelectedFrameIdx, rect));
        }

        SelectedUserDataModel.Frames.Add((SelectedFrameIdx, rect));
    }
}