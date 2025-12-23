using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Ohko.Core;

public class StatesConfiguration
{
    public required Dictionary<string, StateConfiguration> States { get; init; }
}

public class StateConfiguration
{
    public required string AnimationName { get; init; }
    public required string AnimationTag { get; init; }
    public required string? AutomaticContinuation { get; init; }
    public required Dictionary<string, FrameConfiguration> Frames { get; init; }
}

public class FrameConfiguration
{
    public required List<Box> Boxes { get; init; }
    // public required List<Effect> Effects { get; init; }
}

// public abstract class Effect;
//
// public class MoveEffect : Effect
// {
//     public required Vector2 Vector { get; init; }
// }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CollisionBox), typeDiscriminator: nameof(CollisionBox))]
public abstract class Box
{
    private Box()
    {
    }

    [JsonConverter(typeof(RectangleJsonConverter))]
    public required Rectangle Rectangle { get; init; }

    public class CollisionBox : Box
    {
        public string CollisionTag { get; init; }
    }
}

internal class RectangleJsonConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        Point? location = null;
        Point? size = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = reader.GetString();
            switch (propertyName)
            {
                case "location":
                    location = ReadPoint(ref reader, typeToConvert, options);
                    break;
                case "size":
                    size = ReadPoint(ref reader, typeToConvert, options);
                    break;
                default:
                    throw new JsonException();
            }
        }

        if (location is null || size is null)
        {
            throw new JsonException();
        }

        return new Rectangle(location.Value, size.Value);
    }

    private Point ReadPoint(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        var x = reader.GetInt32();

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        var y = reader.GetInt32();

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException();
        }

        return new Point(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
