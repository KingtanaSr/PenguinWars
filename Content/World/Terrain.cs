// TERRENO DESTRUCTIBLE BASADO EN PÍXELES
// PIXEL-BASED DESTRUCTIBLE TERRAIN

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.World;

// Cada píxel del mapa es "sólido" (tiene color) o "aire" (transparente).
// Each pixel of the map is either "solid" (has color) or "air" (transparent).
public class Terrain : IDisposable
{
    // Grosor del borde quemado alrededor de un cráter.
    // Thickness of the scorched border around a crater.
    private const int ScorchWidth = 4;

    public int Width { get; }
    public int Height { get; }

    // Colores de todos los píxeles del mapa, guardados en memoria.
    // Colors of every pixel of the map, stored in memory.
    private readonly Color[] _pixels;

    // Textura que se dibuja en pantalla.
    // Texture that is drawn on screen.
    private readonly Texture2D _texture;

    // Indica si el terreno cambió y hay que actualizar la textura.
    // Indicates whether the terrain changed and the texture must be updated.
    private bool _isDirty;

    public Terrain(GraphicsDevice graphicsDevice, int width, int height, int seed)
    {
        Width = width;
        Height = height;
        _pixels = new Color[width * height];
        _texture = new Texture2D(graphicsDevice, width, height);

        Generate(seed);
        _texture.SetData(_pixels);
    }

    // Genera el terreno sumando ondas senoidales con fases aleatorias.
    // Generates the terrain by adding sine waves with random phases.
    private void Generate(int seed)
    {
        // La semilla hace que el mismo número genere siempre el mismo mapa.
        // The seed makes the same number always generate the same map.
        var rng = new Random(seed);
        float phase1 = (float)rng.NextDouble() * MathHelper.TwoPi;
        float phase2 = (float)rng.NextDouble() * MathHelper.TwoPi;
        float phase3 = (float)rng.NextDouble() * MathHelper.TwoPi;

        for (int x = 0; x < Width; x++)
        {
            // Posición horizontal normalizada entre 0 y 1.
            // Horizontal position normalized between 0 and 1.
            float t = x / (float)Width;

            // Ondas grandes (montañas), medianas (colinas) y pequeñas (irregularidades).
            // Large waves (mountains), medium waves (hills) and small waves (bumps).
            float surface = Height * 0.62f
                + MathF.Sin(t * MathHelper.TwoPi * 1.5f + phase1) * 70f
                + MathF.Sin(t * MathHelper.TwoPi * 4f + phase2) * 25f
                + MathF.Sin(t * MathHelper.TwoPi * 11f + phase3) * 8f;

            // Valle en el centro del mapa, donde irán los restos de la nave.
            // Valley in the center of the map, where the spaceship wreckage will go.
            float distanceToCenter = MathF.Abs(t - 0.5f);
            if (distanceToCenter < 0.12f)
                surface += (0.12f - distanceToCenter) / 0.12f * 60f;

            // Limitar la altura para que el terreno no salga de la pantalla.
            // Clamp the height so the terrain does not leave the screen.
            int surfaceY = (int)Math.Clamp(surface, 150f, Height - 60f);

            // Rellenar desde la superficie hacia abajo.
            // Fill from the surface downwards.
            for (int y = surfaceY; y < Height; y++)
                _pixels[y * Width + x] = ColorForDepth(y - surfaceY, rng);
        }
    }

    // Devuelve el color de un píxel según su profundidad: nieve arriba, hielo abajo.
    // Returns a pixel color based on its depth: snow on top, ice below.
    private static Color ColorForDepth(int depth, Random rng)
    {
        // Pequeña variación aleatoria para dar textura.
        // Small random variation to add texture.
        int noise = rng.Next(-8, 9);

        Color baseColor;
        if (depth < 10)
            baseColor = new Color(240, 246, 255);   // Nieve / Snow
        else if (depth < 40)
            baseColor = Color.Lerp(new Color(200, 225, 245), new Color(150, 195, 230), (depth - 10) / 30f);   // Hielo claro / Light ice
        else
            baseColor = Color.Lerp(new Color(150, 195, 230), new Color(70, 110, 160), Math.Min(1f, (depth - 40) / 200f));   // Hielo profundo / Deep ice

        return new Color(
            Math.Clamp(baseColor.R + noise, 0, 255),
            Math.Clamp(baseColor.G + noise, 0, 255),
            Math.Clamp(baseColor.B + noise, 0, 255));
    }

    // Indica si un píxel es sólido. Fuera del mapa se considera aire.
    // Indicates whether a pixel is solid. Outside the map is considered air.
    public bool IsSolid(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;

        return _pixels[y * Width + x].A > 0;
    }

    // Devuelve la altura de la superficie en una columna (el primer píxel sólido desde arriba).
    // Returns the surface height in a column (the first solid pixel from the top).
    public int GetSurfaceY(int x)
    {
        for (int y = 0; y < Height; y++)
        {
            if (IsSolid(x, y))
                return y;
        }

        // No hay terreno en esa columna.
        // There is no terrain in that column.
        return Height;
    }

    // Abre un cráter circular y quema su borde.
    // Opens a circular crater and scorches its border.
    public void Destroy(Vector2 center, float radius)
    {
        int range = (int)MathF.Ceiling(radius + ScorchWidth);
        int cx = (int)center.X;
        int cy = (int)center.Y;

        for (int dy = -range; dy <= range; dy++)
        {
            for (int dx = -range; dx <= range; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                    continue;

                // Distancia del píxel al centro de la explosión.
                // Distance from the pixel to the center of the explosion.
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                int index = y * Width + x;

                if (distance <= radius)
                {
                    // Dentro del círculo: el píxel se vuelve aire.
                    // Inside the circle: the pixel becomes air.
                    _pixels[index] = Color.Transparent;
                }
                else if (distance <= radius + ScorchWidth && _pixels[index].A > 0)
                {
                    // En el borde: el píxel se oscurece (quemado).
                    // On the border: the pixel gets darker (scorched).
                    _pixels[index] = Color.Lerp(_pixels[index], new Color(55, 60, 75), 0.5f);
                }
            }
        }

        _isDirty = true;
    }

    // Copia los cambios a la textura solo si el terreno cambió.
    // Copies the changes to the texture only if the terrain changed.
    public void ApplyChanges()
    {
        if (!_isDirty)
            return;

        _texture.SetData(_pixels);
        _isDirty = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
    }

    // Libera la textura de la memoria de video.
    // Releases the texture from video memory.
    public void Dispose()
    {
        _texture.Dispose();
    }
}