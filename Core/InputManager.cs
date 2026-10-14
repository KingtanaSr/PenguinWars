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

    // Posición actual del ratón en la pantalla.
    // Current mouse position on screen.
    public static Point MousePosition => _currentMouse.Position;

    // Verdadero solo en el frame en que se hizo clic izquierdo.
    // True only in the frame where the left button was clicked.
    public static bool WasLeftClicked =>
        _currentMouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released;

    // Verdadero mientras el botón izquierdo siga presionado (para cargar la potencia).
    // True while the left button remains pressed (for charging power).
    public static bool IsLeftButtonDown =>
        _currentMouse.LeftButton == ButtonState.Pressed;

    // Verdadero solo en el frame en que se soltó el botón izquierdo.
    // True only in the frame where the left button was released.
    public static bool WasLeftReleased =>
        _currentMouse.LeftButton == ButtonState.Released &&
        _previousMouse.LeftButton == ButtonState.Pressed;

    // Cuánto giró la rueda del ratón desde el frame anterior (positivo = hacia arriba).
    // How much the mouse wheel turned since the previous frame (positive = up).
    public static int ScrollDelta =>
        _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;    
}