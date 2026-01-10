using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ohko.Editor;

[JsonConverter(typeof(AsepriteFrameRectangleJsonConverter))]
public class AsepriteFrameRectangleDto
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public class AsepriteFrameDto
{
    public required int FrameIndex { get; init; }
    public AsepriteFrameRectangleDto? Rectangle { get; init; }
}

public class AsepriteUserDataItemDto
{
    public required Guid Id { get; init; }
    public string? Color { get; init; }
    public JsonElement? Value { get; init; }
    public List<AsepriteFrameDto>? Frames { get; init; }
}

public class AsepriteFrameConfigFileDeserializationModel
{
    public string? AsepriteFilePath { get; init; }
    public List<AsepriteUserDataItemDto>? UserModels { get; init; }
}

public sealed class AsepriteFrameRectangleJsonConverter
    : JsonConverter<AsepriteFrameRectangleDto>
{
    public override AsepriteFrameRectangleDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected JSON array.");

        reader.Read();
        int x = reader.GetInt32();

        reader.Read();
        int y = reader.GetInt32();

        reader.Read();
        int width = reader.GetInt32();

        reader.Read();
        int height = reader.GetInt32();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of JSON array.");

        return new AsepriteFrameRectangleDto
        {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        AsepriteFrameRectangleDto value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Width);
        writer.WriteNumberValue(value.Height);
        writer.WriteEndArray();
    }
}
