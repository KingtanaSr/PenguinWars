// PANTALLA DE PARTIDA
// GAMEPLAY SCREEN

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.Core;
using PenguinWars.World;

namespace PenguinWars.Screens;

public class GameplayScreen : Screen
{
    // Radio de las explosiones de prueba.
    // Radius of the test explosions.
    private const float TestExplosionRadius = 40f;

    // Equipo que controla el jugador; la IA controla el otro.
    // Team controlled by the player; the AI controls the other one.
    private readonly Team _playerTeam;

    private Terrain _terrain;

    public GameplayScreen(Game1 game, Team playerTeam) : base(game)
    {
        _playerTeam = playerTeam;
    }

    public override void LoadContent()
    {
        CreateTerrain();
    }

    // Crea un mapa nuevo con una semilla basada en el tiempo.
    // Creates a new map with a time-based seed.
    private void CreateTerrain()
    {
        _terrain?.Dispose();
        _terrain = new Terrain(Game.GraphicsDevice, Game1.ScreenWidth, Game1.ScreenHeight, Environment.TickCount);
    }

    public override void Update(GameTime gameTime)
    {
        // Volver al menú con Esc.
        // Return to the menu with Esc.
        if (InputManager.WasKeyPressed(Keys.Escape))
        {
            _terrain.Dispose();
            Game.Screens.ChangeScreen(new MenuScreen(Game));
            return;
        }

        // PRUEBA: R genera un mapa nuevo.
        // TEST: R generates a new map.
        if (InputManager.WasKeyPressed(Keys.R))
            CreateTerrain();

        // PRUEBA: clic izquierdo crea una explosión en la posición del ratón.
        // TEST: left click creates an explosion at the mouse position.
        if (InputManager.WasLeftClicked)
            _terrain.Destroy(InputManager.MousePosition.ToVector2(), TestExplosionRadius);

        _terrain.ApplyChanges();
    }

    public override void Draw(SpriteBatch sb)
    {
        DrawSky(sb);
        _terrain.Draw(sb);

        // PRUEBA: marcador rojo sobre la superficie, bajo el ratón.
        // TEST: red marker on the surface, below the mouse.
        int mouseX = Math.Clamp(InputManager.MousePosition.X, 0, Game1.ScreenWidth - 1);
        int surfaceY = _terrain.GetSurfaceY(mouseX);
        DrawRect(sb, new Rectangle(mouseX - 3, surfaceY - 3, 6, 6), Color.Red);

        // Información en pantalla.
        // On-screen information.
        string team = _playerTeam == Team.Heroes ? "Pingüinos Terrestres" : "Pingüinos Alienígenas";
        DrawCenteredText(sb, $"Juegas con: {team}", 20, Color.White, 0.8f);
        DrawCenteredText(sb, "Clic: explosión de prueba  |  R: nuevo mapa  |  Esc: menú", 60, Color.LightGray, 0.6f);
    }

    // Dibuja un cielo con degradado vertical, de noche arriba a más claro abajo.
    // Draws a sky with a vertical gradient, from night at the top to lighter at the bottom.
    private void DrawSky(SpriteBatch sb)
    {
        const int bands = 72;
        int bandHeight = Game1.ScreenHeight / bands;
        var top = new Color(10, 14, 30);
        var bottom = new Color(60, 85, 125);

        for (int i = 0; i < bands; i++)
        {
            Color color = Color.Lerp(top, bottom, i / (float)(bands - 1));
            DrawRect(sb, new Rectangle(0, i * bandHeight, Game1.ScreenWidth, bandHeight), color);
        }
    }
}