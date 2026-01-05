using System.Collections.Generic;
using System.IO;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Ohko.Editor;

public class EditorModel : ReactiveObject
{
    public int FrameCount => AsepriteFile?.Frames.Length ?? 0;

    private Dictionary<int, Bitmap> _bitmaps = [];

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
}