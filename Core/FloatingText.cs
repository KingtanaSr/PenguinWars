// TEXTO FLOTANTE QUE SUBE Y SE DESVANECE
// FLOATING TEXT THAT RISES AND FADES OUT

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenguinWars.Core;

public class FloatingText
{
    private const float Duration = 1.4f;
    private const float RiseSpeed = 30f;
    private const float Scale = 0.55f;

    private readonly string _text;
    private readonly Color _color;
    private Vector2 _position;
    private float _elapsed;

    public bool IsFinished => _elapsed >= Duration;

    public FloatingText(string text, Vector2 position, Color color)
    {
        _text = text;
        _position = position;
        _color = color;
    }

    // Sube lentamente con el tiempo.
    // Rises slowly over time.
    public void Update(float dt)
    {
        _elapsed += dt;
        _position.Y -= RiseSpeed * dt;
    }

    // Se dibuja centrado y se vuelve transparente al final.
    // Drawn centered and becomes transparent at the end.
    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        float alpha = 1f - Math.Min(1f, _elapsed / Duration);
        Vector2 size = font.MeasureString(_text) * Scale;
        var position = new Vector2(_position.X - size.X / 2f, _position.Y);
        sb.DrawString(font, _text, position, _color * alpha, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
    }
}