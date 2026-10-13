// GESTOR DE ENTRADAS DEL TECLADO Y MOUSE
// MOUSE AND KEYBOARD INPUT MANAGER

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PenguinWars.Core;

// Guarda el estado del teclado y el ratón en el frame actual y en el anterior.
// Saves the state of the keyboard and mouse for the current and previous frames.

// Comparar ambos permite saber si una tecla se ACABA de presionar.
// Comparing the two makes it possible to determine if a key has JUST been pressed.
public static class InputManager
{
    private static KeyboardState _currentKeys, _previousKeys;
    private static MouseState _currentMouse, _previousMouse;

    public static void Update()
    {
        _previousKeys = _currentKeys;
        _currentKeys = Keyboard.GetState();
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
    }

    // Verdadero mientras la tecla siga presionada (para moverse, apuntar).
    // True while the key remains pressed (for moving, aiming).
    public static bool IsKeyDown(Keys key) => _currentKeys.IsKeyDown(key);

    // Verdadero solo en el frame en que se presionó (para menús, disparar).
    // True only in the frame where it was pressed (for menus, shooting).
    public static bool WasKeyPressed(Keys key) =>
        _currentKeys.IsKeyDown(key) && _previousKeys.IsKeyUp(key);

    public static Point MousePosition => _currentMouse.Position;

    public static bool WasLeftClicked =>
        _currentMouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released;
}