// PROYECTIL CON FÍSICA DE TIRO PARABÓLICO
// PROJECTILE WITH PROJECTILE-MOTION PHYSICS

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenguinWars.Entities;
using PenguinWars.World;

namespace PenguinWars.Combat;

public class Projectile
{
    // Misma gravedad que los pingüinos, para que todo se sienta coherente.
    // Same gravity as the penguins, so everything feels consistent.
    private const float Gravity = 600f;

    // Distancia fuera del mapa a partir de la cual el proyectil se pierde.
    // Distance outside the map after which the projectile is lost.
    private const float OutOfBoundsMargin = 100f;

    public Weapon Weapon { get; }
    public Vector2 Position { get; private set; }

    // Indica si el proyectil sigue en vuelo.
    // Indicates whether the projectile is still in flight.
    public bool IsActive { get; private set; } = true;

    // Indica si terminó chocando (explota) o saliendo del mapa (no explota).
    // Indicates whether it ended by hitting something (explodes) or leaving the map (no explosion).
    public bool HasExploded { get; private set; }

    private Vector2 _velocity;
    private readonly Penguin _shooter;

    // Ignora al pingüino que disparó hasta que el proyectil salga de su cuerpo.
    // Ignores the penguin who fired until the projectile leaves its body.
    private bool _ignoreShooter = true;

    public Projectile(Weapon weapon, Vector2 position, Vector2 velocity, Penguin shooter)
    {
        Weapon = weapon;
        Position = position;
        _velocity = velocity;
        _shooter = shooter;
    }

    public void Update(float dt, Terrain terrain, IReadOnlyList<Penguin> penguins, float windAcceleration)
    {
        if (!IsActive)
            return;

        // La gravedad tira hacia abajo y el viento empuja hacia los lados.
        // Gravity pulls down and wind pushes sideways.
        _velocity.Y += Gravity * Weapon.GravityScale * dt;
        _velocity.X += windAcceleration * Weapon.WindScale * dt;

        // Dividir el movimiento del frame en pasos de ~1 píxel para no atravesar nada.
        // Split this frame's movement into ~1 pixel steps so nothing is passed through.
        Vector2 movement = _velocity * dt;
        int steps = Math.Max(1, (int)MathF.Ceiling(movement.Length()));
        Vector2 step = movement / steps;

        for (int i = 0; i < steps; i++)
        {
            Position += step;

            // Dejar de ignorar al tirador cuando el proyectil ya salió de su cuerpo.
            // Stop ignoring the shooter once the projectile has left its body.
            if (_ignoreShooter && !_shooter.Bounds.Contains(Position))
                _ignoreShooter = false;

            // Choca con el terreno o con un pingüino: explota.
            // Hits the terrain or a penguin: explodes.
            if (terrain.IsSolid((int)Position.X, (int)Position.Y) || HitsPenguin(penguins))
            {
                IsActive = false;
                HasExploded = true;
                return;
            }

            // Salió del mapa: se pierde sin explotar.
            // Left the map: lost without exploding.
            if (IsOutOfBounds(terrain))
            {
                IsActive = false;
                return;
            }
        }
    }

    // Revisa si el proyectil está dentro de algún pingüino vivo.
    // Checks whether the projectile is inside any living penguin.
    private bool HitsPenguin(IReadOnlyList<Penguin> penguins)
    {
        foreach (Penguin penguin in penguins)
        {
            if (!penguin.IsAlive)
                continue;
            if (penguin == _shooter && _ignoreShooter)
                continue;
            if (penguin.Bounds.Contains(Position))
                return true;
        }

        return false;
    }

    // Por arriba no hay límite: el proyectil puede subir y volver a caer.
    // There is no limit above: the projectile can go up and fall back down.
    private bool IsOutOfBounds(Terrain terrain) =>
        Position.X < -OutOfBoundsMargin ||
        Position.X > terrain.Width + OutOfBoundsMargin ||
        Position.Y > terrain.Height + OutOfBoundsMargin;

    public void Draw(SpriteBatch sb, Texture2D circle)
    {
        float scale = Weapon.ProjectileSize / (float)circle.Width;
        var origin = new Vector2(circle.Width / 2f, circle.Height / 2f);
        sb.Draw(circle, Position, null, Weapon.ProjectileColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}