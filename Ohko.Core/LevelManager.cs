using System;
using LDtk;
using LDtkTypes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

internal class LevelManager(LDtkFile lDtkFile, World physicsWorld)
{
    private LdtkRenderer renderer = null!;
    private LDtkWorld? world;
    public LDtkLevel Level { get; private set; } = null!;

    public int TileSize => world!.WorldGridWidth;

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

                var wall = physicsWorld.CreateRectangle(
                        collisions.TileSize,
                        collisions.TileSize,
                        1f,
                        new nkast.Aether.Physics2D.Common.Vector2(collisions.WorldPosition.X, collisions.WorldPosition.Y)
                        + new nkast.Aether.Physics2D.Common.Vector2((i * collisions.TileSize) + (collisions.TileSize / 2f), (j * collisions.TileSize) + (collisions.TileSize / 2f)),
                        0f,
                        BodyType.Static);
                wall.FixedRotation = true;
                wall.FixtureList[0].Friction = 0f;
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