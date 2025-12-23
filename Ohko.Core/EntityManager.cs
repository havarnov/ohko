using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ohko.Core;

public interface IEntity
{

    List<Box> Boxes => [];

    void Update(GameTime gameTime)
    {
    }

    void OnCollision(IEntity otherEntity, Box own, Box other)
    {
    }

    void Draw(SpriteBatch spriteBatch)
    {
    }

    void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
    }
}

public class EntityManager
{
    private readonly List<IEntity> _entities = [];
    private readonly List<IEntity> _pendingAdds = [];
    private readonly List<IEntity> _pendingRemoves = [];

    public void Add(IEntity entity) => _pendingAdds.Add(entity);
    public void Remove(IEntity entity) => _pendingRemoves.Add(entity);

    public void Update(GameTime gameTime)
    {
        foreach (var entity in _entities)
        {
            entity.Update(gameTime);
        }

        foreach (var entity in _entities)
        {
            foreach (var other in _entities)
            {
                if (entity == other)
                {
                    continue;
                }

                foreach (var entityBox in entity.Boxes)
                {
                    foreach (var otherBox in other.Boxes)
                    {
                        if (entityBox.Rectangle.Intersects(otherBox.Rectangle))
                        {
                            entity.OnCollision(other, entityBox, otherBox);
                        }
                    }
                }
            }
        }

        foreach (var entity in _pendingRemoves)
        {
            _entities.Remove(entity);
        }

        foreach (var entity in _pendingAdds)
        {
            _entities.Add(entity);
        }

        _pendingRemoves.Clear();
        _pendingAdds.Clear();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var entity in _entities)
        {
            entity.Draw(spriteBatch);
        }
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        foreach (var entity in _entities)
        {
            entity.LoadContent(content, graphicsDevice);
        }
    }
}