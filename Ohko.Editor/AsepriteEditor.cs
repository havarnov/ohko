using System.IO;
using Apos.Gui;
using Apos.Input;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Common;
using AsepriteDotNet.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ohko.Editor;

public class AsepriteEditor(OhkoEditor editor)
{
    private AsepriteFile _file = null!;
    private Texture2D?[] _frames = null!;
    private int _currentFrameIdx = 0;
    private readonly SpriteBatch _spriteBatch = new SpriteBatch(editor.GraphicsDevice);
    private float _zoom = 1;
    private int _userDataPosition = 500;
    private string _userData = string.Empty;

    public bool Enable { get; set; } = false;

    public void LoadContent(string filePath)
    {
        _file = AsepriteFileLoader.FromFile(filePath);
        _frames = new Texture2D[_file.Frames.Length];
    }

    Texture2D GetTextureFromFrame(int frameIdx)
    {
        if (_frames[frameIdx] is { } cachedTexture)
        {
            return cachedTexture;
        }

        var frame = _file.GetFrame(frameIdx);
        Rgba32[] frame0Pixels = frame.FlattenFrame();
        var texture = new Texture2D(editor.GraphicsDevice, _file.Frames[frameIdx].Size.Width, _file.Frames[frameIdx].Size.Height);
        texture.SetData(frame0Pixels);

        _frames[frameIdx] = texture;

        return texture;
    }

    public void Update(GameTime gameTime)
    {
        if (!Enable)
        {
            return;
        }

        Dock.Put(0, InputHelper.WindowHeight - 100, InputHelper.WindowWidth, InputHelper.WindowHeight);

        Vertical.Push();

        Horizontal.Push();

        var count = 0;
        foreach (var frame in _file.Frames)
        {
            var btn = Button.Put(count.ToString(), color: _currentFrameIdx == count ? Color.Red : null);
            if (btn.Clicked)
            {
                _currentFrameIdx = count;
            }

            count++;
        }

        Horizontal.Pop();

        Horizontal.Push();

        if (Button.Put("+").Clicked)
        {
            _zoom += 0.2f;
        }

        if (Button.Put("-").Clicked)
        {
            _zoom -= 0.2f;
        }

        Horizontal.Pop();

        Vertical.Pop();

        // User Data Panel
       Dock.Put(
            InputHelper.WindowWidth - _userDataPosition,
            0,
            _userDataPosition,
            InputHelper.WindowHeight);

        Textbox.Put(ref _userData);
    }


    public void Draw(GameTime gameTime)
    {
        if (!Enable)
        {
            return;
        }

        var width = editor.GraphicsDevice.Viewport.Width - _userDataPosition;
        var height = editor.GraphicsDevice.Viewport.Height - 100;
        var position = new Vector2(width / 2f, height / 2f);

        _spriteBatch.Begin(
            SpriteSortMode.FrontToBack,
            null,
            SamplerState.PointClamp);

        var texture = GetTextureFromFrame(_currentFrameIdx);
        Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

        _spriteBatch.Draw(
            texture,
            position,
            null, Color.White,
            0.0f,
            origin: origin,
            _zoom,
            SpriteEffects.None,
            0.0f);

        _spriteBatch.End();
    }
}