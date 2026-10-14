// MENÚ PRINCIPAL: SELECCIÓN DE EQUIPO, DIFICULTAD Y ESTRATEGA LLM
// MAIN MENU: TEAM, DIFFICULTY AND LLM STRATEGIST SELECTION

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.AI;
using PenguinWars.Core;

namespace PenguinWars.Screens;

public class MenuScreen : Screen
{
    // Posición de cada opción del menú.
    // Position of each menu option.
    private const int HeroesOption = 0;
    private const int AliensOption = 1;
    private const int DifficultyOption = 2;
    private const int LlmOption = 3;
    private const int ExitOption = 4;
    private const int OptionCount = 5;

    // Se recuerda la última dificultad elegida al volver al menú.
    // The last chosen difficulty is remembered when returning to the menu.
    private static AiDifficulty _difficulty = AiDifficulty.Normal;

    private int _selected;

    public MenuScreen(Game1 game) : base(game) { }

    public override void Update(GameTime gameTime)
    {
        // Moverse entre opciones.
        // Move between options.
        if (InputManager.WasKeyPressed(Keys.Down))
            _selected = (_selected + 1) % OptionCount;
        if (InputManager.WasKeyPressed(Keys.Up))
            _selected = (_selected - 1 + OptionCount) % OptionCount;

        // Izquierda y derecha cambian la dificultad o activan/desactivan el estratega.
        // Left and right change the difficulty or toggle the strategist.
        bool left = InputManager.WasKeyPressed(Keys.Left);
        bool right = InputManager.WasKeyPressed(Keys.Right);

        if (_selected == DifficultyOption)
        {
            if (left) ChangeDifficulty(-1);
            if (right) ChangeDifficulty(1);
        }
        else if (_selected == LlmOption && (left || right))
        {
            GameSettings.UseLlmStrategist = !GameSettings.UseLlmStrategist;
        }

        if (!InputManager.WasKeyPressed(Keys.Enter))
            return;

        switch (_selected)
        {
            case HeroesOption:
                Game.Screens.ChangeScreen(new GameplayScreen(Game, Team.Heroes, _difficulty));
                break;
            case AliensOption:
                Game.Screens.ChangeScreen(new GameplayScreen(Game, Team.Aliens, _difficulty));
                break;
            case DifficultyOption:
                // Enter también cambia la dificultad.
                // Enter also changes the difficulty.
                ChangeDifficulty(1);
                break;
            case LlmOption:
                // Enter también activa o desactiva el estratega.
                // Enter also toggles the strategist.
                GameSettings.UseLlmStrategist = !GameSettings.UseLlmStrategist;
                break;
            case ExitOption:
                Game.Exit();
                break;
        }
    }

    // Pasa a la dificultad siguiente o anterior, dando la vuelta.
    // Moves to the next or previous difficulty, wrapping around.
    private static void ChangeDifficulty(int direction)
    {
        const int count = 3;
        _difficulty = (AiDifficulty)(((int)_difficulty + direction + count) % count);
    }

    // Texto de cada opción.
    // Text of each option.
    private static string GetOptionText(int option) => option switch
    {
        HeroesOption => "Jugar con los Pingüinos Terrestres",
        AliensOption => "Jugar con los Pingüinos Alienígenas",
        DifficultyOption => $"Dificultad de la IA:  < {AiDifficultySettings.GetName(_difficulty)} >",
        LlmOption => $"Estratega LLM (Ollama):  < {(GameSettings.UseLlmStrategist ? "Activado" : "Desactivado")} >",
        ExitOption => "Salir",
        _ => ""
    };

    public override void Draw(SpriteBatch sb)
    {
        // Título y subtítulo.
        // Title and subtitle.
        DrawCenteredText(sb, "PENGUIN WARS", 140, Color.White, 2f);
        DrawCenteredText(sb, "Elige tu equipo; la IA controlará al otro", 230, Color.LightGray, 0.8f);

        // Opciones; la seleccionada se resalta en dorado.
        // Options; the selected one is highlighted in gold.
        for (int i = 0; i < OptionCount; i++)
        {
            bool isSelected = i == _selected;
            string text = isSelected ? $"> {GetOptionText(i)} <" : GetOptionText(i);
            DrawCenteredText(sb, text, 310 + i * 50, isSelected ? Color.Gold : Color.White);
        }

        // Instrucciones de control.
        // Control instructions.
        DrawCenteredText(sb, "Arriba/abajo: moverte  |  Izquierda/derecha: cambiar ajuste  |  Enter: elegir", 640, Color.Gray, 0.6f);
    }
}