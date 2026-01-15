using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
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

        Opponent.Hit(_heroConfig.GetHitBoxes());

        _heroConfig.Update(gameTime);
    }

    private void Hit(List<HitBoxConfig> hitBoxes)
    {
        _heroConfig.Hit(hitBoxes);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _heroConfig.Draw(spriteBatch);
    }

    internal void AddCombo(List<ControlPad.ButtonPosition> combo)
    {
        _comboQueue.Enqueue(combo);
    }
}