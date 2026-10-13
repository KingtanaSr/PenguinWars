// EFECTO VISUAL DE EXPLOSIÓN
// EXPLOSION VISUAL EFFECT

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.Combat;

// Destello que crece y se desvanece; el daño ya se aplicó al crearlo.
// Flash that grows and fades out; the damage was already applied when it was created.
public class Explosion
{
    private const float Duration = 0.35f;

    private readonly Vector2 _position;
    private readonly float _radius;
    private float _elapsed;

    public bool IsFinished => _elapsed >= Duration;

    public Explosion(Vector2 position, float radius)
    {
        _position = position;
        _radius = radius;
    }

    public void Update(float dt) => _elapsed += dt;

    public void Draw(SpriteBatch sb, Texture2D circle)
    {
        // Progreso de la animación entre 0 y 1.
        // Animation progress between 0 and 1.
        float t = Math.Min(1f, _elapsed / Duration);

        // Crece un poco y pasa de amarillo a rojo mientras se vuelve transparente.
        // Grows slightly and goes from yellow to red while becoming transparent.
        float diameter = _radius * 2f * (0.6f + 0.6f * t);
        float scale = diameter / circle.Width;
        Color color = Color.Lerp(Color.Yellow, Color.OrangeRed, t) * (1f - t);
        var origin = new Vector2(circle.Width / 2f, circle.Height / 2f);

        sb.Draw(circle, _position, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}