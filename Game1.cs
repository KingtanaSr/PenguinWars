// CLASE PRINCIPAL DEL JUEGO
// MAIN GAME CLASS

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenguinWars.Core;
using PenguinWars.Screens;

namespace PenguinWars;

public class Game1 : Game
{
    // Resolución de la ventana.
    // Window resolution.
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Recursos compartidos por todas las pantallas.
    // Resources shared by all screens.
    public ScreenManager Screens { get; } = new ScreenManager();

    // Textura de 1x1 para dibujar rectángulos.
    // 1x1 texture used to draw rectangles.
    public Texture2D Pixel { get; private set; }

    public SpriteFont Font { get; private set; }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Penguin Wars";
    }

    // Configuración inicial: tamaño de la ventana.
    // Initial setup: window size.
    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        _graphics.ApplyChanges();
        base.Initialize();
    }

    // Carga de recursos y pantalla inicial.
    // Resource loading and initial screen.
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        Pixel = new Texture2D(GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });

        Font = Content.Load<SpriteFont>("Fonts/Main");

        Screens.ChangeScreen(new MenuScreen(this));
    }

    // Lógica: primero la entrada, luego la pantalla activa (unas 60 veces por segundo).
    // Logic: input first, then the active screen (about 60 times per second).
    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        Screens.Update(gameTime);
        base.Update(gameTime);
    }

    // Dibujo: limpiar con el color del cielo y dibujar la pantalla activa.
    // Drawing: clear with the sky color and draw the active screen.
    protected override void Draw(GameTime gameTime)
    {
        // Cielo nocturno.
        // Night sky.
        GraphicsDevice.Clear(new Color(18, 24, 38));

        // PointClamp mantiene los píxeles nítidos (ideal para pixel art).
        // PointClamp keeps pixels sharp (ideal for pixel art).
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Screens.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}