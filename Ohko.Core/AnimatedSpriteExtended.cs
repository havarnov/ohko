using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;

namespace Ohko.Core;

public class AnimatedSpriteExtended<T> where T : class
{
    private readonly AnimatedSprite animatedSprite;
    private readonly Dictionary<string, SliceExtended<T>> _slices = new();

    public AnimatedSpriteExtended(
        string assetName,
        string tagName,
        ContentManager content,
        GraphicsDevice graphicsDevice)
    {
        var file = content.Load<AsepriteFile>(assetName);
        foreach (var slice in file.Slices)
        {
            T? userData = null;
            if (slice.UserData.HasText)
            {
                userData = JsonSerializer.Deserialize<T>(slice.UserData.Text);
            }

            _slices[slice.Name] = new SliceExtended<T>(slice, userData);
        }

        var spriteSheet = file.CreateSpriteSheet(graphicsDevice, onlyVisibleLayers: true);
        animatedSprite = spriteSheet.CreateAnimatedSprite(tagName);
    }

    public IEnumerable<SliceKeyExtended<T>> CurrentSliceKeys => _slices
        .Values
        .Select(s => s.SliceKeyFromFrameIndex(animatedSprite.CurrentFrame.FrameIndex));

    public AnimatedSprite AnimatedSprite => animatedSprite;
}

public class SliceExtended<T>(AsepriteSlice slice, T? userData)
{
    public T? UserData => userData;
    public AsepriteSlice Slice => slice;
    public readonly AsepriteSliceKey[] SliceKeys = slice.Keys.ToArray();

    public SliceKeyExtended<T> SliceKeyFromFrameIndex(int frameIndex)
    {
        // Slices that are equal multiple frames in a row will only have one key representing multiple frames.
        var key = SliceKeys
            .Reverse()
            .First(k => k.FrameIndex <= frameIndex);
        return new SliceKeyExtended<T>(slice.Name, key, UserData);
    }
}

public class SliceKeyExtended<T>(
    string sliceName,
    AsepriteSliceKey sliceKey,
    T? userData)
{
    public string SliceName => sliceName;
    public T? UserData => userData;
    public AsepriteSliceKey SliceKey => sliceKey;
}