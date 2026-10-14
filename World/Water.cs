// MAR HELADO ANIMADO EN LA PARTE INFERIOR DEL MAPA
// ANIMATED ICY SEA AT THE BOTTOM OF THE MAP

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.World;

public class Water
{
    // Ancho de cada columna de agua (más ancho = menos rectángulos que dibujar).
    // Width of each water column (wider = fewer rectangles to draw).
    private const int ColumnWidth = 4;

    private static readonly Color WaterColor = new(30, 70, 120);
    private static readonly Color FoamColor = new(170, 210, 240);

    // Altura media de la superficie del agua.
    // Average height of the water surface.
    private readonly int _surfaceY;
    private float _time;

    public Water(int surfaceY)
    {
        _surfaceY = surfaceY;
    }

    public void Update(float dt) => _time += dt;

    // Cada columna sube y baja según dos ondas que se desplazan con el tiempo.
    // Each column rises and falls following two waves that move over time.
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, int width, int height)
    {
        for (int x = 0; x < width; x += ColumnWidth)
        {
            int waveY = (int)(_surfaceY
                + MathF.Sin(x * 0.03f + _time * 2f) * 3f
                + MathF.Sin(x * 0.07f - _time * 1.3f) * 1.5f);

            spriteBatch.Draw(pixel, new Rectangle(x, waveY, ColumnWidth, height - waveY), WaterColor * 0.88f);

            // Línea de espuma en la cresta de la ola.
            // Foam line at the crest of the wave.
            spriteBatch.Draw(pixel, new Rectangle(x, waveY, ColumnWidth, 2), FoamColor * 0.8f);
        }
    }
}