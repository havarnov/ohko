using Apos.Gui;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ohko.Editor;

public class OhkoEditor : Game
{
    private SelectFileComponent _selectFileComponent = null!;
    private AsepriteEditor _asepriteEditor = null!;
    private readonly GraphicsDeviceManager _graphics;
    private IMGUI _ui = null!;
    private SpriteBatch _spriteBatch = null!;

    public OhkoEditor()
    {
        Window.AllowUserResizing = true;
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        IsMouseVisible = true;
        _selectFileComponent = new SelectFileComponent(this);
        _asepriteEditor = new AsepriteEditor(this);

        base.Initialize();

        // LOADED

        SelectFile("/tmp/a.aseprite");
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var fontSystem = new FontSystem();
        fontSystem.AddFont(TitleContainer.OpenStream($"{Content.RootDirectory}/jetbrains-mono-regular.ttf"));
        GuiHelper.Setup(this, fontSystem);

        _ui = new IMGUI();

        _selectFileComponent.LoadContent();

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        GuiHelper.UpdateSetup(gameTime);
        _ui.UpdateStart(gameTime);

        _selectFileComponent.Update(gameTime);
        _asepriteEditor.Update(gameTime);

        _ui.UpdateEnd(gameTime);
        GuiHelper.UpdateCleanup();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _ui.Draw(gameTime);

        // _spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp);
        _asepriteEditor.Draw(gameTime);
        // _spriteBatch.End();

        base.Draw(gameTime);
    }

    public void SelectFile(string selectedFile)
    {
        _selectFileComponent.Enable = false;

        _asepriteEditor.LoadContent(selectedFile);
        _asepriteEditor.Enable = true;
    }
}