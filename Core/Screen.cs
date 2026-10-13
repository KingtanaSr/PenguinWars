// CLASE BASE DE TODAS LAS PANTALLAS
// BASE CLASS FOR ALL SCREENS

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.Core;

// Cada pantalla (menú, partida, fin) hereda de esta clase y define su propia lógica y dibujo.
// Each screen (menu, gameplay, game over) inherits from this class and defines its own logic and drawing.
public abstract class Screen
{
    // Referencia al juego principal para acceder a recursos compartidos.
    // Reference to the main game to access shared resources.
    protected readonly Game1 Game;

    protected Screen(Game1 game)
    {
        Game = game;
    }

    // Carga los recursos de la pantalla.
    // Loads the screen's resources.
    public virtual void LoadContent() { }

    // Lógica de la pantalla, se ejecuta cada frame.
    // Screen logic, runs every frame.
    public abstract void Update(GameTime gameTime);

    // Dibujo de la pantalla, se ejecuta cada frame.
    // Screen drawing, runs every frame.
    public abstract void Draw(SpriteBatch spriteBatch);

    // Dibuja un rectángulo de color usando la textura de 1x1.
    // Draws a colored rectangle using the 1x1 texture.
    protected void DrawRect(SpriteBatch sb, Rectangle rect, Color color) =>
        sb.Draw(Game.Pixel, rect, color);

    // Dibuja un texto centrado horizontalmente en la pantalla.
    // Draws text horizontally centered on the screen.
    protected void DrawCenteredText(SpriteBatch sb, string text, float y, Color color, float scale = 1f)
    {
        Vector2 size = Game.Font.MeasureString(text) * scale;
        var position = new Vector2((Game1.ScreenWidth - size.X) / 2f, y);
        sb.DrawString(Game.Font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}