using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AsepriteDotNet.Aseprite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

public class StateManager<TState>(TState initialState, bool isFacingLeft, Body body)
    where TState : struct, Enum
{
    public Vector2 Position
    {
        get => body.Position.Into();
        set => body.Position = value.Into();
    }

    public TState CurrentState
    {
        get => field;
        set
        {
            if (Enum.IsDefined(field))
            {
                var previousStateInfo = _states[field];
                previousStateInfo.Animation.Stop();
                previousStateInfo.Animation.Reset();
            }

            field = value;
            var stateInfo = _states[field];
            stateInfo.Animation.OnFrameBegin += _ =>
            {
                foreach (var fixture in body.FixtureList.ToArray())
                {
                    body.Remove(fixture);
                }

                foreach (var box in CurrentFrameConfiguration?.Boxes ?? [])
                {
                    if (box is Box.CollisionBox { CollisionTag: "wall" } collisionBox)
                    {
                        body.CreateRectangle(
                            collisionBox.Rectangle.Size.X,
                            collisionBox.Rectangle.Size.Y,
                            1f,
                            Vector2.Zero.Into());
                    }
                }
            };

            stateInfo.Animation.FlipHorizontally = isFacingLeft;
            stateInfo.Animation.Stop();
            stateInfo.Animation.Reset();

            stateInfo.Animation.Play();

            if (stateInfo.AutomaticContinuation is not null)
            {
                stateInfo.Animation.OnAnimationBegin += _ =>
                {
                    var start = stateInfo.Animation.CurrentFrame.FrameIndex;
                    var count = stateInfo.Animation.FrameCount;
                    var end = start + count - 1;
                    stateInfo.Animation.OnFrameBegin += _ =>
                    {
                        if (stateInfo.Animation.CurrentFrame.FrameIndex == end)
                        {
                            CurrentState = stateInfo.AutomaticContinuation.Value;
                        }
                    };
                };
            }
        }
    }

    private StateInfo CurrentStateInfo => _states[CurrentState];
    public FrameConfiguration? CurrentFrameConfiguration
    {
        get
        {
            var current = CurrentStateInfo.Animation.CurrentFrame.FrameIndex - CurrentStateInfo.AnimationStartFrame;
            var all = CurrentStateInfo.AllFrames;
            var currentFrame = CurrentStateInfo.Frames.GetValueOrDefault(current);
            if (all is not null || currentFrame is not null)
            {
                return new FrameConfiguration
                {
                    Effects = (currentFrame?.Effects ?? [])
                            .Concat(all?.Effects ?? [])
                            .Select(i => i switch {
                                Effect.MoveEffect moveEffect => (Effect)new Effect.MoveEffect()
                                {
                                    SpeedFactor = moveEffect.SpeedFactor,
                                    Vector = moveEffect.Vector * new Vector2(isFacingLeft ? -1 : 1, 1),
                                },
                                _ => throw new ArgumentOutOfRangeException(nameof(i))
                            })
                            .ToList(),
                    Boxes = (currentFrame?.Boxes ?? [])
                        .Concat(all?.Boxes ?? [])
                        // .Select(b => b switch
                        // {
                        //     Box.CollisionBox collisionBox => (Box)new Box.CollisionBox()
                        //     {
                        //         CollisionTag = collisionBox.CollisionTag,
                        //         Rectangle = new Rectangle(
                        //             (Position
                        //              - (_states[CurrentState].Animation.CurrentFrame.TextureRegion.Bounds.Size
                        //                  .ToVector2() / 2))
                        //             .ToPoint()
                        //             + collisionBox.Rectangle.Location,
                        //             collisionBox.Rectangle.Size),
                        //     },
                        //     _ => throw new ArgumentOutOfRangeException(nameof(b))
                        // })
                        .ToList(),
                };
            }
            else
            {
                return null;
            }
        }
    }

    private readonly Dictionary<TState, StateInfo> _states = new();

    public void Load(ContentManager content, GraphicsDevice graphicsDevice)
    {
        var statesConfiguration = JsonSerializer.Deserialize<StatesConfiguration>(
            File.ReadAllText("Content/states.json"),
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            })
            ?? throw new InvalidOperationException();

        foreach (var state in Enum.GetValues<TState>())
        {
            if (!statesConfiguration.States.TryGetValue(state.ToString(), out var stateConfig))
            {
                throw new InvalidOperationException();
            }

            var file = content.Load<AsepriteFile>(stateConfig.AnimationName);
            var spriteSheet = file.CreateSpriteSheet(graphicsDevice, onlyVisibleLayers: true);
            var animatedSprite = spriteSheet.CreateAnimatedSprite(stateConfig.AnimationTag);
            var stateInfo = new StateInfo()
            {
                Animation = animatedSprite,
                AnimationStartFrame = animatedSprite.CurrentFrame.FrameIndex,
                Frames = stateConfig.Frames
                    .Where(kv => kv.Key != "all")
                    .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value),
                AllFrames = stateConfig.Frames.GetValueOrDefault("all"),
                AutomaticContinuation = stateConfig.AutomaticContinuation is not null
                    ? Enum.Parse<TState>(stateConfig.AutomaticContinuation)
                    : null,
            };
            _states.Add(state, stateInfo);
        }

        CurrentState = initialState;
    }

    private class StateInfo
    {
        public required AnimatedSprite Animation { get; init; }
        public required int AnimationStartFrame { get; init; }
        public required Dictionary<int, FrameConfiguration> Frames { get; init; }
        public required TState? AutomaticContinuation { get; init; }
        public FrameConfiguration? AllFrames { get; set; }
    }

    public void Update(GameTime gameTime)
    {
        _states[CurrentState].Animation.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var spritePosition = Position - (_states[CurrentState].Animation.CurrentFrame.TextureRegion.Bounds.Size.ToVector2() / 2);
        spriteBatch.Draw(
            _states[CurrentState].Animation.TextureRegion,
            spritePosition,
            _states[CurrentState].Animation.Color * _states[CurrentState].Animation.Transparency,
            _states[CurrentState].Animation.Rotation,
            Vector2.Zero,
            _states[CurrentState].Animation.Scale,
            _states[CurrentState].Animation.SpriteEffects,
            layerDepth: 0.8f);
    }
}

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

