using System;
using Microsoft.Xna.Framework;

namespace Ohko.Core;

public class HeroBot(Hero hero) : IEntity
{
    private TimeSpan _sinceLastAction = TimeSpan.Zero;

    public void Update(GameTime gameTime)
    {
        _sinceLastAction += gameTime.ElapsedGameTime;
        if (_sinceLastAction.TotalMilliseconds < 200)
        {
            return;
        }

        var distance = Math.Sqrt(
            Math.Pow(hero.Face.Position.X - hero.Position.X, 2) +
            Math.Pow(hero.Face.Position.Y - hero.Position.Y, 2));

        if (distance > 50)
        {
            hero.AddCombo([
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft,
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleRight,
                ControlPad.ButtonPosition.TopRight,
            ]);
        }
        else
        {
            if (hero.Face.CurrentAnimation == "kIdle")
            {
                hero.AddCombo([
                    ControlPad.ButtonPosition.Center,
                    ControlPad.ButtonPosition.MiddleLeft,
                    ControlPad.ButtonPosition.Center,
                    ControlPad.ButtonPosition.MiddleRight,
                    ControlPad.ButtonPosition.TopRight,
                ]);
            }
            else
            {
                hero.AddCombo([
                    ControlPad.ButtonPosition.Center,
                    ControlPad.ButtonPosition.BottomCenter,
                ]);
            }
        }


        _sinceLastAction = TimeSpan.Zero;
    }
}