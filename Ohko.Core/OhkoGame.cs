using LDtk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

public class OhkoGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private readonly Point _gameBounds = new Point(500, 1000);

    private ControlPad _controlPad = null!;
    private SpriteBatch _spriteBatch = null!;
    private Hero _hero = null!;
    private LevelManager _levelManager = null!;
    private World _physicsWorld = null!;
    private Camera camera = null!;

    public OhkoGame(bool isFullScreen)
    {
        if (isFullScreen)
        {
            _gameBounds.X = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _gameBounds.Y = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }

        _graphics = new GraphicsDeviceManager(this);
        _graphics.IsFullScreen = isFullScreen;
        _graphics.PreferredBackBufferWidth = _gameBounds.X;
        _graphics.PreferredBackBufferHeight = _gameBounds.Y;
        _graphics.SupportedOrientations = DisplayOrientation.Portrait;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";

        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // GumService.Default.Initialize(this);
        _hero = new Hero(new Vector2(_gameBounds.X / 2f, (_gameBounds.Y * 0.6f) / 2f));
        _controlPad = new ControlPad(_hero);

        var worldFile = LDtkFile.FromFile("ohko.ldtk");
        _physicsWorld = new World
        {
            Gravity = new nkast.Aether.Physics2D.Common.Vector2(0, 9.81f),
        };

        base.Initialize();

        _levelManager = new LevelManager(worldFile, _physicsWorld);
        _levelManager.Load("Level1", GraphicsDevice, _spriteBatch, Content);

        camera = new Camera(GraphicsDevice);
        camera.Position = (_levelManager.Level.Position + new Vector2(_levelManager.Level.Size.X / 2f, _levelManager.Level.Size.Y / 2f  + 50).ToPoint()).ToVector2();
        camera.Zoom = 4f;
        _hero.Position = (_levelManager.Level.Position + new Vector2(_levelManager.Level.Size.X / 2f, _levelManager.Level.Size.Y / 2f).ToPoint()).ToVector2();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _controlPad.LoadContent(Content, _graphics.GraphicsDevice);
        _hero.LoadContent(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _controlPad.Update(_gameBounds);
        _hero.Update(gameTime);
        camera.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _levelManager.Draw(GraphicsDevice, camera);

        _spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, transformMatrix: camera.Transform);

        _hero.Draw(_spriteBatch);

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _controlPad.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}