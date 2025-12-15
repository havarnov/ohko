using Microsoft.Xna.Framework;

namespace Ohko.Core;

public static class AetherExtenstions
{
    public static Vector2 ToVector2(this nkast.Aether.Physics2D.Common.Vector2 vector2)
    {
        return new Vector2(vector2.X, vector2.Y);
    }

    public static nkast.Aether.Physics2D.Common.Vector2 ToVector2(this Vector2 vector2)
    {
        return new nkast.Aether.Physics2D.Common.Vector2(vector2.X, vector2.Y);
    }
}