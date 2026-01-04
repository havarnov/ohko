using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using Avalonia;

namespace Ohko.Editor;

public class AsepriteFrameRectangleDto
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public class AsepriteFrameConfigItemDto
{
    public AsepriteFrameRectangleDto? Rectangle { get; init; }
    public required List<int> Frames { get; init; }
    public JsonElement? UserData { get; init; }
}

public class AsepriteFrameConfigFileDeserializationModel
{
    public string? AsepriteFilePath { get; init; }
    public List<AsepriteFrameConfigItemDto>? Items { get; init; }
}

public class AsepriteFrameConfigItem : INotifyPropertyChanged
{
    public required Rect? Rectangle
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rectangle)));
        }
    }

    public required JsonElement? UserData
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rectangle)));
        }
    }

    public required ObservableCollection<int> Frames
    {
        get;
        init
        {
            field = value;
            field.CollectionChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frames)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class AsepriteFrameConfigFile : INotifyPropertyChanged
{
    public bool IsDirty
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDirty)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Path { get; private set; }

    public void SetPath(string path)
    {
        if (Path != path)
        {
            IsDirty = true;
        }

        Path = path;
    }

    public AsepriteFile? AsepriteFile { get; private set; }
    public string? AsepriteFilePath { get; private set; }

    public void SetAsepriteFile(AsepriteFile? asepriteFile, string? path)
    {
        if (!ReferenceEquals(asepriteFile, AsepriteFile))
        {
            IsDirty = true;
        }

        AsepriteFilePath = path;
        AsepriteFile = asepriteFile;
    }

    public required ObservableCollection<AsepriteFrameConfigItem> Items
    {
        get;
        set
        {
            if (field != null)
            {
                // Unsubscribe from previous collection and items
                field.CollectionChanged -= Items_CollectionChanged;
                foreach (var item in field)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            field = value;

            // Subscribe to new collection and items
            field.CollectionChanged += Items_CollectionChanged;
            foreach (var item in field)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        IsDirty = true;

        if (e.OldItems != null)
        {
            foreach (AsepriteFrameConfigItem oldItem in e.OldItems)
            {
                oldItem.PropertyChanged -= Item_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (AsepriteFrameConfigItem newItem in e.NewItems)
            {
                newItem.PropertyChanged += Item_PropertyChanged;
            }
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
    }

    public static async Task<AsepriteFrameConfigFile> FromPath(string path)
    {
        if (!File.Exists(path))
        {
            return new AsepriteFrameConfigFile
            {
                Path = path,
                AsepriteFile = null,
                Items = new ObservableCollection<AsepriteFrameConfigItem>(),
            };
        }

        var file = JsonSerializer.Deserialize<AsepriteFrameConfigFileDeserializationModel>(await File.ReadAllBytesAsync(path));

        AsepriteFile? asepriteFile = null;
        if (file?.AsepriteFilePath is not null)
        {

            var asepriteFileName = System.IO.Path.GetFileName(file.AsepriteFilePath);
            asepriteFile = AsepriteFileLoader.FromStream(
                asepriteFileName,
                File.OpenRead(file.AsepriteFilePath));
        }

        var items = new ObservableCollection<AsepriteFrameConfigItem>();
        if (file?.Items is not null)
        {
            foreach (var item in file.Items)
            {
                var frames = new ObservableCollection<int>(item.Frames);
                items.Add(new AsepriteFrameConfigItem()
                {
                    Rectangle = item.Rectangle is { } r
                        ? new Rect(
                            position: new Point(r.X, r.Y),
                            size: new Size(r.Width, r.Height))
                        : null,
                    UserData = item.UserData,
                    Frames = frames,
                });
            }
        }

        return new AsepriteFrameConfigFile
        {
            Path = path,
            AsepriteFile = asepriteFile,
            Items = items,
        };
    }

    public void Save()
    {
        if (Path is null)
        {
            return;
        }

        var serialized = JsonSerializer.Serialize(new AsepriteFrameConfigFileDeserializationModel()
        {
            AsepriteFilePath = AsepriteFilePath,
            Items = Items
                .Select(i => new AsepriteFrameConfigItemDto
                {
                    Frames = [],
                })
                .ToList(),
        });
        File.WriteAllText(Path, serialized);
        IsDirty = false;
    }
}