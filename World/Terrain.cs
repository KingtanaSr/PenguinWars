// TERRENO DESTRUCTIBLE BASADO EN PÍXELES, CON LOS RESTOS DE LA NAVE
// PIXEL-BASED DESTRUCTIBLE TERRAIN, WITH THE SPACESHIP WRECKAGE

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

    // Colores de la nave alienígena (basados en el arte conceptual).
    // Alien spaceship colors (based on the concept art).
    private static readonly Color HullColor = new(62, 56, 84);
    private static readonly Color HullLightColor = new(88, 82, 112);
    private static readonly Color HullDarkColor = new(40, 36, 56);
    private static readonly Color NeonColor = new(110, 255, 140);
    private static readonly Color GlowColor = new(190, 100, 240);
    private static readonly Color GlassColor = new(60, 30, 90);
    private static readonly Color GlassShineColor = new(200, 150, 255);

    // Posiciones horizontales (locales) de las ventanas del casco.
    // Horizontal (local) positions of the hull windows.
    private static readonly float[] WindowPositions = { -40f, -18f, 4f };

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

    // Genera el terreno sumando ondas senoidales con fases aleatorias, y agrega la nave.
    // Generates the terrain by adding sine waves with random phases, and adds the ship.
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

            // Valle en el centro del mapa, donde están los restos de la nave.
            // Valley in the center of the map, where the spaceship wreckage lies.
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

        AddWreckage(rng);
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

    // RESTOS DE LA NAVE
    // SPACESHIP WRECKAGE

    // Pinta los restos de la nave en el valle central como parte del terreno:
    // son sólidos, se puede caminar sobre ellos y se destruyen con las explosiones.
    // Paints the spaceship wreckage in the central valley as part of the terrain:
    // it is solid, can be walked on and is destroyed by explosions.
    private void AddWreckage(Random rng)
    {
        int centerX = Width / 2;

        // El casco queda un poco hundido en la nieve e inclinado.
        // The hull is slightly buried in the snow and tilted.
        var hullCenter = new Vector2(centerX, GetSurfaceY(centerX) - 6);
        const float hullAngle = -0.22f;

        // Aleta trasera (se dibuja primero para que el casco quede encima).
        // Rear fin (drawn first so the hull ends up on top).
        FillRotatedShape(hullCenter, hullAngle, 110, (lx, ly) =>
            IsInsideTriangle(lx, ly, new Vector2(-55f, -14f), new Vector2(-100f, -52f), new Vector2(-82f, -10f))
                ? Noisy(HullDarkColor, rng)
                : (Color?)null);

        // Casco principal: una elipse alargada.
        // Main hull: an elongated ellipse.
        FillRotatedShape(hullCenter, hullAngle, 100, (lx, ly) =>
            IsInsideEllipse(lx, ly, 95f, 26f)
                ? HullColorAt(lx, ly, rng)
                : (Color?)null);

        // Cabina de cristal sobre el casco, con un brillo en la parte superior.
        // Glass cockpit on top of the hull, with a shine on the upper part.
        Vector2 cockpitCenter = hullCenter + Rotate(new Vector2(28f, -18f), hullAngle);
        FillRotatedShape(cockpitCenter, hullAngle, 32, (lx, ly) =>
        {
            if (!IsInsideEllipse(lx, ly, 28f, 12f))
                return null;

            float shine = Math.Clamp(-ly / 12f, 0f, 1f);
            return Color.Lerp(GlassColor, GlassShineColor, shine * 0.6f);
        });

        // Ala rota clavada en la nieve, con bordes de neón.
        // Broken wing stuck in the snow, with neon edges.
        Vector2 wingCenter = hullCenter + Rotate(new Vector2(5f, 26f), hullAngle);
        FillRotatedShape(wingCenter, hullAngle + 0.5f, 40, (lx, ly) =>
        {
            if (MathF.Abs(lx) > 36f || MathF.Abs(ly) > 5f)
                return null;

            return MathF.Abs(ly) > 3.5f ? NeonColor : Noisy(HullDarkColor, rng);
        });

        // Escombros sueltos a ambos lados del choque.
        // Loose debris on both sides of the crash.
        for (int i = 0; i < 6; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            int x = centerX + side * rng.Next(100, 170);
            var debrisCenter = new Vector2(x, GetSurfaceY(x) - 2);
            float angle = (float)(rng.NextDouble() * MathHelper.TwoPi);
            float halfWidth = rng.Next(5, 12);
            float halfHeight = rng.Next(2, 5);
            Color debrisColor = i % 3 == 0 ? NeonColor : HullColor;

            FillRotatedShape(debrisCenter, angle, 14, (lx, ly) =>
                MathF.Abs(lx) <= halfWidth && MathF.Abs(ly) <= halfHeight
                    ? Noisy(debrisColor, rng)
                    : (Color?)null);
        }
    }

    // Color de cada punto del casco: ventanas, franja de neón, paneles y zona superior más clara.
    // Color of each hull point: windows, neon stripe, panels and a lighter upper area.
    private static Color HullColorAt(float lx, float ly, Random rng)
    {
        // Ventanas circulares moradas.
        // Purple circular windows.
        foreach (float windowX in WindowPositions)
        {
            float dx = lx - windowX;
            float dy = ly + 7f;
            if (dx * dx + dy * dy <= 16f)
                return GlowColor;
        }

        // Altura relativa dentro del casco (-1 arriba, 1 abajo).
        // Relative height inside the hull (-1 top, 1 bottom).
        float v = ly / 26f;

        // Franja de neón verde a lo largo del casco.
        // Green neon stripe along the hull.
        if (MathF.Abs(v - 0.25f) < 0.09f)
            return NeonColor;

        // Parte superior más clara y líneas de paneles cada 22 píxeles.
        // Lighter upper part and panel lines every 22 pixels.
        Color color = v < -0.5f ? HullLightColor : HullColor;
        if (MathF.Abs(lx % 22f) < 1.2f)
            color = HullDarkColor;

        return Noisy(color, rng);
    }

    // Rasteriza una figura rotada: para cada píxel cercano calcula sus coordenadas locales
    // (deshaciendo la rotación) y le pregunta a "shader" de qué color es, o null si está fuera.
    // Rasterizes a rotated shape: for each nearby pixel it computes its local coordinates
    // (undoing the rotation) and asks the "shader" for its color, or null if it is outside.
    private void FillRotatedShape(Vector2 center, float angle, int extent, Func<float, float, Color?> shader)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        int cx = (int)center.X;
        int cy = (int)center.Y;

        for (int dy = -extent; dy <= extent; dy++)
        {
            for (int dx = -extent; dx <= extent; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                    continue;

                // Rotación inversa: de coordenadas del mapa a coordenadas de la figura.
                // Inverse rotation: from map coordinates to shape coordinates.
                float lx = dx * cos + dy * sin;
                float ly = -dx * sin + dy * cos;

                Color? color = shader(lx, ly);
                if (color.HasValue)
                    _pixels[y * Width + x] = color.Value;
            }
        }
    }

    private static bool IsInsideEllipse(float x, float y, float radiusX, float radiusY) =>
        (x / radiusX) * (x / radiusX) + (y / radiusY) * (y / radiusY) <= 1f;

    // Un punto está dentro si queda del mismo lado de los tres bordes del triángulo.
    // A point is inside if it lies on the same side of all three edges of the triangle.
    private static bool IsInsideTriangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
    {
        float Side(Vector2 p1, Vector2 p2) => (x - p2.X) * (p1.Y - p2.Y) - (p1.X - p2.X) * (y - p2.Y);

        float d1 = Side(a, b);
        float d2 = Side(b, c);
        float d3 = Side(c, a);
        bool hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }

    private static Vector2 Rotate(Vector2 vector, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
    }

    // Agrega una pequeña variación aleatoria al color para que no se vea plano.
    // Adds a small random variation to the color so it does not look flat.
    private static Color Noisy(Color color, Random rng)
    {
        int noise = rng.Next(-7, 8);
        return new Color(
            Math.Clamp(color.R + noise, 0, 255),
            Math.Clamp(color.G + noise, 0, 255),
            Math.Clamp(color.B + noise, 0, 255));
    }

    // CONSULTAS Y DESTRUCCIÓN
    // QUERIES AND DESTRUCTION

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