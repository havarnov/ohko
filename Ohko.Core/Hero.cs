using System;
using System.Collections.Generic;
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
    private readonly Queue<State>_comboQueue = new();

    public State CurrentState
    {
        get;

        set
        {
            if (field != value && field != State.Unknown)
            {
                _animations[field].Stop();
                _animations[field].Reset();
            }

            field = value;
            _animations[field].Play();

            if (_continuation.TryGetValue(field, out var continuation))
            {
                _currentAnimation.OnFrameEnd += _ =>
                {
                    CurrentState = continuation;
                    _currentAnimation.OnAnimationLoop += _ =>
                    {
                        CurrentState = State.Idle;
                    };
                };
            }
        }
    }

    private readonly Dictionary<State, State> _continuation = new()
    {
        { State.PunchACharge, State.PunchA },
        { State.PunchBCharge, State.PunchB },
        { State.PunchCCharge, State.PunchC },
        { State.KickACharge, State.KickA },
    };

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        var file = content.Load<AsepriteFile>("entities");
        var spriteSheet = file.CreateSpriteSheet(graphicsDevice, onlyVisibleLayers: true);

        _animations[State.Idle] = spriteSheet.CreateAnimatedSprite("kIdle");
        _animations[State.PunchACharge] = spriteSheet.CreateAnimatedSprite("kPunchA_charge");
        _animations[State.PunchA] = spriteSheet.CreateAnimatedSprite("kPunchA_hit");
        _animations[State.PunchBCharge] = spriteSheet.CreateAnimatedSprite("kPunchB_charge");
        _animations[State.PunchB] = spriteSheet.CreateAnimatedSprite("kPunchB_hit");
        _animations[State.PunchCCharge] = spriteSheet.CreateAnimatedSprite("kPunchC_charge");
        _animations[State.PunchC] = spriteSheet.CreateAnimatedSprite("kPunchC_hit");
        _animations[State.KickACharge] = spriteSheet.CreateAnimatedSprite("kKickA_charge");
        _animations[State.KickA] = spriteSheet.CreateAnimatedSprite("kKickA_hit");
        CurrentState = State.Idle;
    }

    public void Update(GameTime gameTime)
    {
        if (_comboQueue.TryDequeue(out var combo))
        {
            CurrentState = combo;
        }

        _currentAnimation.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
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
        Unknown = 0,
        Idle = 1,
        PunchACharge = 2,
        PunchA = 3,
        PunchBCharge = 4,
        PunchB = 5,
        PunchCCharge = 6,
        PunchC = 7,
        KickACharge = 8,
        KickA = 9,
    }

    internal void AddCombo(List<ControlPad.ButtonPosition> combo)
    {
        if (_comboes.TryGetValue(ToUInt128(combo), out var state))
        {
            _comboQueue.Enqueue(state);
        }
    }

    private Dictionary<UInt128, State> _comboes = new()
    {
        { ToUInt128([
            ControlPad.ButtonPosition.Center,
            ControlPad.ButtonPosition.MiddleRight]),
            State.PunchACharge
        },
        {
            ToUInt128([
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft,
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleRight,
            ]),
            State.PunchBCharge
        },
        {
            ToUInt128([
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft,
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleRight,
                ControlPad.ButtonPosition.BottomRight,
            ]),
            State.PunchCCharge
        },
        {
            ToUInt128([
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft,
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleRight,
                ControlPad.ButtonPosition.TopRight,
            ]),
            State.KickACharge
        },
    };

    private static UInt128 ToUInt128(List<ControlPad.ButtonPosition> combo)
    {
        UInt128 result = 0;
        var idx = 1;
        foreach (var position in combo)
        {
            result |= (((UInt128)(int)position) << (9 * idx));
            idx++;
        }

        return result;
    }
}