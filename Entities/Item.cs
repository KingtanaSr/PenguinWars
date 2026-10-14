// OBJETOS DEL MAPA: BOTIQUINES Y CAJAS DE MUNICIÓN
// MAP ITEMS: MEDKITS AND AMMO CRATES

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenguinWars.World;

namespace PenguinWars.Entities;

public enum ItemType
{
    // Se guarda en el inventario y se usa para curar.
    // Stored in the inventory and used to heal.
    Medkit,

    // Da munición al instante al recogerla.
    // Gives ammo instantly when picked up.
    AmmoCrate
}

public class Item
{
    // Tamaño del objeto en píxeles.
    // Item size in pixels.
    public const int Size = 14;

    private const float Gravity = 600f;
    private const float MaxFallSpeed = 600f;

    public ItemType Type { get; }

    // X es el centro horizontal; Y es la fila justo debajo del objeto.
    // X is the horizontal center; Y is the row right below the item.
    public int X { get; private set; }
    public int Y { get; private set; }

    // Indica si ya se recogió o se destruyó.
    // Indicates whether it was already picked up or destroyed.
    public bool IsRemoved { get; private set; }

    public Rectangle Bounds => new Rectangle(X - Size / 2, Y - Size, Size, Size);
    public Vector2 Center => new Vector2(X, Y - Size / 2f);

    private float _velocityY;

    // Fracciones de píxel acumuladas de la caída.
    // Accumulated pixel fractions of the fall.
    private float _remainderY;

    public Item(ItemType type, int x, int y)
    {
        Type = type;
        X = x;
        Y = y;
    }

    // Cae con gravedad si no hay suelo debajo (por ejemplo, tras una explosión).
    // Falls with gravity if there is no ground below (for example, after an explosion).
    public void Update(float dt, Terrain terrain)
    {
        if (IsRemoved)
            return;

        if (IsOnGround(terrain))
        {
            _velocityY = 0f;
            _remainderY = 0f;
            return;
        }

        _velocityY = Math.Min(_velocityY + Gravity * dt, MaxFallSpeed);
        _remainderY += _velocityY * dt;
        int move = (int)_remainderY;
        _remainderY -= move;

        // Bajar píxel a píxel hasta tocar el suelo.
        // Move down pixel by pixel until touching the ground.
        for (int i = 0; i < move; i++)
        {
            if (IsOnGround(terrain))
            {
                _velocityY = 0f;
                break;
            }

            Y++;
        }

        // Si cae fuera del mapa, se pierde.
        // If it falls off the map, it is lost.
        if (Y > terrain.Height + Size)
            IsRemoved = true;
    }

    // Revisa si hay terreno justo debajo del objeto.
    // Checks whether there is terrain right below the item.
    private bool IsOnGround(Terrain terrain)
    {
        int left = X - Size / 2;

        for (int xx = left; xx < left + Size; xx++)
        {
            if (terrain.IsSolid(xx, Y))
                return true;
        }

        return false;
    }

    // Marca el objeto como recogido o destruido.
    // Marks the item as picked up or destroyed.
    public void Remove() => IsRemoved = true;

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (IsRemoved)
            return;

        Rectangle box = Bounds;

        if (Type == ItemType.Medkit)
        {
            // Caja blanca con una cruz roja.
            // White box with a red cross.
            sb.Draw(pixel, box, Color.WhiteSmoke);
            sb.Draw(pixel, new Rectangle(box.X + 5, box.Y + 2, 4, 10), Color.Red);
            sb.Draw(pixel, new Rectangle(box.X + 2, box.Y + 5, 10, 4), Color.Red);
        }
        else
        {
            // Caja de madera con franjas y un detalle dorado.
            // Wooden crate with stripes and a golden detail.
            sb.Draw(pixel, box, new Color(110, 90, 50));
            sb.Draw(pixel, new Rectangle(box.X, box.Y + 4, box.Width, 2), new Color(70, 55, 30));
            sb.Draw(pixel, new Rectangle(box.X, box.Y + 9, box.Width, 2), new Color(70, 55, 30));
            sb.Draw(pixel, new Rectangle(box.X + 5, box.Y + 5, 4, 4), Color.Gold);
        }
    }
}