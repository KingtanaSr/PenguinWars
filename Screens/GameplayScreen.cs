// PANTALLA DE PARTIDA (PROVISIONAL)
// GAMEPLAY SCREEN (TEMPORARY)

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.Core;

namespace PenguinWars.Screens;

public class GameplayScreen : Screen
{
    // Equipo que controla el jugador; la IA controla el otro.
    // Team controlled by the player; the AI controls the other one.
    private readonly Team _playerTeam;

    public GameplayScreen(Game1 game, Team playerTeam) : base(game)
    {
        _playerTeam = playerTeam;
    }

    public override void Update(GameTime gameTime)
    {
        // Volver al menú con Esc.
        // Return to the menu with Esc.
        if (InputManager.WasKeyPressed(Keys.Escape))
            Game.Screens.ChangeScreen(new MenuScreen(Game));
    }

    public override void Draw(SpriteBatch sb)
    {
        // Suelo de nieve provisional; en la Fase 3 será terreno destructible.
        // Temporary snow ground; in Phase 3 it will become destructible terrain.
        DrawRect(sb, new Rectangle(0, 520, Game1.ScreenWidth, 200), Color.WhiteSmoke);

        // Mostrar el equipo elegido.
        // Show the chosen team.
        string team = _playerTeam == Team.Heroes ? "Pingüinos Terrestres" : "Pingüinos Alienígenas";
        DrawCenteredText(sb, $"Juegas con: {team}", 40, Color.White);
        DrawCenteredText(sb, "Aquí irá la partida. Esc para volver al menú.", 90, Color.LightGray, 0.8f);
    }
}