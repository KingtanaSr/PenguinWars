// PINGÜINO: MOVIMIENTO, FÍSICA, VIDA Y DIBUJO
// PENGUIN: MOVEMENT, PHYSICS, HEALTH AND DRAWING

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenguinWars.Core;
using PenguinWars.World;

namespace PenguinWars.Entities;

public class Penguin
{
    // Tamaño de la caja de colisión del pingüino, en píxeles.
    // Size of the penguin's collision box, in pixels.
    public const int Width = 20;
    public const int Height = 28;

    public const int MaxHealth = 100;

    // Constantes de movimiento y física.
    // Movement and physics constants.
    private const float WalkSpeed = 60f;          // píxeles por segundo / pixels per second
    private const int MaxStepHeight = 6;          // escalón máximo que puede subir / max step it can climb
    private const float Gravity = 600f;           // aceleración hacia abajo / downward acceleration
    private const float MaxFallSpeed = 900f;      // velocidad máxima de caída / max fall speed
    private const float JumpSpeedX = 90f;         // impulso horizontal del salto / horizontal jump impulse
    private const float JumpSpeedY = 230f;        // impulso vertical del salto / vertical jump impulse
    private const int SafeFallDistance = 60;      // caída sin daño / fall without damage
    private const int FallDamageDivisor = 3;      // cada 3 píxeles extra = 1 de daño / every 3 extra pixels = 1 damage

    public string Name { get; }
    public Team Team { get; }
    public PenguinKind Kind { get; }
    public int Health { get; private set; } = MaxHealth;
    public bool IsAlive => Health > 0;
    public bool IsGrounded { get; private set; } = true;
    public bool FacingRight { get; private set; }

    // Indica si murió ahogado (cayó fuera del mapa).
    // Indicates whether it drowned (fell off the map).
    public bool Drowned { get; private set; }

    // X es el centro horizontal; Y es la fila justo debajo de los pies.
    // X is the horizontal center; Y is the row right below the feet.
    public int X { get; private set; }
    public int Y { get; private set; }

    // Centro del cuerpo, usado para calcular distancias a explosiones.
    // Body center, used to calculate distances to explosions.
    public Vector2 Center => new Vector2(X, Y - Height / 2f);

    // Caja de colisión del pingüino en coordenadas del mapa.
    // Penguin collision box in map coordinates.
    public Rectangle Bounds => new Rectangle(X - Width / 2, Y - Height, Width, Height);

    // Punto desde donde sale el disparo (a la altura del pecho).
    // Point where the shot comes from (at chest height).
    public Vector2 GunOrigin => new Vector2(X, Y - Height / 2f - 2f);

    // Hace que el pingüino mire hacia una posición horizontal (por ejemplo, el ratón).
    // Makes the penguin face a horizontal position (for example, the mouse).
    public void FaceTowards(float targetX)
    {
        if (IsAlive)
            FacingRight = targetX >= X;
    }

    private Vector2 _velocity;

    // Fracciones de píxel acumuladas del movimiento en el aire.
    // Accumulated pixel fractions of airborne movement.
    private Vector2 _remainder;

    // Fracciones de píxel acumuladas al caminar.
    // Accumulated pixel fractions while walking.
    private float _walkAccumulator;

    // Punto más alto alcanzado en el aire, para calcular el daño por caída.
    // Highest point reached in the air, used to calculate fall damage.
    private int _highestY;

    public Penguin(PenguinKind kind, Team team, int x, int y)
    {
        Kind = kind;
        Team = team;
        Name = PenguinProfiles.GetName(kind);
        X = x;
        Y = y;
        _highestY = y;

        // Cada equipo empieza mirando hacia el enemigo.
        // Each team starts facing the enemy.
        FacingRight = team == Team.Heroes;
    }

