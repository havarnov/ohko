using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ohko.Core;

public interface IEntity
{
    void Update(GameTime gameTime)
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

        Flush();
    }

    private void Flush()
    {
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
        Flush();

        foreach (var entity in _entities)
        {
            entity.LoadContent(content, graphicsDevice);
        }
    }
}