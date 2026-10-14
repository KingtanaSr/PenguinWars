// PANTALLA DE FIN DE PARTIDA
// GAME OVER SCREEN

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.Core;
using PenguinWars.Entities;

namespace PenguinWars.Screens;

public class GameOverScreen : Screen
{
    private readonly Team _playerTeam;

    // Equipo ganador; null significa empate.
    // Winning team; null means a draw.
    private readonly Team? _winner;

    // Motivo del final (equipo eliminado, rendición, empate).
    // Reason for the ending (team eliminated, surrender, draw).
    private readonly string _reason;

    private readonly string[] _options = { "Revancha", "Menú principal" };
    private int _selected;

    public GameOverScreen(Game1 game, Team playerTeam, Team? winner, string reason) : base(game)
    {
        _playerTeam = playerTeam;
        _winner = winner;
        _reason = reason;
    }

    public override void Update(GameTime gameTime)
    {
        // Moverse entre opciones.
        // Move between options.
        if (InputManager.WasKeyPressed(Keys.Down))
            _selected = (_selected + 1) % _options.Length;
        if (InputManager.WasKeyPressed(Keys.Up))
            _selected = (_selected - 1 + _options.Length) % _options.Length;

        // Revancha con el mismo equipo, o volver al menú.
        // Rematch with the same team, or return to the menu.
        if (InputManager.WasKeyPressed(Keys.Enter))
        {
            if (_selected == 0)
                Game.Screens.ChangeScreen(new GameplayScreen(Game, _playerTeam));
            else
                Game.Screens.ChangeScreen(new MenuScreen(Game));
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        // Título según el resultado para el jugador.
        // Title based on the result for the player.
        string title;
        Color titleColor;

        if (_winner == null)
        {
            title = "¡EMPATE!";
            titleColor = Color.LightGray;
        }
        else if (_winner == _playerTeam)
        {
            title = "¡VICTORIA!";
            titleColor = Color.Gold;
        }
        else
        {
            title = "DERROTA";
            titleColor = Color.IndianRed;
        }

        DrawCenteredText(sb, title, 170, titleColor, 2f);

        // Equipo ganador y motivo.
        // Winning team and reason.
        string subtitle = _winner == null
            ? "Nadie quedó en pie"
            : $"Ganan los {PenguinProfiles.GetTeamName(_winner.Value)}";
        DrawCenteredText(sb, subtitle, 270, Color.White);
        DrawCenteredText(sb, _reason, 315, Color.LightGray, 0.7f);

        // Opciones.
        // Options.
        for (int i = 0; i < _options.Length; i++)
        {
            bool isSelected = i == _selected;
            string text = isSelected ? $"> {_options[i]} <" : _options[i];
            DrawCenteredText(sb, text, 420 + i * 50, isSelected ? Color.Gold : Color.White);
        }

        DrawCenteredText(sb, "Flechas para moverte, Enter para elegir", 640, Color.Gray, 0.8f);
    }
}