    // Actualiza el pingüino. moveDirection: -1 izquierda, 0 quieto, 1 derecha.
    // Updates the penguin. moveDirection: -1 left, 0 idle, 1 right.
    public void Update(float dt, Terrain terrain, int moveDirection, bool jumpRequested)
    {
        if (!IsAlive)
            return;

        if (IsGrounded)
        {
            if (moveDirection != 0)
            {
                FacingRight = moveDirection > 0;
                Walk(terrain, moveDirection, dt);
            }
            else
            {
                _walkAccumulator = 0f;
            }

            if (jumpRequested && IsGrounded)
            {
                // Saltar hacia donde mira.
                // Jump towards the facing direction.
                Launch(new Vector2((FacingRight ? 1 : -1) * JumpSpeedX, -JumpSpeedY));
            }
            else if (IsGrounded && !IsOnGround(terrain, X, Y))
            {
                // El suelo desapareció (por ejemplo, por una explosión).
                // The ground disappeared (for example, because of an explosion).
                StartFalling();
            }
        }

        if (!IsGrounded)
            UpdateAirborne(terrain, dt);

        // Si cae por debajo del mapa, se ahoga en el agua helada.
        // If it falls below the map, it drowns in the icy water.
        if (Y > terrain.Height + Height)
        {
            Health = 0;
            Drowned = true;
        }
    }

    // Camina píxel a píxel, subiendo escalones pequeños y bajando pendientes.
    // Walks pixel by pixel, climbing small steps and going down slopes.
    private void Walk(Terrain terrain, int direction, float dt)
    {
        _walkAccumulator += WalkSpeed * dt;

        while (_walkAccumulator >= 1f)
        {
            _walkAccumulator -= 1f;
            int nextX = X + direction;

            // No salir por los bordes del mapa.
            // Do not leave through the edges of the map.
            if (nextX < Width / 2 || nextX > terrain.Width - Width / 2)
            {
                _walkAccumulator = 0f;
                return;
            }

            // Probar avanzar subiendo de 0 a MaxStepHeight píxeles.
            // Try to move forward climbing from 0 to MaxStepHeight pixels.
            bool moved = false;
            for (int step = 0; step <= MaxStepHeight; step++)
            {
                if (!CollidesAt(terrain, nextX, Y - step))
                {
                    X = nextX;
                    Y -= step;
                    moved = true;
                    break;
                }
            }

            // Pared demasiado alta: detenerse.
            // Wall too high: stop.
            if (!moved)
            {
                _walkAccumulator = 0f;
                return;
            }

            // Si no hay suelo cerca debajo, empieza a caer.
            // If there is no ground close below, start falling.
            if (!SnapToGround(terrain))
            {
                StartFalling();
                return;
            }
        }
    }

    // Baja hasta MaxStepHeight píxeles para pegarse al suelo en pendientes.
    // Drops up to MaxStepHeight pixels to stick to the ground on slopes.
    private bool SnapToGround(Terrain terrain)
    {
        if (IsOnGround(terrain, X, Y))
            return true;

        for (int drop = 1; drop <= MaxStepHeight; drop++)
        {
            if (CollidesAt(terrain, X, Y + drop))
                return false;

            if (IsOnGround(terrain, X, Y + drop))
            {
                Y += drop;
                return true;
            }
        }

        return false;
    }

