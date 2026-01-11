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

public abstract class HeroConfig
{
    public required Body Body { get; init; }

    public Vector2 Position
    {
        get => Body.Position.Into();
        set => Body.Position = value.Into();
    }

    private readonly Dictionary<string, Animation> _animations = new();

    private string CurrentAnimation
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            Console.WriteLine(value);

            if (field is not null)
            {
                _animations[field].AnimatedSprite.Stop();
                _animations[field].AnimatedSprite.Reset();
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
                                Vector2.Zero.Into());
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            };

            // currentAnimation.AnimatedSprite.FlipHorizontally = _isFacingLeft;
            currentAnimation.AnimatedSprite.Stop();
            currentAnimation.AnimatedSprite.Reset();

            currentAnimation.AnimatedSprite.Play();

            currentAnimation.AnimatedSprite.OnAnimationBegin = _ =>
            {
                var start = currentAnimation.AnimatedSprite.CurrentFrame.FrameIndex;
                var count = currentAnimation.AnimatedSprite.FrameCount;
                var end = start + count - 1;
                currentAnimation.AnimatedSprite.OnFrameBegin += _ =>
                {
                    if (currentAnimation.AnimatedSprite.CurrentFrame.FrameIndex == end)
                    {
                        CurrentAnimation = currentAnimation.AutomaticContinuation ?? GetIdleAnimation();
                        Console.WriteLine("FROM: " + CurrentAnimation);
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
                foreach (var frame in userModel.Frames?.Where(f => f.FrameIndex >= tag.From && f.FrameIndex <= tag.To) ?? [])
                {
                    if (!frameConfigDict.TryGetValue(frame.FrameIndex, out var configs))
                    {
                        configs = [];
                        frameConfigDict[frame.FrameIndex] = configs;
                    }

                    if (frame.Rectangle is null)
                    {
                        throw new NotImplementedException();
                    }

                    configs.Add(new CollisionBoxConfig
                    {
                        Rectangle = new Rectangle(frame.Rectangle.X, frame.Rectangle.Y,  frame.Rectangle.Width, frame.Rectangle.Height),
                    });
                }
            }

            _animations[tag.Name] = new Animation
            {
                AnimatedSprite = animatedSprite,
                FrameConfigs = frameConfigDict,
                AutomaticContinuation = AutomaticContinuations.GetValueOrDefault(tag.Name),
            };
        }
    }


    public void Update(GameTime gameTime)
    {
        if (CurrentAnimation is null)
        {
            CurrentAnimation = GetIdleAnimation();
        }

        _animations[CurrentAnimation].AnimatedSprite.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var idle = GetIdleAnimation();
        var animation = _animations[idle];
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

    public List<FrameConfig> CurrentFrame =>
        FrameConfigs.TryGetValue(AnimatedSprite.CurrentFrame.FrameIndex, out var frames)
            ? frames
            : [];
}

public abstract class FrameConfig;

public abstract class BoxConfig : FrameConfig
{
    public required Rectangle Rectangle { get; init; }
}

public class CollisionBoxConfig : BoxConfig;

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
    };

    protected override List<ComboConfig> ComboConfigs =>
    [
        new ComboConfig
        {
            Buttons = [ControlPad.ButtonPosition.Center, ControlPad.ButtonPosition.MiddleRight],
            AnimationName = "kPunchA_charge",
        },
    ];
}