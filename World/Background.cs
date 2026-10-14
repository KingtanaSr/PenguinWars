// FONDO PROCEDURAL: CIELO NOCTURNO, AURORA BOREAL, ESTRELLAS Y MONTAÑAS
// PROCEDURAL BACKGROUND: NIGHT SKY, NORTHERN LIGHTS, STARS AND MOUNTAINS

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.World;

// Se genera una sola vez en una textura; después dibujarlo cuesta lo mismo que una imagen.
// It is generated only once into a texture; afterwards drawing it costs the same as an image.
public class Background : IDisposable
{
    private readonly Texture2D _texture;
    private readonly int _width;
    private readonly int _height;

    public Background(GraphicsDevice graphicsDevice, int width, int height, int seed)
    {
        _width = width;
        _height = height;
        var rng = new Random(seed);
        var pixels = new Color[width * height];

        // Cada capa se pinta encima de la anterior: de lo más lejano a lo más cercano.
        // Each layer is painted over the previous one: from the farthest to the nearest.
        PaintSky(pixels);
        PaintAurora(pixels, rng);
        PaintStars(pixels, rng);
        PaintMountains(pixels, rng, baseFraction: 0.50f, amplitude: 60f, new Color(38, 50, 82), snowCaps: false);
        PaintMountains(pixels, rng, baseFraction: 0.60f, amplitude: 45f, new Color(28, 37, 62), snowCaps: true);

        _texture = new Texture2D(graphicsDevice, width, height);
        _texture.SetData(pixels);
    }

    // Degradado vertical: noche profunda arriba, más claro hacia el horizonte.
    // Vertical gradient: deep night at the top, lighter towards the horizon.
    private void PaintSky(Color[] pixels)
    {
        var top = new Color(8, 12, 28);
        var bottom = new Color(52, 72, 110);

        for (int y = 0; y < _height; y++)
        {
            Color row = Color.Lerp(top, bottom, y / (float)(_height - 1));
            for (int x = 0; x < _width; x++)
                pixels[y * _width + x] = row;
        }
    }

    // Dos bandas onduladas de luz (verde y morada) que se suman al cielo.
    // Two wavy bands of light (green and purple) added onto the sky.
    private void PaintAurora(Color[] pixels, Random rng)
    {
        Color[] bandColors = { new Color(80, 255, 160), new Color(170, 90, 255) };

        for (int band = 0; band < bandColors.Length; band++)
        {
            float phase = (float)rng.NextDouble() * MathHelper.TwoPi;
            float baseY = 110f + band * 55f;
            const float thickness = 38f;

            for (int x = 0; x < _width; x++)
            {
                float centerY = baseY + MathF.Sin(x * 0.006f + phase) * 35f + MathF.Sin(x * 0.017f + phase * 2f) * 10f;

                for (int y = (int)(centerY - thickness); y <= (int)(centerY + thickness); y++)
                {
                    if (y < 0 || y >= _height)
                        continue;

                    // Más intensa en el centro de la banda y suave en los bordes.
                    // Stronger at the center of the band and soft at the edges.
                    float intensity = 1f - MathF.Abs(y - centerY) / thickness;
                    AddLight(pixels, x, y, bandColors[band], intensity * intensity * 0.28f);
                }
            }
        }
    }

    // Estrellas de distinto brillo en la parte alta del cielo.
    // Stars of different brightness in the upper part of the sky.
    private void PaintStars(Color[] pixels, Random rng)
    {
        for (int i = 0; i < 220; i++)
        {
            int x = rng.Next(_width);
            int y = rng.Next((int)(_height * 0.55f));
            float brightness = 0.4f + (float)rng.NextDouble() * 0.6f;
            AddLight(pixels, x, y, Color.White, brightness);

            // Algunas estrellas más grandes, con un pequeño brillo alrededor.
            // Some bigger stars, with a small glow around them.
            if (rng.NextDouble() < 0.15)
            {
                AddLight(pixels, x + 1, y, Color.White, brightness * 0.4f);
                AddLight(pixels, x - 1, y, Color.White, brightness * 0.4f);
                AddLight(pixels, x, y + 1, Color.White, brightness * 0.4f);
                AddLight(pixels, x, y - 1, Color.White, brightness * 0.4f);
            }
        }
    }

    // Silueta de montañas; opcionalmente con nieve en la parte alta.
    // Mountain silhouette; optionally with snow on the upper part.
    private void PaintMountains(Color[] pixels, Random rng, float baseFraction, float amplitude, Color color, bool snowCaps)
    {
        float phase1 = (float)rng.NextDouble() * MathHelper.TwoPi;
        float phase2 = (float)rng.NextDouble() * MathHelper.TwoPi;
        float baseY = _height * baseFraction;
        var snowColor = new Color(170, 185, 215);

        for (int x = 0; x < _width; x++)
        {
            int ridgeY = (int)(baseY
                - MathF.Sin(x * 0.004f + phase1) * amplitude
                - MathF.Sin(x * 0.013f + phase2) * amplitude * 0.35f);

            for (int y = Math.Max(0, ridgeY); y < _height; y++)
            {
                // Cumbres nevadas: unos píxeles bajo la cresta, solo en las partes altas.
                // Snowy peaks: a few pixels below the ridge, only on the high parts.
                bool isSnow = snowCaps && y - ridgeY < 6 && ridgeY < baseY - amplitude * 0.3f;
                pixels[y * _width + x] = isSnow ? Color.Lerp(color, snowColor, 0.6f) : color;
            }
        }
    }

    // Suma luz a un píxel (mezcla aditiva), sin pasar de 255.
    // Adds light to a pixel (additive blending), without exceeding 255.
    private void AddLight(Color[] pixels, int x, int y, Color light, float amount)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return;

        int index = y * _width + x;
        Color current = pixels[index];
        pixels[index] = new Color(
            Math.Min(255, current.R + (int)(light.R * amount)),
            Math.Min(255, current.G + (int)(light.G * amount)),
            Math.Min(255, current.B + (int)(light.B * amount)));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
    }

    public void Dispose()
    {
        _texture.Dispose();
    }
}