    // Movimiento en el aire con gravedad, píxel a píxel para no atravesar el terreno.
    // Airborne movement with gravity, pixel by pixel to avoid passing through terrain.
    private void UpdateAirborne(Terrain terrain, float dt)
    {
        _velocity.Y = Math.Min(_velocity.Y + Gravity * dt, MaxFallSpeed);
        _remainder += _velocity * dt;

        // Movimiento horizontal.
        // Horizontal movement.
        int moveX = (int)_remainder.X;
        _remainder.X -= moveX;
        int stepX = Math.Sign(moveX);

        for (int i = 0; i < Math.Abs(moveX); i++)
        {
            int nextX = X + stepX;
            bool outOfBounds = nextX < Width / 2 || nextX > terrain.Width - Width / 2;

            if (outOfBounds || CollidesAt(terrain, nextX, Y))
            {
                // Choca con una pared: pierde la velocidad horizontal.
                // Hits a wall: loses horizontal speed.
                _velocity.X = 0f;
                _remainder.X = 0f;
                break;
            }

            X = nextX;
        }

        // Movimiento vertical.
        // Vertical movement.
        int moveY = (int)_remainder.Y;
        _remainder.Y -= moveY;
        int stepY = Math.Sign(moveY);

        for (int i = 0; i < Math.Abs(moveY); i++)
        {
            // Bajando y hay suelo justo debajo: aterriza.
            // Going down and there is ground right below: land.
            if (stepY > 0 && IsOnGround(terrain, X, Y))
            {
                Land();
                return;
            }

            // Subiendo y hay terreno sobre la cabeza: se detiene.
            // Going up and there is terrain above the head: stop.
            if (stepY < 0 && CollidesAt(terrain, X, Y - 1))
            {
                _velocity.Y = 0f;
                _remainder.Y = 0f;
                break;
            }

            Y += stepY;
        }

        _highestY = Math.Min(_highestY, Y);

        // Aterrizar si ya está cayendo y toca el suelo.
        // Land if it is already falling and touches the ground.
        if (_velocity.Y >= 0f && IsOnGround(terrain, X, Y))
            Land();
    }

    // Lanza al pingüino con una velocidad inicial (salto o explosión).
    // Launches the penguin with an initial velocity (jump or explosion).
    private void Launch(Vector2 velocity)
    {
        IsGrounded = false;
        _velocity = velocity;
        _remainder = Vector2.Zero;
        _highestY = Y;
    }

    private void StartFalling() => Launch(Vector2.Zero);

    // Aterriza y aplica daño si la caída fue muy alta.
    // Lands and applies damage if the fall was too high.
    private void Land()
    {
        IsGrounded = true;
        int fallDistance = Y - _highestY;

        if (fallDistance > SafeFallDistance)
            TakeDamage((fallDistance - SafeFallDistance) / FallDamageDivisor);

        _velocity = Vector2.Zero;
        _remainder = Vector2.Zero;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
            return;

        Health = Math.Max(0, Health - amount);
    }

    // Daño y empuje por una explosión cercana: más fuerte cuanto más cerca.
    // Damage and knockback from a nearby explosion: stronger the closer it is.
    public void ApplyExplosion(Vector2 center, float radius, int maxDamage)
    {
        if (!IsAlive)
            return;

        Vector2 offset = Center - center;
        float distance = offset.Length();

        // Alcance un poco mayor que el cráter.
        // Reach slightly larger than the crater.
        float reach = radius * 1.5f;
        if (distance >= reach)
            return;

        float strength = 1f - distance / reach;
        TakeDamage((int)(maxDamage * strength));

        // Empujar hacia afuera de la explosión y un poco hacia arriba.
        // Push away from the explosion and slightly upwards.
        Vector2 direction = distance > 0.01f ? offset / distance : -Vector2.UnitY;
        Vector2 knockback = direction * 350f * strength;
        knockback.Y -= 120f * strength;
        Launch(knockback);
    }

    // Revisa si la caja del pingüino tocaría terreno en la posición dada.
    // Checks whether the penguin's box would touch terrain at the given position.
    private static bool CollidesAt(Terrain terrain, int x, int y)
    {
        int left = x - Width / 2;

        for (int yy = y - Height; yy < y; yy++)
        {
            for (int xx = left; xx < left + Width; xx++)
            {
                if (terrain.IsSolid(xx, yy))
                    return true;
            }
        }

        return false;
    }

    // Revisa si hay terreno en la fila justo debajo de los pies.
    // Checks whether there is terrain in the row right below the feet.
    private static bool IsOnGround(Terrain terrain, int x, int y)
    {
        int left = x - Width / 2;

        for (int xx = left; xx < left + Width; xx++)
        {
            if (terrain.IsSolid(xx, y))
                return true;
        }

        return false;
    }

