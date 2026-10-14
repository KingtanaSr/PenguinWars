// NIEVE CAYENDO: SISTEMA DE PARTÍCULAS QUE SIGUE AL VIENTO
// FALLING SNOW: PARTICLE SYSTEM THAT FOLLOWS THE WIND

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.World;

public class Snowfall
{
    // Cuánto empuja el viento a los copos (píxeles por segundo con viento máximo).
    // How much the wind pushes the flakes (pixels per second at maximum wind).
    private const float WindDrift = 70f;

    // Un copo de nieve. Es un struct: datos pequeños guardados juntos en memoria.
    // A snowflake. It is a struct: small data stored together in memory.
    private struct Flake
    {
        public Vector2 Position;
        public float FallSpeed;
        public int Size;
        public float SwayPhase;
    }

    private readonly Flake[] _flakes;
    private readonly Random _random;
    private readonly int _width;
    private readonly int _height;
    private float _time;

    public Snowfall(int count, int width, int height, Random random)
    {
        _width = width;
        _height = height;
        _random = random;
        _flakes = new Flake[count];

        for (int i = 0; i < count; i++)
        {
            _flakes[i] = new Flake
            {
                Position = new Vector2(random.Next(width), random.Next(height)),
                FallSpeed = 25f + (float)random.NextDouble() * 45f,
                Size = random.NextDouble() < 0.3 ? 3 : 2,
                SwayPhase = (float)random.NextDouble() * MathHelper.TwoPi
            };
        }
    }

    // Mueve los copos: caen, se balancean y el viento los empuja hacia los lados.
    // Moves the flakes: they fall, sway and the wind pushes them sideways.
    public void Update(float dt, float windStrength)
    {
        _time += dt;

        for (int i = 0; i < _flakes.Length; i++)
        {
            // "ref" modifica el copo directamente en el arreglo, sin copiarlo.
            // "ref" modifies the flake directly in the array, without copying it.
            ref Flake flake = ref _flakes[i];

            float sway = MathF.Sin(_time * 1.5f + flake.SwayPhase) * 12f;
            flake.Position.X += (windStrength * WindDrift + sway) * dt;
            flake.Position.Y += flake.FallSpeed * dt;

            // Al salir por abajo, vuelve a aparecer arriba en otra posición.
            // When it leaves through the bottom, it reappears at the top in another position.
            if (flake.Position.Y > _height)
            {
                flake.Position.Y = -5f;
                flake.Position.X = _random.Next(_width);
            }

            // Si sale por los lados, entra por el lado contrario.
            // If it leaves through the sides, it enters from the opposite side.
            if (flake.Position.X < -5f)
                flake.Position.X = _width + 5f;
            else if (flake.Position.X > _width + 5f)
                flake.Position.X = -5f;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (Flake flake in _flakes)
        {
            var rect = new Rectangle((int)flake.Position.X, (int)flake.Position.Y, flake.Size, flake.Size);
            spriteBatch.Draw(pixel, rect, Color.White * 0.8f);
        }
    }
}