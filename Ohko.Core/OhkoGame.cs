using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGameGum;

namespace Ohko.Core;

internal class ControlPad
{
    private Texture2D _texture = null!;
    private Button[] _buttons = new Button[9];
    private Point? _gameBounds = null;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _texture = new Texture2D(graphicsDevice, 1, 1);
        _texture.SetData([Color.White]);
    }

    bool leftButtonPressed = false;
    private List<Vector2> touches = new List<Vector2>(10);

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
                    _buttons[idx] = new Button(location, size);
                    idx++;
                }
            }
            _gameBounds = gameBounds;
        }

        var state = TouchPanel.GetState();
        foreach (var touchLocation in state)
        {
            touches.Add(touchLocation.Position);
            if (touchLocation.State == TouchLocationState.Released)
            {
                var buttons = _buttons.Where(b => touches.Any(t => b.Contains(t.ToPoint())));
                foreach (var button in buttons)
                {
                    button.IsEnabled = !button.IsEnabled;
                }

                touches.Clear();
            }
        }

        var mouseState = Mouse.GetState();

        if (leftButtonPressed && mouseState.LeftButton == ButtonState.Released)
        {
            var button = _buttons.FirstOrDefault(b => b.Contains(mouseState.Position));
            if (button is not null)
            {
                button.IsEnabled = !button.IsEnabled;
            }
        }

        leftButtonPressed = mouseState.LeftButton == ButtonState.Pressed;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var button in _buttons)
        {
            spriteBatch.Draw(
                _texture,
                new Vector2(button.Bounds.X, button.Bounds.Y),
                new Rectangle(
                    Point.Zero,
                    button.Bounds.Size),
                button.Color,
                0,
                Vector2.Zero,
                1.0f,
                SpriteEffects.None,
                0.00001f);
        }
    }

    private class Button(Point location, Point size)
    {
        private readonly Color colorDisabled = Color.Red;
            // new Color(Random.Shared.Next(0, 255), Random.Shared.Next(0, 255), Random.Shared.Next(0, 255));
        private readonly Color colorEnabled = Color.Green;

        public Rectangle Bounds { get; } = new Rectangle(location, size);
        public Color Color => IsEnabled ? colorEnabled : colorDisabled;

        public bool IsEnabled { get; set; } = false;

        public bool Contains(Point location)
        {
            return Bounds.Contains(location);
        }
    }
}

public class OhkoGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private readonly Point _gameBounds = new Point(500, 1000);

    private ControlPad _controlPad = null!;
    private SpriteBatch _spriteBatch = null!;
    private Hero _hero = null!;

    public OhkoGame(bool isFullScreen)
    {
        if (isFullScreen)
        {
            _gameBounds.X = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _gameBounds.Y = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }

        _graphics = new GraphicsDeviceManager(this);
        _graphics.IsFullScreen = isFullScreen;
        _graphics.PreferredBackBufferWidth = _gameBounds.X;
        _graphics.PreferredBackBufferHeight = _gameBounds.Y;
        _graphics.SupportedOrientations = DisplayOrientation.Portrait;
        _graphics.ApplyChanges();

        #if IOS
            Content.RootDirectory = "Content/bin/iOS/Content/";
        #else
            Content.RootDirectory = "Content";
        #endif

        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumService.Default.Initialize(this);
        _controlPad = new ControlPad();
        _hero = new Hero(new Vector2(_gameBounds.X / 2f, (_gameBounds.Y * 0.6f) / 2f));
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _controlPad.LoadContent(_graphics.GraphicsDevice);
        _hero.LoadContent(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _controlPad.Update(_gameBounds);
        _hero.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _controlPad.Draw(_spriteBatch);
        _hero.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}