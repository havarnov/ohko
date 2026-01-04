using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;

namespace Ohko.Editor;

public class AsepriteFrameConfigItemDto;

public class AsepriteFrameConfigFileDeserializationModel
{
    public string? AsepriteFilePath { get; init; }
    public List<AsepriteFrameConfigItemDto>? Items { get; init; }
}

public class AsepriteFrameConfigItem;

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
        set {
            field = value;
            field.CollectionChanged += (_, _) => IsDirty = true;
        }
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
                items.Add(new AsepriteFrameConfigItem());
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
                .Select(i => new AsepriteFrameConfigItemDto())
                .ToList(),
        });
        File.WriteAllText(Path, serialized);
        IsDirty = false;
    }
}