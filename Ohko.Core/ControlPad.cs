using System;
using System.Collections.Generic;
using System.Linq;
using AsepriteDotNet.Aseprite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Aseprite;

namespace Ohko.Core;

internal class ControlPad(Hero hero)
{
    private Texture2D _texture = null!;
    private readonly Button[] _buttons = new Button[9];
    private Point? _gameBounds = null;
    private TextureRegion region = null!;
    bool leftButtonPressed = false;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        var tilesFile = content.Load<AsepriteFile>("tiles");
        var atlas = tilesFile.CreateTextureAtlas(
            graphicsDevice,
            layers: tilesFile.Layers.ToArray().Select(l => l.Name).ToList());
        region = atlas.GetRegion("tiles 0");

        _texture = new Texture2D(graphicsDevice, 1, 1);
        _texture.SetData([Color.White]);
    }

    bool started = false;
    private List<ButtonPosition> combo = new(10);

    public void Update(Point gameBounds)
    {
        if (_gameBounds != gameBounds)
        {
            var height = gameBounds.Y * 0.4;
            var offset = gameBounds.Y - height;
            var width = gameBounds.X;
            var idx = 0;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var colorBytes = new byte[3];
                    Random.Shared.NextBytes(colorBytes);
                    var size = new Point((int)(width / 3f), (int)(height / 3f));
                    var location = new Point(size.X * i, (int)(offset + (size.Y * j)));
                    // TODO: remember previous state.
                    _buttons[idx] = new Button(location, size, (ButtonPosition)(idx + 1));
                    idx++;
                }
            }
            _gameBounds = gameBounds;
        }

        var state = TouchPanel.GetState();
        foreach (var touchLocation in state)
        {
            if (touchLocation.State == TouchLocationState.Released)
            {
                hero.AddCombo(combo.ToList());

                started = false;
                combo.Clear();
                foreach (var button in _buttons)
                {
                    button.IsEnabled = false;
                }
            }
            else if (touchLocation.State == TouchLocationState.Pressed)
            {
                var button = _buttons.FirstOrDefault(b => b.Contains(touchLocation.Position.ToPoint()));
                if (button is not null && object.ReferenceEquals(button, _buttons[4]))
                {
                    started = true;
                    button.IsEnabled = true;
                    combo.Add(button.Position);
                }
            }
            else if (touchLocation.State == TouchLocationState.Moved && started)
            {
                var button = _buttons.FirstOrDefault(b => b.Contains(touchLocation.Position.ToPoint()));
                if (button is not null)
                {
                    button.IsEnabled = true;
                    if (combo.Last() != button.Position)
                    {
                        combo.Add(button.Position);
                    }
                }
            }
        }

        var mouseState = Mouse.GetState();

        if (leftButtonPressed && mouseState.LeftButton == ButtonState.Released)
        {
            hero.AddCombo(combo.ToList());

            started = false;
            combo.Clear();
            foreach (var button in _buttons)
            {
                button.IsEnabled = false;
            }
        }
        else if (!leftButtonPressed && mouseState.LeftButton == ButtonState.Pressed)
        {
            var button = _buttons.FirstOrDefault(b => b.Contains(new  Point(mouseState.Position.X, mouseState.Position.Y)));
            if (button is not null && object.ReferenceEquals(button, _buttons[4]))
            {
                started = true;
                button.IsEnabled = true;
                combo.Add(button.Position);
            }
        }
        else if (leftButtonPressed && mouseState.LeftButton == ButtonState.Pressed && started)
        {
            var button = _buttons.FirstOrDefault(b => b.Contains(new  Point(mouseState.Position.X, mouseState.Position.Y)));
            if (button is not null)
            {
                button.IsEnabled = true;
                if (combo.Last() != button.Position)
                {
                    combo.Add(button.Position);
                }
            }
        }

        leftButtonPressed = mouseState.LeftButton == ButtonState.Pressed;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var button in _buttons)
        {
            var sourceRec = !button.IsEnabled
                ? new Rectangle(
                    new Point(32, 32),
                    new Point(32, 32))
                : new Rectangle(
                    new Point(64, 32),
                    new Point(32, 32));
            spriteBatch.Draw(
                region.Texture,
                new Rectangle(
                    new Point(button.Bounds.X, button.Bounds.Y),
                    button.Bounds.Size),
                sourceRec,
                Color.White,
                rotation: 0,
                origin: Vector2.One,
                effects: SpriteEffects.None,
                layerDepth: 1f);
        }
    }

    public enum ButtonPosition
    {
        Unknown = 0,
        TopLeft = 1,
        MiddleLeft = 2,
        BottomLeft = 3,
        TopCenter = 4,
        Center = 5,
        BottomCenter = 6,
        TopRight = 7,
        MiddleRight = 8,
        BottomRight = 9,
    }

    private class Button(Point location, Point size, ButtonPosition position)
    {
        private readonly Color colorDisabled = Color.Red;
        // new Color(Random.Shared.Next(0, 255), Random.Shared.Next(0, 255), Random.Shared.Next(0, 255));
        private readonly Color colorEnabled = Color.Green;

        public Rectangle Bounds { get; } = new Rectangle(location, size);
        public Color Color => IsEnabled ? colorEnabled : colorDisabled;
        public ButtonPosition Position => position;

        public bool IsEnabled { get; set; } = false;

        public bool Contains(Point location)
        {
            return Bounds.Contains(location);
        }
    }
}