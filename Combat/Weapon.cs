// DEFINICIÓN DE LAS ARMAS
// WEAPON DEFINITIONS

using Microsoft.Xna.Framework;
using PenguinWars.Core;

namespace PenguinWars.Combat;

// Describe cómo se comporta un arma; cada proyectil usa estos valores.
// Describes how a weapon behaves; each projectile uses these values.
public class Weapon
{
    public string Name { get; }

    // Velocidad de salida con la potencia al máximo.
    // Launch speed at full power.
    public float MaxLaunchSpeed { get; }

    // Cuánto le afecta la gravedad (1 = normal).
    // How much gravity affects it (1 = normal).
    public float GravityScale { get; }

    // Cuánto le afecta el viento (1 = normal, 0 = nada).
    // How much wind affects it (1 = normal, 0 = none).
    public float WindScale { get; }

    public float ExplosionRadius { get; }
    public int MaxDamage { get; }
    public Color ProjectileColor { get; }

    // Diámetro del proyectil en píxeles.
    // Projectile diameter in pixels.
    public int ProjectileSize { get; }

    private Weapon(string name, float maxLaunchSpeed, float gravityScale, float windScale,
        float explosionRadius, int maxDamage, Color projectileColor, int projectileSize)
    {
        Name = name;
        MaxLaunchSpeed = maxLaunchSpeed;
        GravityScale = gravityScale;
        WindScale = windScale;
        ExplosionRadius = explosionRadius;
        MaxDamage = maxDamage;
        ProjectileColor = projectileColor;
        ProjectileSize = projectileSize;
    }

    // Bazooka: pesada, muy afectada por el viento, explosión grande.
    // Bazooka: heavy, strongly affected by wind, big explosion.
    public static readonly Weapon Bazooka =
        new("Bazooka", 750f, 1f, 1f, 40f, 45, new Color(110, 120, 70), 8);

    // Rifle de Plasma: rápido, cae menos, casi inmune al viento, menos daño.
    // Plasma Rifle: fast, falls less, nearly immune to wind, less damage.
    public static readonly Weapon PlasmaRifle =
        new("Rifle de Plasma", 900f, 0.6f, 0.3f, 30f, 35, new Color(120, 255, 90), 7);

    // Arma inicial de cada equipo (en la Fase 7 habrá más armas para elegir).
    // Starting weapon for each team (Phase 7 will add more weapons to choose from).
    public static Weapon ForTeam(Team team) =>
        team == Team.Heroes ? Bazooka : PlasmaRifle;
}