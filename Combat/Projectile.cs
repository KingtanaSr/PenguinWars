// PROYECTIL CON FÍSICA DE TIRO PARABÓLICO, REBOTES Y TEMPORIZADOR
// PROJECTILE WITH PROJECTILE-MOTION PHYSICS, BOUNCES AND FUSE TIMER

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

    // Por debajo de esta velocidad, una granada en el suelo se queda quieta.
    // Below this speed, a grenade on the ground comes to rest.
    private const float RestSpeed = 40f;

    // Velocidad horizontal que conserva al rebotar contra el suelo.
    // Horizontal speed kept when bouncing off the ground.
    private const float GroundFriction = 0.8f;

    public Weapon Weapon { get; }
    public Vector2 Position { get; private set; }

    // Indica si el proyectil sigue en juego.
    // Indicates whether the projectile is still in play.
    public bool IsActive { get; private set; } = true;

    // Indica si terminó explotando (o si se perdió fuera del mapa).
    // Indicates whether it ended by exploding (or got lost outside the map).
    public bool HasExploded { get; private set; }

    private Vector2 _velocity;
    private readonly Penguin _shooter;

    // Ignora al pingüino que disparó hasta que el proyectil salga de su cuerpo.
    // Ignores the penguin who fired until the projectile leaves its body.
    private bool _ignoreShooter = true;

    // Tiempo restante para explotar (solo armas con temporizador).
    // Time left before exploding (timed weapons only).
    private float _fuseLeft;

    // Indica si la granada está quieta en el suelo.
    // Indicates whether the grenade is resting on the ground.
    private bool _isResting;

    public Projectile(Weapon weapon, Vector2 position, Vector2 velocity, Penguin shooter)
    {
        Weapon = weapon;
        Position = position;
        _velocity = velocity;
        _shooter = shooter;
        _fuseLeft = weapon.FuseTime;
    }

    public void Update(float dt, Terrain terrain, IReadOnlyList<Penguin> penguins, float windAcceleration)
    {
        if (!IsActive)
            return;

        // Las armas con temporizador explotan al llegar a cero, estén donde estén.
        // Timed weapons explode when reaching zero, wherever they are.
        if (Weapon.IsTimed)
        {
            _fuseLeft -= dt;
            if (_fuseLeft <= 0f)
            {
                Explode();
                return;
            }
        }

        // Si está quieta, solo vuelve a moverse si el suelo desaparece.
        // If it is resting, it only moves again if the ground disappears.
        if (_isResting)
        {
            if (terrain.IsSolid((int)Position.X, (int)Position.Y + 1))
                return;

            _isResting = false;
        }

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
            Vector2 next = Position + step;

            // El siguiente paso choca con el terreno.
            // The next step hits the terrain.
            if (terrain.IsSolid((int)next.X, (int)next.Y))
            {
                // Las granadas rebotan; el resto explota.
                // Grenades bounce; everything else explodes.
                if (Weapon.IsTimed)
                {
                    Bounce(terrain, step);
                    return;
                }

                Position = next;
                Explode();
                return;
            }

            Position = next;

            // Dejar de ignorar al tirador cuando el proyectil ya salió de su cuerpo.
            // Stop ignoring the shooter once the projectile has left its body.
            if (_ignoreShooter && !_shooter.Bounds.Contains(Position))
                _ignoreShooter = false;

            // Las armas de impacto explotan al tocar un pingüino; las granadas pasan.
            // Impact weapons explode when touching a penguin; grenades pass by.
            if (!Weapon.IsTimed && HitsPenguin(penguins))
            {
                Explode();
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

    // Invierte la velocidad en el eje del choque y pierde energía.
    // Reverses the velocity on the collision axis and loses energy.
    private void Bounce(Terrain terrain, Vector2 step)
    {
        bool hitX = terrain.IsSolid((int)(Position.X + step.X), (int)Position.Y);
        bool hitY = terrain.IsSolid((int)Position.X, (int)(Position.Y + step.Y));

        if (hitX)
            _velocity.X = -_velocity.X * Weapon.Bounciness;

        if (hitY)
        {
            _velocity.Y = -_velocity.Y * Weapon.Bounciness;
            _velocity.X *= GroundFriction;
        }

        // Choque en diagonal (esquina): rebota en ambos ejes.
        // Diagonal hit (corner): bounces on both axes.
        if (!hitX && !hitY)
            _velocity = -_velocity * Weapon.Bounciness;

        // Si va muy lento y hay suelo debajo, se queda quieta.
        // If it is very slow and there is ground below, it comes to rest.
        if (_velocity.Length() < RestSpeed && terrain.IsSolid((int)Position.X, (int)Position.Y + 1))
        {
            _velocity = Vector2.Zero;
            _isResting = true;
        }
    }

    private void Explode()
    {
        IsActive = false;
        HasExploded = true;
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

    // Por arriba solo se pierden los disparos sin gravedad (los demás vuelven a caer).
    // Upwards, only gravity-free shots are lost (the rest fall back down).
    private bool IsOutOfBounds(Terrain terrain) =>
        Position.X < -OutOfBoundsMargin ||
        Position.X > terrain.Width + OutOfBoundsMargin ||
        Position.Y > terrain.Height + OutOfBoundsMargin ||
        (Weapon.GravityScale <= 0f && Position.Y < -OutOfBoundsMargin);

    public void Draw(SpriteBatch sb, Texture2D circle, SpriteFont font)
    {
        float scale = Weapon.ProjectileSize / (float)circle.Width;
        var origin = new Vector2(circle.Width / 2f, circle.Height / 2f);
        sb.Draw(circle, Position, null, Weapon.ProjectileColor, 0f, origin, scale, SpriteEffects.None, 0f);

        // Cuenta regresiva sobre las granadas.
        // Countdown above grenades.
        if (Weapon.IsTimed)
        {
            string text = ((int)MathF.Ceiling(_fuseLeft)).ToString();
            const float textScale = 0.45f;
            Vector2 size = font.MeasureString(text) * textScale;
            sb.DrawString(font, text, new Vector2(Position.X - size.X / 2f, Position.Y - 22f), Color.White,
                0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }
    }
}