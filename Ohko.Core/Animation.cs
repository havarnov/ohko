using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Aseprite;

namespace Ohko.Core;

public class Animation
{
    public required AnimatedSprite AnimatedSprite { get; init; }
    public required Dictionary<int, List<FrameConfig>> FrameConfigs { get; init; }
    public required string? AutomaticContinuation { get; init; }
    public required Hero Hero { get; init; }

    public List<FrameConfig> CurrentFrame =>
        FrameConfigs.TryGetValue(AnimatedSprite.CurrentFrame.FrameIndex, out var frames)
            ? frames
            : [];

    public IEnumerable<T> Current<T>() where T : FrameConfig => CurrentFrame
        .Where(c => c is T)
        .Select(c =>
        {
            if (c is BoxConfig bc)
            {
                var size = Hero._animations[Hero.CurrentAnimation].AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Size;
                var offset = size.ToVector2() / 2f;
                offset = new Vector2((float)Math.Ceiling(offset.X), (float)Math.Floor(offset.Y));

                var location = Hero._isFacingLeft
                    ? new Point(size.X - bc.Rectangle.Location.X - bc.Rectangle.Size.X, bc.Rectangle.Location.Y)
                    : bc.Rectangle.Location;

                var position = (Hero.Position - offset);
                position.Round();
                var boxRectangle = new Rectangle(
                    position.ToPoint()
                    + location,
                    bc.Rectangle.Size);

                return bc with { Rectangle = boxRectangle, };
            }

            return c;
        })
        .Cast<T>();
}