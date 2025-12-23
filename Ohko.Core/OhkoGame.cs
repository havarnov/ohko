using LDtk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;

namespace Ohko.Core;

public class OhkoGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private readonly Point _gameBounds = new Point(500, 1000);

    private ControlPad _controlPad = null!;
    private SpriteBatch _spriteBatch = null!;
    private Hero _hero = null!;
    private EntityManager _entityManager = new();
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

        base.Initialize();

        _levelManager = new LevelManager(worldFile, _physicsWorld);
        _levelManager.Load("Level1", GraphicsDevice, _spriteBatch, Content);

        camera = new Camera(GraphicsDevice);
        camera.Zoom = _graphics.GraphicsDevice.Viewport.Width / 100f;

        var unscaledYOffset = _graphics.GraphicsDevice.Viewport.Height * 0.6f - (_graphics.GraphicsDevice.Viewport.Height / 2f);

        camera.Position = _levelManager.Level.Position.ToVector2()
                          + new Vector2(
                              _levelManager.Level.Size.X / 2f,
                              // NOTE: 16 here is hard coded for this tile size
                              _levelManager.Level.Size.Y - 16 - (unscaledYOffset / camera.Zoom));

        _hero.Position = _levelManager.Level.Position.ToVector2()
                         + new Vector2(_levelManager.Level.Size.X / 2f, _levelManager.Level.Size.Y / 2f);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _controlPad.LoadContent(Content, GraphicsDevice);
        _entityManager.LoadContent(Content, GraphicsDevice);
        _hero.LoadContent(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _physicsWorld.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
        _controlPad.Update(_gameBounds);
        _entityManager.Update(gameTime);
        camera.Position = new Vector2(_hero.Position.X, camera.Position.Y);
        camera.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _levelManager.Draw(GraphicsDevice, camera);

        _spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, transformMatrix: camera.Transform);

        _entityManager.Draw(_spriteBatch);
        // foreach (var body in _physicsWorld.BodyList)
        // {
        //     if (body.FixtureList[0].Shape is PolygonShape s)
        //     {
        //         var _texture = new Texture2D(GraphicsDevice, 1, 1);
        //         _texture.SetData([Color.Blue]);
        //         var aabb = s.Vertices.GetAABB();
        //         _spriteBatch.Draw(
        //             _texture,
        //             new Rectangle(
        //                 new Point((int)(body.Position.X - aabb.Extents.X), (int)(body.Position.Y - aabb.Extents.Y)),
        //                 new Point((int)s.Vertices.GetAABB().Extents.X * 2,
        //                     (int)s.Vertices.GetAABB().Extents.Y * 2)),
        //             sourceRectangle: null,
        //             Color.White,
        //             rotation: 0,
        //             origin: Vector2.Zero,
        //             effects: SpriteEffects.None,
        //             layerDepth: 0.99f);
        //     }
        // }

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _controlPad.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}