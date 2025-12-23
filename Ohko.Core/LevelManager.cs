using System;
using System.Collections.Generic;
using LDtk;
using LDtkTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ohko.Core;

public class Collision : IEntity
{
    public required List<Box> Boxes { get; init; }
}

internal class LevelManager(LDtkFile lDtkFile, EntityManager entityManager)
{
    private LdtkRenderer renderer = null!;
    private LDtkWorld? world;
    public LDtkLevel Level { get; private set; } = null!;

    public void Load(
        string levelName,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        ContentManager content)
    {
        if (world is null)
        {
            world = lDtkFile.LoadWorld(Worlds.World.Iid)
                    ?? throw new InvalidOperationException();
        }

        Level = world.LoadLevel(levelName);

        var collisions = Level.GetIntGrid("Collisions");

        for (int i = 0; i < collisions.GridSize.X; i++)
        {
            for (int j = 0; j < collisions.GridSize.Y; j++)
            {
                var value = collisions.GetValueAt(i, j);
                if (value == 0)
                {
                    continue;
                }

                entityManager.Add(new Collision
                {
                    Boxes =
                    [
                        new Box.CollisionBox()
                        {

                            Rectangle = new Rectangle(
                                collisions.WorldPosition.X + (i * collisions.TileSize),
                                collisions.WorldPosition.Y + (j * collisions.TileSize),
                                collisions.TileSize,
                                collisions.TileSize),
                            CollisionTag = $"Collision_{value}"
                        }
                    ],
                });
            }
        }

        renderer = new LdtkRenderer(spriteBatch, content, lDtkFile);
        _ = renderer.PrerenderLevel(Level);
    }

    public void Draw(GraphicsDevice graphicsDevice, Camera camera)
    {
        graphicsDevice.Clear(Level._BgColor);
        renderer.RenderPrerenderedLevelX(Level, camera);
    }
}