public class Hero : IEntity
{
    public Hero(World world)
    {
        var body = world.CreateBody(Vector2.Zero.Into(), 0f, BodyType.Dynamic);
        body.FixedRotation = true;
        _stateManager = new(State.Idle, isFacingLeft: false, body);
    }

    private readonly StateManager<State> _stateManager;
    private readonly Queue<State>_comboQueue = new();
    private GraphicsDevice _graphicsDevice = null!;

    public Vector2 Position
    {
        get => _stateManager.Position;
        set => _stateManager.Position = value;
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        var states = JsonSerializer.Deserialize<StatesConfiguration>(
            File.ReadAllText("Content/states.json"),
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            });

        _graphicsDevice = graphicsDevice;

        _stateManager.Load(content, graphicsDevice);
    }

    public void Update(GameTime gameTime)
    {
        if (!isGrounded)
        {
        }

        if (_comboQueue.TryDequeue(out var combo))
        {
            _stateManager.CurrentState = combo;
        }

        bool anyEffects = false;
        foreach (var effect in  _stateManager.CurrentFrameConfiguration?.Effects ?? [])
        {
            if (effect is Effect.MoveEffect moveEffect)
            {
                var vector = moveEffect.Vector;
                vector.Normalize();
                _stateManager.Position += (vector * moveEffect.SpeedFactor * 1);
                anyEffects = true;
            }
        }

        if (!anyEffects)
        {
            _stateManager.Position = new Vector2(_stateManager.Position.X, _stateManager.Position.Y + (float)(50f * gameTime.ElapsedGameTime.TotalSeconds));
        }

        _stateManager.Update(gameTime);

        // Assume not grounded, will be updated on collision tests.
        isGrounded = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _stateManager.Draw(spriteBatch);
    }

    public List<Box> Boxes => _stateManager.CurrentFrameConfiguration?.Boxes ?? [];

    public void OnCollision(IEntity otherEntity, Box own, Box other)
    {
    }

    public enum State
    {
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

    private bool isGrounded = false;
}