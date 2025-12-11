using System;
using System.Collections.Generic;
using System.IO;
using AsepriteDotNet.Aseprite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;

namespace Ohko.Core;

public class Hero(Vector2 position)
{
    private readonly Dictionary<State, AnimatedSprite> _animations = new();
    private GraphicsDevice _graphicsDevice = null!;

    private AnimatedSprite _currentAnimation => _animations[CurrentState];

    public State CurrentState
    {
        get;

        set
        {
            if (field != value && (int)field != 0)
            {
                _animations[field].Stop();
                _animations[field].Reset();
            }

            field = value;
            _animations[field].Play();
        }
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        var file = content.Load<AsepriteFile>("entities");
        var spriteSheet = file.CreateSpriteSheet(graphicsDevice, onlyVisibleLayers: true);

        _animations[State.Idle] = spriteSheet.CreateAnimatedSprite("kIdle");
        CurrentState = State.Idle;
    }

    public void Update(GameTime gameTime)
    {
        _currentAnimation.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // var scale = 1 / ((float)_currentAnimation.CurrentFrame.TextureRegion.Bounds.Size.X / _graphicsDevice.Viewport.Width);
        var scale = _graphicsDevice.Viewport.Width / 50;
        var spritePosition = position - (_currentAnimation.CurrentFrame.TextureRegion.Bounds.Size.ToVector2() / 2 * scale);
        spriteBatch.Draw(
            _currentAnimation.TextureRegion,
            spritePosition,
            _currentAnimation.Color * _currentAnimation.Transparency,
            _currentAnimation.Rotation,
            Vector2.Zero,
            _currentAnimation.Scale * scale,
            _currentAnimation.SpriteEffects,
            layerDepth: 1);
    }

    public enum State
    {
        Idle = 1,
    }
}