using Microsoft.Xna.Framework;

namespace Ohko.Core;

public abstract record FrameConfig;

public abstract record BoxConfig : FrameConfig
{
    public required Rectangle Rectangle { get; init; }
}

public record CollisionBoxConfig : BoxConfig;

public record HitBoxConfig : BoxConfig;

public record HurtBoxConfig : BoxConfig;

public record MoveEffectConfig : FrameConfig
{
    public required Vector2 Vector { get; init; }
    public required float Speed { get; init; }
}