    // Dibuja el pingüino provisional con rectángulos, su nombre y su barra de vida.
    // Draws the temporary penguin with rectangles, its name and its health bar.
    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font, bool isSelected)
    {
        var body = new Rectangle(X - Width / 2, Y - Height, Width, Height);

        if (!IsAlive)
        {
            // Lápida si murió en tierra; nada si se ahogó.
            // Tombstone if it died on land; nothing if it drowned.
            if (!Drowned)
            {
                sb.Draw(pixel, new Rectangle(X - 6, Y - 14, 12, 14), Color.Gray);
                sb.Draw(pixel, new Rectangle(X - 1, Y - 12, 2, 8), Color.DimGray);
                sb.Draw(pixel, new Rectangle(X - 4, Y - 10, 8, 2), Color.DimGray);
            }
            return;
        }

        // Cuerpo oscuro.
        // Dark body.
        sb.Draw(pixel, body, new Color(25, 28, 40));

        // Panza blanca, desplazada hacia donde mira.
        // White belly, shifted towards the facing direction.
        int bellyX = FacingRight ? body.X + 7 : body.X + 3;
        sb.Draw(pixel, new Rectangle(bellyX, body.Y + 10, 10, 15), Color.WhiteSmoke);

        // Accesorio en la cabeza (gorra, casco, armadura) con el color del personaje.
        // Head accessory (cap, helmet, armor) with the character's color.
        sb.Draw(pixel, new Rectangle(body.X, body.Y, Width, 5), PenguinProfiles.GetAccentColor(Kind));

        // Ojo y pico.
        // Eye and beak.
        int eyeX = FacingRight ? body.Right - 7 : body.X + 4;
        sb.Draw(pixel, new Rectangle(eyeX, body.Y + 7, 3, 3), Color.White);
        int beakX = FacingRight ? body.Right : body.X - 4;
        sb.Draw(pixel, new Rectangle(beakX, body.Y + 11, 4, 3), Color.Orange);

        DrawHealthBar(sb, pixel, body);

        // Nombre sobre la barra de vida, con el color del equipo.
        // Name above the health bar, with the team color.
        const float nameScale = 0.45f;
        Vector2 nameSize = font.MeasureString(Name) * nameScale;
        var namePosition = new Vector2(X - nameSize.X / 2f, body.Y - 24);
        sb.DrawString(font, Name, namePosition, PenguinProfiles.GetTeamColor(Team),
            0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);

        // Flecha dorada sobre el pingüino seleccionado.
        // Golden arrow above the selected penguin.
        if (isSelected)
        {
            int arrowY = body.Y - 38;
            sb.Draw(pixel, new Rectangle(X - 5, arrowY, 11, 3), Color.Gold);
            sb.Draw(pixel, new Rectangle(X - 3, arrowY + 3, 7, 3), Color.Gold);
            sb.Draw(pixel, new Rectangle(X - 1, arrowY + 6, 3, 3), Color.Gold);
        }
    }

    // Barra de vida: verde, amarilla o roja según la vida restante.
    // Health bar: green, yellow or red depending on remaining health.
    private void DrawHealthBar(SpriteBatch sb, Texture2D pixel, Rectangle body)
    {
        const int barWidth = 30;
        int barX = X - barWidth / 2;
        int barY = body.Y - 10;
        float ratio = Health / (float)MaxHealth;

        Color fillColor = ratio > 0.5f ? Color.LimeGreen
                        : ratio > 0.25f ? Color.Gold
                        : Color.Red;

        // Borde con el color del equipo, fondo oscuro y relleno.
        // Border with the team color, dark background and fill.
        sb.Draw(pixel, new Rectangle(barX - 1, barY - 1, barWidth + 2, 6), PenguinProfiles.GetTeamColor(Team));
        sb.Draw(pixel, new Rectangle(barX, barY, barWidth, 4), new Color(20, 20, 20));
        sb.Draw(pixel, new Rectangle(barX, barY, (int)(barWidth * ratio), 4), fillColor);
    }
}