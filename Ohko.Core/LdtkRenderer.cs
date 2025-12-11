using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsepriteDotNet.Aseprite;
using LDtk;
using LDtk.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;

namespace Ohko.Core;

/// <summary>
/// Renderer for the ldtkWorld, ldtkLevel, intgrids and entities.
/// This can all be done in your own class if you want to reimplement it and customize it differently
/// this one is mostly here to get you up and running quickly.
/// </summary>
public class LdtkRenderer : IDisposable
{
    /// <summary> Gets or sets the spritebatch used for rendering with this Renderer. </summary>
    private SpriteBatch SpriteBatch { get; set; }

    /// <summary> Gets or sets the levels identifier to layers Dictionary. </summary>
    Dictionary<string, RenderedLevel> PrerenderedLevels { get; } = [];

    /// <summary> Gets or sets the levels identifier to layers Dictionary. </summary>
    Dictionary<string, Texture2D> TilemapCache { get; } = [];

    readonly Texture2D pixel;
    readonly Texture2D error;
    readonly GraphicsDevice graphicsDevice;
    readonly ContentManager content;
    private readonly LDtkFile _worldFile;

    /// <summary> Initializes a new instance of the <see cref="LdtkRenderer"/> class.</summary>
    /// <param name="spriteBatch">Spritebatch</param>
    /// <param name="content">ContentManger</param>
    /// <param name="worldFile"></param>
    public LdtkRenderer(SpriteBatch spriteBatch, ContentManager content, LDtkFile worldFile)
    {
        this.content = content;
        _worldFile = worldFile;
        SpriteBatch = spriteBatch;
        graphicsDevice = spriteBatch.GraphicsDevice;

        if (pixel == null)
        {
            pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        if (error == null)
        {
            error = new Texture2D(graphicsDevice, 2, 2);
            error.SetData(
                new byte[]
                {
                    0xFF, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0xFF,
                    0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00,
                }
            );
        }
    }

    /// <summary> Prerender out the level to textures to optimize the rendering process. </summary>
    /// <param name="level">The level to prerender.</param>
    /// <returns>The prerendered level.</returns>
    /// <exception cref="Exception">The level already has been prerendered.</exception>
    public RenderedLevel PrerenderLevel(LDtkLevel level)
    {
        if (PrerenderedLevels.TryGetValue(level.Identifier, out RenderedLevel cachedLevel))
        {
            return cachedLevel;
        }

        RenderedLevel renderLevel = new();

        // SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // {
            renderLevel.Layers = RenderLayers(level);
        // }
        // SpriteBatch.End();

        PrerenderedLevels.Add(level.Identifier, renderLevel);
        graphicsDevice.SetRenderTarget(null);

        return renderLevel;
    }

    Texture2D[] RenderLayers(LDtkLevel level)
    {
        List<Texture2D> layers = [];

        if (level.BgRelPath != null)
        {
            layers.Add(RenderBackgroundToLayer(level));
        }

        if (level.LayerInstances == null)
        {
            if (level.ExternalRelPath != null)
            {
                throw new LDtkException("Level has not been loaded.");
            }
            else
            {
                throw new LDtkException("Level has no layers.");
            }
        }

        // Render Tile, Auto and Int grid layers
        for (int i = level.LayerInstances.Length - 1; i >= 0; i--)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                LayerInstance layer = level.LayerInstances[i];

                if (layer._TilesetRelPath == null)
                {
                    continue;
                }

                if (layer._Type == LayerType.Entities)
                {
                    continue;
                }

                Texture2D texture = GetTexture(layer._TilesetRelPath);

                int width = layer._CWid * layer._GridSize;
                int height = layer._CHei * layer._GridSize;
                RenderTarget2D renderTarget = new(graphicsDevice, width, height, false, SurfaceFormat.Color,
                    DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

                graphicsDevice.SetRenderTarget(renderTarget);
                renderTarget.Name = layer._Identifier;
                layers.Add(renderTarget);

                switch (layer._Type)
                {
                    case LayerType.Tiles:
                        foreach (TileInstance tile in layer.GridTiles.Where(_ => layer._TilesetDefUid.HasValue))
                        {
                            Vector2 position = new(tile.Px.X + layer._PxTotalOffsetX,
                                tile.Px.Y + layer._PxTotalOffsetY);
                            Rectangle rect = new(tile.Src.X, tile.Src.Y, layer._GridSize, layer._GridSize);
                            SpriteEffects mirror = (SpriteEffects)tile.F;
                            SpriteBatch.Draw(texture, position, rect, new Color(1f, 1f, 1f, layer._Opacity), 0,
                                Vector2.Zero, 1f, mirror, 0);
                        }

                        break;

                    case LayerType.AutoLayer:
                    case LayerType.IntGrid:
                        if (layer.AutoLayerTiles.Length > 0)
                        {
                            foreach (TileInstance tile in
                                     layer.AutoLayerTiles.Where(_ => layer._TilesetDefUid.HasValue))
                            {
                                Vector2 position = new(tile.Px.X + layer._PxTotalOffsetX,
                                    tile.Px.Y + layer._PxTotalOffsetY);
                                Rectangle rect = new(tile.Src.X, tile.Src.Y, layer._GridSize, layer._GridSize);
                                SpriteEffects mirror = (SpriteEffects)tile.F;
                                SpriteBatch.Draw(texture, position, rect, new Color(1f, 1f, 1f, layer._Opacity), 0,
                                    Vector2.Zero, 1f, mirror, 0);
                            }
                        }

                        break;
                }

            }
            finally
            {
                SpriteBatch.End();
            }
        }

        return layers.ToArray();
    }

