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
using nkast.Aether.Physics2D.Dynamics;

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

public class Hero : IEntity
{
    public Hero Opponent { get; set; } = null!;

    public Hero(World world)
    {
        var body = world.CreateBody(Vector2.Zero.Into(), 0f, BodyType.Dynamic);
        body.FixedRotation = true;
        _heroConfig = new KarateConfig
        {
            Body = body,
        };
    }

    private readonly HeroConfig _heroConfig;
    private readonly Queue<List<ControlPad.ButtonPosition>> _comboQueue = new();
    private GraphicsDevice _graphicsDevice = null!;

    public IEntity Face
    {
        set => _heroConfig.Face = value;
    }

    public Vector2 Position
    {
        get => _heroConfig.Position;
        set => _heroConfig.Position = value;
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _heroConfig.Load(content, graphicsDevice);
    }

    public void Update(GameTime gameTime)
    {
        if (_comboQueue.TryDequeue(out var combo))
        {
            _heroConfig.Apply(combo);
        }

        Opponent.Hit(Boxes.Where(b => b is Box.HitBox).Cast<Box.HitBox>().ToList());

        var velocity = Vector2.Zero;

        // if (_knockbackVector is not null && _knockbackTimeRemaining > TimeSpan.Zero)
        // {
        //     if (_heroConfig.CurrentState != State.MajorHit)
        //     {
        //         _heroConfig.CurrentState = State.MajorHit;
        //     }
        //
        //     var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //
        //     if (_knockbackTimeRemaining > TimeSpan.Zero)
        //     {
        //         velocity += _knockbackVector.Value * dt;
        //         _knockbackTimeRemaining -= gameTime.ElapsedGameTime;
        //         if (_knockbackTimeRemaining < TimeSpan.Zero)
        //         {
        //             _knockbackTimeRemaining = TimeSpan.Zero;
        //         }
        //     }
        // }
        //
        // foreach (var effect in  _heroConfig.CurrentFrameConfiguration?.Effects ?? [])
        // {
        //     if (effect is Effect.MoveEffect moveEffect)
        //     {
        //         var vector = moveEffect.Vector;
        //         vector.Normalize();
        //         velocity += vector * moveEffect.SpeedFactor * 1;
        //     }
        // }

        // GRAVITY-ISH.
        // if (velocity != Vector2.Zero)
        // {
        //     _heroConfig.Position += velocity;
        // }
        // else
        // {
        //     _heroConfig.Position = new Vector2(_heroConfig.Position.X, _heroConfig.Position.Y + (float)(50f * gameTime.ElapsedGameTime.TotalSeconds));
        // }

        _heroConfig.Update(gameTime);

        // Assume not grounded, will be updated on collision tests.
        // isGrounded = false;
    }

    private Vector2? _knockbackVector = null;
    private TimeSpan _knockbackTimeRemaining = TimeSpan.Zero;

    private void Hit(List<Box.HitBox> hitBoxes)
    {
        Vector2? newKnockbackVector = null;
        foreach (var hitBox in hitBoxes)
        {
            foreach (var hurtBox in Boxes.Where(b => b is Box.HurtBox))
            {
                if (hurtBox.Rectangle.Intersects(hitBox.Rectangle))
                {
                    var top = Math.Max(hitBox.Rectangle.Top, hurtBox.Rectangle.Top);
                    var bottom = Math.Min(hitBox.Rectangle.Bottom, hurtBox.Rectangle.Bottom);
                    var yP = Math.Abs(bottom - top) / (float)hurtBox.Rectangle.Height;

                    var left = Math.Max(hitBox.Rectangle.Left, hitBox.Rectangle.Left);
                    var right = Math.Min(hitBox.Rectangle.Right, hitBox.Rectangle.Right);
                    var xP = Math.Abs(right - left) / (float)hitBox.Rectangle.Width;
                    // if (!_heroConfig._isFacingLeft)
                    // {
                    //     xP = -xP;
                    // }

                    var v = new Vector2(xP, -yP);

                    if (newKnockbackVector is null)
                    {
                        newKnockbackVector = v;
                    }
                    else
                    {
                        newKnockbackVector += v;
                    }
                }
            }
        }

        if (newKnockbackVector is not null)
        {
            newKnockbackVector?.Normalize();
            newKnockbackVector *= new Vector2(300, 500);
            // 60fps == 0.06 fpms == 16.67 mspf
            _knockbackTimeRemaining = TimeSpan.FromMilliseconds(3 * 16.67);
            _knockbackVector = newKnockbackVector;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _heroConfig.Draw(spriteBatch);
    }

    public List<Box> Boxes => [];

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
        MajorHit = 11,
    }

    internal void AddCombo(List<ControlPad.ButtonPosition> combo)
    {
        _comboQueue.Enqueue(combo);
    }

    // private readonly Dictionary<UInt128, State> _combos = new()
    // {
    //     {
    //         ToUInt128([
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleLeft]),
    //         State.Back
    //     },
    //     {
    //         ToUInt128([
    //         ControlPad.ButtonPosition.Center,
    //         ControlPad.ButtonPosition.MiddleRight]),
    //         State.PunchACharge
    //     },
    //     {
    //         ToUInt128([
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleLeft,
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleRight,
    //         ]),
    //         State.PunchBCharge
    //     },
    //     {
    //         ToUInt128([
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleLeft,
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleRight,
    //             ControlPad.ButtonPosition.BottomRight,
    //         ]),
    //         State.PunchCCharge
    //     },
    //     {
    //         ToUInt128([
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleLeft,
    //             ControlPad.ButtonPosition.Center,
    //             ControlPad.ButtonPosition.MiddleRight,
    //             ControlPad.ButtonPosition.TopRight,
    //         ]),
    //         State.KickACharge
    //     },
    // };
    //
    // private static UInt128 ToUInt128(List<ControlPad.ButtonPosition> combo)
    // {
    //     UInt128 result = 0;
    //     var idx = 1;
    //     foreach (var position in combo)
    //     {
    //         result |= (((UInt128)(int)position) << (9 * idx));
    //         idx++;
    //     }
    //
    //     return result;
    // }
    //
    // private bool isGrounded = false;
}