using System.Collections.Generic;
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

public class AsepriteFrameConfigFile
{
    public required string? Path { get; init; }
    public required AsepriteFile? AsepriteFile { get; set; }
    public required List<AsepriteFrameConfigItem> Items { get; init; }

    public static async Task<AsepriteFrameConfigFile> FromPath(string path)
    {
        if (!File.Exists(path))
        {
            return new AsepriteFrameConfigFile
            {
                Path = path,
                AsepriteFile = null,
                Items = [],
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

        var items = new List<AsepriteFrameConfigItem>();
        if (file?.Items is not null)
        {
            items.AddRange(file.Items.Select(i => new AsepriteFrameConfigItem()));
        }

        return new AsepriteFrameConfigFile
        {
            Path = path,
            AsepriteFile = asepriteFile,
            Items = items,
        };
    }
}