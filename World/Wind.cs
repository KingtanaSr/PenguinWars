// VIENTO QUE AFECTA A LOS PROYECTILES
// WIND THAT AFFECTS PROJECTILES

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.World;

public class Wind
{
    // Aceleración horizontal máxima que puede aplicar el viento.
    // Maximum horizontal acceleration the wind can apply.
    public const float MaxAcceleration = 150f;

    // Fuerza entre -1 (izquierda) y 1 (derecha).
    // Strength between -1 (left) and 1 (right).
    public float Strength { get; private set; }

    public float Acceleration => Strength * MaxAcceleration;

    // Elige un viento nuevo al azar.
    // Picks a new random wind.
    public void Randomize(Random random)
    {
        Strength = (float)(random.NextDouble() * 2.0 - 1.0);
    }

    // Indicador: la barra crece desde el centro hacia el lado al que sopla.
    // Indicator: the bar grows from the center towards the side it blows to.
    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font, int screenWidth)
    {
        const int barWidth = 120;
        const int barHeight = 10;
        int x = screenWidth - barWidth - 30;
        int y = 34;
        int center = x + barWidth / 2;

        // Etiqueta.
        // Label.
        sb.DrawString(font, "Viento", new Vector2(x, y - 22), Color.White,
            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

        // Borde y fondo.
        // Border and background.
        sb.Draw(pixel, new Rectangle(x - 1, y - 1, barWidth + 2, barHeight + 2), Color.White * 0.6f);
        sb.Draw(pixel, new Rectangle(x, y, barWidth, barHeight), new Color(20, 25, 40));

        // Relleno según la fuerza y la dirección.
        // Fill based on strength and direction.
        int fill = (int)(Math.Abs(Strength) * barWidth / 2);
        Rectangle fillRect = Strength >= 0
            ? new Rectangle(center, y, fill, barHeight)
            : new Rectangle(center - fill, y, fill, barHeight);
        sb.Draw(pixel, fillRect, Color.SkyBlue);

        // Marca del centro.
        // Center mark.
        sb.Draw(pixel, new Rectangle(center, y - 2, 1, barHeight + 4), Color.White);
    }
}