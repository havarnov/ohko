using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HurtBox), typeDiscriminator: nameof(HurtBox))]
[JsonDerivedType(typeof(CollisionBox), typeDiscriminator: nameof(CollisionBox))]
[JsonDerivedType(typeof(HitBox), typeDiscriminator: nameof(HitBox))]
public abstract class AnimationBoxData
{
    private AnimationBoxData()
    {
    }

    public class HitBox : AnimationBoxData;

    public class HurtBox : AnimationBoxData
    {
        [JsonPropertyName("damage_multiplier")]
        public required float DamageMultiplier { get; init; }
    }

    public class CollisionBox : AnimationBoxData;
}

public class Hero
{
    private readonly Dictionary<State, AnimatedSpriteExtended<AnimationBoxData>> _animations = new();

    private AnimatedSpriteExtended<AnimationBoxData> _currentAnimation => _animations[CurrentState];
    private readonly Queue<State>_comboQueue = new();

    public Vector2 Position
    {
        get => body.Position.ToVector2();
        set => body.Position = value.ToVector2();
    }

    private readonly Body body;

    public Hero(World world)
    {
        body = world.CreateBody(Vector2.Zero.ToVector2(), bodyType: BodyType.Dynamic);
        body.FixedRotation = true;
    }

    private void UpdateBoxes()
    {
        foreach (var fixture in body.FixtureList.ToList())
        {
            body.Remove(fixture);
        }

        foreach (var key in _currentAnimation.CurrentSliceKeys)
        {
            var density = key.UserData is AnimationBoxData.CollisionBox
                ? 1f
                : 0f;
            // var center = _currentAnimation.AnimatedSprite.TextureRegion.Bounds.Center;
            var center = new Point(16, 16);
            var xx = key.SliceKey.Bounds.X + key.SliceKey.Bounds.Width / 2f;
            var yy = key.SliceKey.Bounds.Y + key.SliceKey.Bounds.Height / 2f;
            var fixture = body.CreateRectangle(
                key.SliceKey.Bounds.Width,
                key.SliceKey.Bounds.Height,
                density,
                offset: new nkast.Aether.Physics2D.Common.Vector2(xx - center.X, yy - center.Y));
            fixture.Tag = key.SliceName;

            if (key.UserData is AnimationBoxData.HurtBox)
            {
                fixture.CollidesWith = Category.Cat3;
                fixture.CollisionCategories = Category.Cat2;
            }
            else if (key.UserData is AnimationBoxData.HitBox)
            {
                fixture.CollidesWith = Category.Cat2;
                fixture.CollisionCategories = Category.Cat3;
            }
            else
            {
                fixture.CollidesWith = Category.Cat1;
            }
        }
    }

    public State CurrentState
    {
        get;

        set
        {
            if (field != value && field != State.Unknown)
            {
                _animations[field].AnimatedSprite.Stop();
                _animations[field].AnimatedSprite.Reset();
            }

            field = value;
            _animations[field].AnimatedSprite.Play();

            if (_continuation.TryGetValue(field, out var continuation))
            {
                _currentAnimation.AnimatedSprite.OnAnimationBegin += _ =>
                {
                    var start = _currentAnimation.AnimatedSprite.CurrentFrame.FrameIndex;
                    var count = _currentAnimation.AnimatedSprite.FrameCount;
                    var end = start + count - 1;
                    _currentAnimation.AnimatedSprite.OnFrameBegin += _ =>
                    {
                        if (_currentAnimation.AnimatedSprite.CurrentFrame.FrameIndex == end)
                        {
                            CurrentState = continuation;
                        }
                    };
                };
            }
        }
    }

    private readonly Dictionary<State, State> _continuation = new()
    {
        { State.PunchACharge, State.PunchA },
        { State.PunchA, State.Idle },
        { State.PunchBCharge, State.PunchB },
        { State.PunchB, State.Idle },
        { State.PunchCCharge, State.PunchC },
        { State.PunchC, State.Idle },
        { State.KickACharge, State.KickA },
        { State.KickA, State.Idle },
        { State.Back, State.Idle },
    };

    private readonly Dictionary<(State, State), Vector2> effects = new()
    {
        { (State.KickACharge, State.KickA), new Vector2(1f, -4f) },
        { (State.Idle, State.Back), new Vector2(-1f, -2f) },
    };

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _animations[State.Idle] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kIdle", content, graphicsDevice);
        _animations[State.PunchACharge] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kPunchA_charge", content, graphicsDevice);
        _animations[State.PunchA] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kPunchA_hit", content, graphicsDevice);
        _animations[State.PunchBCharge] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kPunchB_charge", content, graphicsDevice);
        _animations[State.PunchB] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kPunchB_hit", content, graphicsDevice);
        _animations[State.PunchCCharge] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kPunchC_charge", content, graphicsDevice);
        _animations[State.PunchC] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kPunchC_hit", content, graphicsDevice);
        _animations[State.KickACharge] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kKickA_charge", content, graphicsDevice);
        _animations[State.KickA] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kKickA_hit", content, graphicsDevice);
        _animations[State.Back] =
            new AnimatedSpriteExtended<AnimationBoxData>("entities", "kHit", content, graphicsDevice);
        CurrentState = State.Idle;
    }

    private State lastState = State.Unknown;

    public void Update(GameTime gameTime)
    {
        UpdateBoxes();

        if (_comboQueue.TryDequeue(out var combo))
        {
            CurrentState = combo;
        }

        if (effects.TryGetValue((lastState, CurrentState), out var effect))
        {
            float jumpSpeed = 24f;
            float jumpImpulse = body.Mass * jumpSpeed;
            effect.Normalize();
            var effectImpulse = effect *  jumpImpulse;
            body.ApplyLinearImpulse(effectImpulse.ToVector2());
        }

        lastState = CurrentState;

        _currentAnimation.AnimatedSprite.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var spritePosition = Position - (_currentAnimation.AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Size.ToVector2() / 2);
        spriteBatch.Draw(
            _currentAnimation.AnimatedSprite.TextureRegion,
            spritePosition,
            _currentAnimation.AnimatedSprite.Color * _currentAnimation.AnimatedSprite.Transparency,
            _currentAnimation.AnimatedSprite.Rotation,
            Vector2.Zero,
            _currentAnimation.AnimatedSprite.Scale,
            _currentAnimation.AnimatedSprite.SpriteEffects,
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
        Back = 10,
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
        {
            ToUInt128([
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft]),
            State.Back
        },
        {
            ToUInt128([
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