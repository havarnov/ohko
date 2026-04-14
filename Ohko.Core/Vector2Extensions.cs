using Microsoft.Xna.Framework;

namespace Ohko.Core;

public static class Vector2Extensions
{
    public static Vector2 Into(this nkast.Aether.Physics2D.Common.Vector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static nkast.Aether.Physics2D.Common.Vector2 Into(this Vector2 vector)
    {
        return new nkast.Aether.Physics2D.Common.Vector2(vector.X, vector.Y);
    }
}