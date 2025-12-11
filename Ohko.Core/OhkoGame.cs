using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace Ohko.Core;

public class OhkoGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private readonly Point _gameBounds = new Point(500, 1000);

    private ControlPad _controlPad = null!;
    private SpriteBatch _spriteBatch = null!;
    private Hero _hero = null!;

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
        GumService.Default.Initialize(this);
        _hero = new Hero(new Vector2(_gameBounds.X / 2f, (_gameBounds.Y * 0.6f) / 2f));
        _controlPad = new ControlPad(_hero);
        base.Initialize();
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
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _controlPad.Draw(_spriteBatch);
        _hero.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}