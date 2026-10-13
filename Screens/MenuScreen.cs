// MENÚ PRINCIPAL Y SELECCIÓN DE EQUIPO
// MAIN MENU AND TEAM SELECTION

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.Core;

namespace PenguinWars.Screens;

public class MenuScreen : Screen
{
    // Opciones del menú.
    // Menu options.
    private readonly string[] _options =
    {
        "Jugar con los Pingüinos Terrestres",
        "Jugar con los Pingüinos Alienígenas",
        "Salir"
    };

    // Índice de la opción seleccionada.
    // Index of the selected option.
    private int _selected;

    public MenuScreen(Game1 game) : base(game) { }

    public override void Update(GameTime gameTime)
    {
        // Moverse entre opciones; el módulo hace que la selección dé la vuelta.
        // Move between options; the modulo makes the selection wrap around.
        if (InputManager.WasKeyPressed(Keys.Down))
            _selected = (_selected + 1) % _options.Length;
        if (InputManager.WasKeyPressed(Keys.Up))
            _selected = (_selected - 1 + _options.Length) % _options.Length;

        // Confirmar la opción elegida.
        // Confirm the chosen option.
        if (InputManager.WasKeyPressed(Keys.Enter))
        {
            switch (_selected)
            {
                case 0: Game.Screens.ChangeScreen(new GameplayScreen(Game, Team.Heroes)); break;
                case 1: Game.Screens.ChangeScreen(new GameplayScreen(Game, Team.Aliens)); break;
                case 2: Game.Exit(); break;
            }
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        // Título y subtítulo.
        // Title and subtitle.
        DrawCenteredText(sb, "PENGUIN WARS", 170, Color.White, 2f);
        DrawCenteredText(sb, "Elige tu equipo", 260, Color.LightGray);

        // Opciones; la seleccionada se resalta en dorado.
        // Options; the selected one is highlighted in gold.
        for (int i = 0; i < _options.Length; i++)
        {
            bool isSelected = i == _selected;
            string text = isSelected ? $"> {_options[i]} <" : _options[i];
            DrawCenteredText(sb, text, 360 + i * 50, isSelected ? Color.Gold : Color.White);
        }

        // Instrucciones de control.
        // Control instructions.
        DrawCenteredText(sb, "Flechas para moverte, Enter para elegir", 640, Color.Gray, 0.8f);
    }
}