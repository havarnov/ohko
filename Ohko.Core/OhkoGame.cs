using System;
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
    private Hero _opponent = null!;
    private readonly EntityManager _entityManager = new();
    private LevelManager _levelManager = null!;
    private Camera camera = null!;
    private World _physicsWorld = null!;

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
        var worldFile = LDtkFile.FromFile("ohko.ldtk");

        _physicsWorld = new World()
        {
            Gravity = Vector2.Zero.Into(),
        };

        _hero = new Hero(_physicsWorld);
        _entityManager.Add(_hero);
        _controlPad = new ControlPad(_hero);

        _opponent = new Hero(_physicsWorld);
        _entityManager.Add(_opponent);

        _hero.Opponent = _opponent;
        _opponent.Opponent = _hero;

        _hero.Face = _opponent;
        _opponent.Face = _hero;

        base.Initialize();

        _levelManager = new LevelManager(worldFile, _physicsWorld);
        _levelManager.Load("Level1", GraphicsDevice, _spriteBatch, Content);

        camera = new Camera(GraphicsDevice);
        camera.Zoom = _graphics.GraphicsDevice.Viewport.Width / 100f;

        var unscaledYOffset = _graphics.GraphicsDevice.Viewport.Height * 0.6f - (_graphics.GraphicsDevice.Viewport.Height / 2f);

        camera.Position = _levelManager.Level.Position.ToVector2()
                          + new Vector2(
                              _levelManager.Level.Size.X / 2f,
                              _levelManager.Level.Size.Y - (unscaledYOffset / camera.Zoom) - 20);

        _hero.Position = _levelManager.Level.Position.ToVector2()
                         + new Vector2(_levelManager.Level.Size.X / 2f, _levelManager.Level.Size.Y / 2f);
        _opponent.Position = _hero.Position + new Vector2(40, 0);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _controlPad.LoadContent(Content, GraphicsDevice);
        _entityManager.LoadContent(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _physicsWorld.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
        _controlPad.Update(_gameBounds);
        _entityManager.Update(gameTime);

        UpdateCamera(gameTime);

        base.Update(gameTime);
    }

    public void UpdateCamera(GameTime gameTime)
    {
        // --- POSITION ---
        float targetX = (_hero.Position.X + _opponent.Position.X) / 2f;
        camera.Position = new Vector2(
            MathHelper.Lerp(camera.Position.X, targetX, 0.1f),
            camera.Position.Y
        );

        // --- ZOOM ---
        var distance = Math.Abs(_hero.Position.X - _opponent.Position.X);
        var targetZoom = 200 / distance;
        targetZoom = MathHelper.Clamp(targetZoom, 2.5f, 5);
        camera.Zoom = MathHelper.Lerp(camera.Zoom, targetZoom, 0.1f);

        camera.Update();
    }

    protected override void Draw(GameTime gameTime)
    {
        _levelManager.Draw(GraphicsDevice, camera);

        _spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, transformMatrix: camera.Transform);

        _entityManager.Draw(_spriteBatch);

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _controlPad.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}