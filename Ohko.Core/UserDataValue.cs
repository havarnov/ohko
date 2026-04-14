using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Ohko.Core;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CollisionUserDataValue), typeDiscriminator: "CollisionBox")]
[JsonDerivedType(typeof(HitBoxUserDataValue), typeDiscriminator: "HitBox")]
[JsonDerivedType(typeof(HurtBoxUserDataValue), typeDiscriminator: "HurtBox")]
[JsonDerivedType(typeof(MoveEffectUserDataValue), typeDiscriminator: "MoveEffect")]
public abstract class UserDataValue;

public class CollisionUserDataValue : UserDataValue;
public class HitBoxUserDataValue : UserDataValue;
public class HurtBoxUserDataValue : UserDataValue;

public class MoveEffectUserDataValue : UserDataValue
{
    [JsonConverter(typeof(Vector2JsonConverter))]
    public required Vector2 Vector { get; init; }
    public required float Speed { get; init; }
}

internal class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        var x = reader.GetSingle();

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        var y = reader.GetSingle();

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException();
        }

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}