    RenderTarget2D RenderBackgroundToLayer(LDtkLevel level)
    {
        Texture2D texture = GetTexture(level.BgRelPath);

        RenderTarget2D layer = new(graphicsDevice, level.PxWid, level.PxHei, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        graphicsDevice.SetRenderTarget(layer);
        {
            LevelBackgroundPosition? bg = level._BgPos;
            if (bg != null)
            {
                Vector2 pos = bg.TopLeftPx.ToVector2();
                SpriteBatch.Draw(texture, pos, new Rectangle((int)bg.CropRect[0], (int)bg.CropRect[1], (int)bg.CropRect[2], (int)bg.CropRect[3]), Color.White, 0, Vector2.Zero, bg.Scale, SpriteEffects.None, 0);
            }
        }
        graphicsDevice.SetRenderTarget(null);

        return layer;
    }

    Texture2D GetTexture(string? path)
    {
        if (path == null)
        {
            throw new LDtkException("Tileset path is null.");
        }

        if (TilemapCache.TryGetValue(path, out var texture))
        {
            return texture;
        }

        Texture2D tilemap;
        if (path.EndsWith(".ase") || path.EndsWith(".aseprite"))
        {
            var file = Path.GetFileNameWithoutExtension(path);
            var asepriteFile = content.Load<AsepriteFile>(file);
            var sheet = asepriteFile.CreateSpriteSheet(
                graphicsDevice,
                onlyVisibleLayers: true,
                includeBackgroundLayer: false,
                includeTilemapLayers: false,
                mergeDuplicateFrames: true,
                borderPadding: 0,
                spacing: 0,
                innerPadding: 0);
            tilemap = sheet.TextureAtlas.Texture;
        }
        else
        {
            var file = Path.GetFileNameWithoutExtension(path);
            tilemap = content.Load<Texture2D>(file);
        }

        TilemapCache.Add(path, tilemap);

        return tilemap;
    }

    /// <summary> Render the prerendered level you created from PrerenderLevel(). </summary>
    /// <param name="level">Level to prerender</param>
    /// <exception cref="LDtkException"></exception>
    public void RenderPrerenderedLevel(LDtkLevel level)
    {
        if (PrerenderedLevels.TryGetValue(level.Identifier, out RenderedLevel prerenderedLevel))
        {
            for (int i = 0; i < prerenderedLevel.Layers.Length; i++)
            {
                SpriteBatch.Draw(prerenderedLevel.Layers[i], level.Position.ToVector2(), Color.White);
            }
        }
        else
        {
            throw new LDtkException($"No prerendered level with Identifier {level.Identifier} found.");
        }
    }

    public void RenderPrerenderedLevelX(LDtkLevel level, Camera camera)
    {
        if (PrerenderedLevels.TryGetValue(level.Identifier, out RenderedLevel prerenderedLevel))
        {
            for (int i = 0; i < prerenderedLevel.Layers.Length; i++)
            {
                Vector2 parallax = new Vector2(1f);
                var layerDef = _worldFile.Defs.Layers.FirstOrDefault(f => f.Identifier == prerenderedLevel.Layers[i].Name);
                if (layerDef is not null && layerDef.ParallaxFactorX != 0f)
                {
                    parallax *= new Vector2(layerDef.ParallaxFactorX, layerDef.ParallaxFactorY);
                }

                SpriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, transformMatrix: camera.GetViewMatrix(parallax));
                SpriteBatch.Draw(prerenderedLevel.Layers[i], level.Position.ToVector2(), Color.White);
                SpriteBatch.End();
            }
        }
        else
        {
            throw new LDtkException($"No prerendered level with Identifier {level.Identifier} found.");
        }
    }

    /// <summary> Dispose Renderer </summary>
    public void Dispose()
    {
        pixel.Dispose();
        GC.SuppressFinalize(this);
    }
}
