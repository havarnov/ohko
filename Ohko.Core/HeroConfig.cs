using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AsepriteDotNet.Aseprite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

public abstract class HeroConfig
{
    public required Body Body { get; init; }

    public Vector2 Position
    {
        get => Body.Position.Into();
        set => Body.Position = value.Into();
    }

    public Vector2 Velocity { get; set; } = Vector2.Zero;

    public readonly Dictionary<string, Animation> _animations = new();

    public static Vector2 CenterOffset(
        Rectangle a,
        Rectangle b)
    {
        // Center of A
        var aCenterX = a.Left + a.Width * 0.5f;
        var aCenterY = a.Top + a.Height * 0.5f;

        // Center of B
        var bCenterX = b.Left + b.Width * 0.5f;
        var bCenterY = b.Top + b.Height * 0.5f;

        // Vector from A -> B
        var dx = bCenterX - aCenterX;
        var dy = bCenterY - aCenterY;

        return new Vector2(dx, dy);
    }

    public string CurrentAnimation
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is not null)
            {
                _animations[field].AnimatedSprite.Stop();
                _animations[field].AnimatedSprite.Reset();
                _animations[field].AnimatedSprite.OnFrameEnd = null;
                _animations[field].AnimatedSprite.OnFrameBegin = null;
                _animations[field].AnimatedSprite.OnAnimationBegin = null;
            }

            field = value;

            var currentAnimation = _animations[field];
            currentAnimation.AnimatedSprite.OnFrameBegin += _ =>
            {
                foreach (var fixture in Body.FixtureList.ToArray())
                {
                    Body.Remove(fixture);
                }

                foreach (var frameConfig in currentAnimation.CurrentFrame)
                {
                    switch (frameConfig)
                    {
                        case CollisionBoxConfig collisionBox:
                            Body.CreateRectangle(
                                collisionBox.Rectangle.Size.X,
                                collisionBox.Rectangle.Size.Y,
                                1f,
                                CenterOffset(new Rectangle(0, 0, currentAnimation.AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Width, currentAnimation.AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Height),
                                    _isFacingLeft
                                    ? new Rectangle(currentAnimation.AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Width - collisionBox.Rectangle.X - collisionBox.Rectangle.Width, collisionBox.Rectangle.Y, collisionBox.Rectangle.Width, collisionBox.Rectangle.Height)
                                    : collisionBox.Rectangle)
                                    .Into());
                            break;
                    }
                }
            };

            currentAnimation.AnimatedSprite.FlipHorizontally = _isFacingLeft;
            currentAnimation.AnimatedSprite.Stop();
            currentAnimation.AnimatedSprite.Reset();

            currentAnimation.AnimatedSprite.Play();

            currentAnimation.AnimatedSprite.OnAnimationBegin = _ =>
            {
                var start = currentAnimation.AnimatedSprite.CurrentFrame.FrameIndex;
                var count = currentAnimation.AnimatedSprite.FrameCount;
                var end = start + count - 1;
                currentAnimation.AnimatedSprite.OnFrameEnd += _ =>
                {
                    if (currentAnimation.AnimatedSprite.CurrentFrame.FrameIndex == end)
                    {
                        CurrentAnimation = currentAnimation.AutomaticContinuation ?? GetIdleAnimation();
                    }
                };
            };
        }
    }

    public abstract string Name { get; }
    protected abstract string AsepriteFile { get; }
    protected abstract string AsepriteConfigFile { get; }
    protected abstract string GetIdleAnimation();
    protected abstract Dictionary<string, string?> AutomaticContinuations { get; }
    protected abstract List<ComboConfig> ComboConfigs { get; }
    public IEntity Face { get; set; } = null!;

    public bool _isFacingLeft
    {
        get;
        set
        {
            _animations[CurrentAnimation].AnimatedSprite.FlipHorizontally = value;
            field = value;
        }
    } = false;

    public void Load(ContentManager content, GraphicsDevice graphicsDevice)
    {
        var model = JsonSerializer.Deserialize<AsepriteFrameConfigFileDeserializationModel>(
                        File.ReadAllText($"Content/{AsepriteConfigFile}"),
                        new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true,
                        })
                    ?? throw new InvalidOperationException();

        var file = content.Load<AsepriteFile>(AsepriteFile);
        var spriteSheet = file.CreateSpriteSheet(graphicsDevice, onlyVisibleLayers: true);

        foreach (var tag in file.Tags)
        {
            var animatedSprite = spriteSheet.CreateAnimatedSprite(tag.Name);
            var frameConfigDict = new Dictionary<int, List<FrameConfig>>();

            var relevant =
                model.UserModels?
                    .Where(u =>
                        u.Frames?.Any(f => f.FrameIndex >= tag.From && f.FrameIndex <= tag.To) == true)
                ?? [];

            foreach (var userModel in relevant)
            {
                if (userModel.Value is null)
                {
                    continue;
                }

                var userModelData = userModel.Value.Value.Deserialize<UserDataValue>();

                foreach (var frame in userModel.Frames?.Where(f => f.FrameIndex >= tag.From && f.FrameIndex <= tag.To) ?? [])
                {
                    if (!frameConfigDict.TryGetValue(frame.FrameIndex, out var configs))
                    {
                        configs = [];
                        frameConfigDict[frame.FrameIndex] = configs;
                    }

                    switch (userModelData)
                    {
                        case CollisionUserDataValue:
                            if (frame.Rectangle is null)
                            {
                                throw new Exception("Collision user data doesn't have a rectangle");
                            }

                            configs.Add(new CollisionBoxConfig
                            {
                                Rectangle = new Rectangle(frame.Rectangle.X, frame.Rectangle.Y,  frame.Rectangle.Width, frame.Rectangle.Height),
                            });
                            break;
                        case MoveEffectUserDataValue moveEffect:
                            configs.Add(new MoveEffectConfig
                            {
                                Vector = moveEffect.Vector,
                                Speed = moveEffect.Speed,
                            });
                            break;
                        case HitBoxUserDataValue hitBox:
                            if (frame.Rectangle is null)
                            {
                                throw new Exception("HitBox user data doesn't have a rectangle");
                            }

                            configs.Add(new HitBoxConfig
                            {
                                Rectangle = new Rectangle(frame.Rectangle.X, frame.Rectangle.Y,  frame.Rectangle.Width, frame.Rectangle.Height),
                            });
                            break;
                        case HurtBoxUserDataValue hurtBox:
                            if (frame.Rectangle is null)
                            {
                                throw new Exception("HurtBox user data doesn't have a rectangle");
                            }

                            configs.Add(new HurtBoxConfig
                            {
                                Rectangle = new Rectangle(frame.Rectangle.X, frame.Rectangle.Y,  frame.Rectangle.Width, frame.Rectangle.Height),
                            });
                            break;
                        default:
                            throw new ArgumentException($"Unknown user data type: {userModelData}");
                    }
                }
            }

            _animations[tag.Name] = new Animation
            {
                AnimatedSprite = animatedSprite,
                FrameConfigs = frameConfigDict,
                AutomaticContinuation = AutomaticContinuations.GetValueOrDefault(tag.Name),
                HeroConfig = this,
            };
        }

        CurrentAnimation = GetIdleAnimation();
    }

    private bool lockOrientation = false;

    public void Update(GameTime gameTime)
    {
        lockOrientation = false;
        Velocity = Vector2.Zero;

        if (_knockbackVector is not null && _knockbackTimeRemaining > TimeSpan.Zero)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_knockbackTimeRemaining > TimeSpan.Zero)
            {
                Console.WriteLine("knocked: " + _knockbackTimeRemaining);
                Velocity += _knockbackVector.Value * dt;
                _knockbackTimeRemaining -= gameTime.ElapsedGameTime;
                if (_knockbackTimeRemaining < TimeSpan.Zero)
                {
                    _knockbackTimeRemaining = TimeSpan.Zero;
                }
            }
        }

        foreach (var moveEffect in _animations[CurrentAnimation].Current<MoveEffectConfig>())
        {
            var vector = moveEffect.Vector * new Vector2(_isFacingLeft ? -1 : 1, 1);
            vector.Normalize();
            Velocity += vector * moveEffect.Speed * 0.7f;
            lockOrientation = true;
        }

        if (!lockOrientation)
        {
            if (Face.Position.X < Position.X)
            {
                _isFacingLeft = true;
            }
            else
            {
                _isFacingLeft = false;
            }
        }

        if (Velocity != Vector2.Zero)
        {
            Body.Position += Velocity.Into();
        }
        else
        {
            Body.Position = new Vector2(
                    Body.Position.X,
                    (float)(Body.Position.Y + gameTime.ElapsedGameTime.TotalSeconds * 50f))
                .Into();
        }

        _animations[CurrentAnimation].AnimatedSprite.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var animation = _animations[CurrentAnimation];
        var spritePosition = Position - (animation.AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Size.ToVector2() / 2);
        spriteBatch.Draw(
            animation.AnimatedSprite.TextureRegion,
            spritePosition,
            animation.AnimatedSprite.Color * animation.AnimatedSprite.Transparency,
            animation.AnimatedSprite.Rotation,
            Vector2.Zero,
            animation.AnimatedSprite.Scale,
            animation.AnimatedSprite.SpriteEffects,
            layerDepth: 0.8f);
    }

    public void Apply(List<ControlPad.ButtonPosition> result)
    {
        if (ComboConfigs.FirstOrDefault(c => ToUInt128(c.Buttons) == ToUInt128(result)) is { } comboConfig)
        {
            CurrentAnimation = comboConfig.AnimationName;
        }
    }

     private Vector2? _knockbackVector = null;
     private TimeSpan _knockbackTimeRemaining = TimeSpan.Zero;

     public void Hit(List<HitBoxConfig> hitBoxes)
     {
         Vector2? newKnockbackVector = null;
         foreach (var hitBox in hitBoxes)
         {
             foreach (var hurtBox in _animations[CurrentAnimation].Current<HurtBoxConfig>())
             {
                 if (hurtBox.Rectangle.Intersects(hitBox.Rectangle))
                 {
                     var top = Math.Max(hitBox.Rectangle.Top, hurtBox.Rectangle.Top);
                     var bottom = Math.Min(hitBox.Rectangle.Bottom, hurtBox.Rectangle.Bottom);
                     var yP = Math.Abs(bottom - top) / (float)hurtBox.Rectangle.Height;

                     var left = Math.Max(hitBox.Rectangle.Left, hitBox.Rectangle.Left);
                     var right = Math.Min(hitBox.Rectangle.Right, hitBox.Rectangle.Right);
                     var xP = Math.Abs(right - left) / (float)hitBox.Rectangle.Width;
                     if (!_isFacingLeft)
                     {
                         xP = -xP;
                     }

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

    public List<HitBoxConfig> GetHitBoxes()
    {
        return _animations[CurrentAnimation].Current<HitBoxConfig>().ToList();
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CollisionUserDataValue), typeDiscriminator: "CollisionBox")]
[JsonDerivedType(typeof(HitBoxUserDataValue), typeDiscriminator: "HitBox")]
[JsonDerivedType(typeof(HurtBoxUserDataValue), typeDiscriminator: "HurtBox")]
[JsonDerivedType(typeof(MoveEffectUserDataValue), typeDiscriminator: "MoveEffect")]
public abstract class UserDataValue;

public class CollisionUserDataValue : UserDataValue;
public class HitBoxUserDataValue : UserDataValue;
public class HurtBoxUserDataValue : UserDataValue;

public class MoveEffectUserDataValue : UserDataValue
{
    [JsonConverter(typeof(Vector2JsonConverter))]
    public required Vector2 Vector { get; init; }
    public required float Speed { get; init; }
}

public class ComboConfig
{
    public required List<ControlPad.ButtonPosition> Buttons { get; init; }
    public required string AnimationName { get; init; }
}

public class Animation
{
    public required AnimatedSprite AnimatedSprite { get; init; }
    public required Dictionary<int, List<FrameConfig>> FrameConfigs { get; init; }
    public required string? AutomaticContinuation { get; init; }
    public required HeroConfig HeroConfig { get; init; }

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
                var size = HeroConfig._animations[HeroConfig.CurrentAnimation].AnimatedSprite.CurrentFrame.TextureRegion.Bounds.Size;
                 var offset = size.ToVector2() / 2f;
                 offset = new Vector2((float)Math.Ceiling(offset.X), (float)Math.Floor(offset.Y));

                 var location = HeroConfig._isFacingLeft
                     ? new Point(size.X - bc.Rectangle.Location.X - bc.Rectangle.Size.X, bc.Rectangle.Location.Y)
                     : bc.Rectangle.Location;

                 var position = (HeroConfig.Position - offset);
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

public abstract record FrameConfig;

public abstract record BoxConfig : FrameConfig
{
    public required Rectangle Rectangle { get; init; }
}

public record CollisionBoxConfig : BoxConfig;

public record HitBoxConfig : BoxConfig;

public record HurtBoxConfig : BoxConfig;

public record MoveEffectConfig : FrameConfig
{
    public required Vector2 Vector { get; init; }
    public required float Speed { get; init; }
}

public class KarateConfig : HeroConfig
{
    public override string Name => "Karate";
    protected override string AsepriteFile => "karate";
    protected override string AsepriteConfigFile => "karate.json";
    protected override string GetIdleAnimation() => "kIdle";

    protected override Dictionary<string, string?> AutomaticContinuations => new()
    {
        { "kPunchA_charge", "kPunchA_hit" },
        { "kPunchA_hit", null },
        { "kKickA_charge", "kKickA_hit" },
        { "kKickA_hit", null },
    };

    protected override List<ComboConfig> ComboConfigs =>
    [
        new()
        {
            Buttons = [ControlPad.ButtonPosition.Center, ControlPad.ButtonPosition.MiddleRight],
            AnimationName = "kPunchA_charge",
        },
        new()
        {
            Buttons = [
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleLeft,
                ControlPad.ButtonPosition.Center,
                ControlPad.ButtonPosition.MiddleRight,
                ControlPad.ButtonPosition.TopRight
            ],
            AnimationName = "kKickA_charge",
        },
    ];
}