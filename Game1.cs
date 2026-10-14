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

    // Textura de un círculo blanco, para proyectiles y explosiones.
    // White circle texture, for projectiles and explosions.
    public Texture2D Circle { get; private set; }

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
        Circle = CreateCircleTexture(64);

        Font = Content.Load<SpriteFont>("Fonts/Main");
        // Si un texto trae un carácter que la fuente no tiene, se dibuja "?" en lugar de fallar.
        // If a text contains a character the font does not have, "?" is drawn instead of crashing.
        Font.DefaultCharacter = '?';

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

        // Crea por código una textura con un círculo blanco sobre fondo transparente.
    // Creates by code a texture with a white circle on a transparent background.
    private Texture2D CreateCircleTexture(int diameter)
    {
        var texture = new Texture2D(GraphicsDevice, diameter, diameter);
        var data = new Color[diameter * diameter];
        float radius = diameter / 2f;

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                // Distancia del centro del píxel al centro del círculo.
                // Distance from the pixel center to the circle center.
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                data[y * diameter + x] = dx * dx + dy * dy <= radius * radius
                    ? Color.White
                    : Color.Transparent;
            }
        }

        texture.SetData(data);
        return texture;
    }
}