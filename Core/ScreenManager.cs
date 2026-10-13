// GESTOR DE PANTALLAS
// SCREEN MANAGER

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.Core;

// Mantiene la pantalla activa y permite cambiar entre ellas.
// Keeps track of the active screen and allows switching between screens.
public class ScreenManager
{
    private Screen _current;

    // Cambia a una nueva pantalla y carga sus recursos.
    // Switches to a new screen and loads its resources.
    public void ChangeScreen(Screen screen)
    {
        _current = screen;
        _current.LoadContent();
    }

    // Delega la actualización y el dibujo a la pantalla activa.
    // Delegates updating and drawing to the active screen.
    public void Update(GameTime gameTime) => _current?.Update(gameTime);
    public void Draw(SpriteBatch spriteBatch) => _current?.Draw(spriteBatch);
}