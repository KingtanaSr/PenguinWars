// DEFINICIÓN DE LAS ARMAS Y ARSENALES DE CADA EQUIPO
// WEAPON DEFINITIONS AND EACH TEAM'S ARSENAL

using Microsoft.Xna.Framework;
using PenguinWars.Core;

namespace PenguinWars.Combat;

// Describe cómo se comporta un arma; cada proyectil usa estos valores.
// Describes how a weapon behaves; each projectile uses these values.
public class Weapon
{
    public string Name { get; init; }

    // Velocidad de salida con la potencia al máximo.
    // Launch speed at full power.
    public float MaxLaunchSpeed { get; init; }

    // Cuánto le afectan la gravedad y el viento (1 = normal, 0 = nada).
    // How much gravity and wind affect it (1 = normal, 0 = none).
    public float GravityScale { get; init; } = 1f;
    public float WindScale { get; init; } = 1f;

    public float ExplosionRadius { get; init; }
    public int MaxDamage { get; init; }

    // Empuje de la explosión: 1 = normal, negativo = atrae.
    // Explosion knockback: 1 = normal, negative = pulls.
    public float KnockbackScale { get; init; } = 1f;

    // Segundos hasta explotar; 0 significa que explota al impactar.
    // Seconds until it explodes; 0 means it explodes on impact.
    public float FuseTime { get; init; }

    // Cuánta velocidad conserva al rebotar (solo armas con temporizador).
    // How much speed it keeps when bouncing (timed weapons only).
    public float Bounciness { get; init; } = 0.45f;

    // Si es falso, dispara al instante con potencia máxima (sin cargar).
    // If false, it fires instantly at full power (no charging).
    public bool UsesCharge { get; init; } = true;

    // Munición inicial; -1 significa ilimitada.
    // Starting ammo; -1 means unlimited.
    public int StartingAmmo { get; init; } = -1;

    public Color ProjectileColor { get; init; }
    public int ProjectileSize { get; init; } = 8;

    public bool IsTimed => FuseTime > 0f;
    public bool HasUnlimitedAmmo => StartingAmmo < 0;

    // ARMAS TERRESTRES
    // EARTH WEAPONS

    // Bazooka: explota al impactar, muy afectada por el viento.
    // Bazooka: explodes on impact, strongly affected by wind.
    public static readonly Weapon Bazooka = new()
    {
        Name = "Bazooka",
        MaxLaunchSpeed = 750f,
        ExplosionRadius = 40f,
        MaxDamage = 45,
        ProjectileColor = new Color(110, 120, 70)
    };

    // Granada: rebota y explota con temporizador; no le afecta el viento.
    // Grenade: bounces and explodes with a timer; not affected by wind.
    public static readonly Weapon Grenade = new()
    {
        Name = "Granada",
        MaxLaunchSpeed = 650f,
        WindScale = 0f,
        ExplosionRadius = 45f,
        MaxDamage = 50,
        FuseTime = 3f,
        StartingAmmo = 3,
        ProjectileColor = new Color(70, 110, 50),
        ProjectileSize = 9
    };

    // Escopeta: disparo recto e instantáneo, daño pequeño y preciso.
    // Shotgun: straight instant shot, small and precise damage.
    public static readonly Weapon Shotgun = new()
    {
        Name = "Escopeta",
        MaxLaunchSpeed = 1500f,
        GravityScale = 0f,
        WindScale = 0f,
        ExplosionRadius = 14f,
        MaxDamage = 25,
        UsesCharge = false,
        StartingAmmo = 2,
        ProjectileColor = Color.LightGoldenrodYellow,
        ProjectileSize = 4
    };

    // ARMAS ALIENÍGENAS
    // ALIEN WEAPONS

    // Rifle de Plasma: rápido, cae menos, casi inmune al viento.
    // Plasma Rifle: fast, falls less, nearly immune to wind.
    public static readonly Weapon PlasmaRifle = new()
    {
        Name = "Rifle de Plasma",
        MaxLaunchSpeed = 900f,
        GravityScale = 0.6f,
        WindScale = 0.3f,
        ExplosionRadius = 30f,
        MaxDamage = 35,
        ProjectileColor = new Color(120, 255, 90),
        ProjectileSize = 7
    };

    // Granada Gravitacional: rebota, explota con temporizador y atrae a los pingüinos.
    // Gravity Grenade: bounces, explodes with a timer and pulls penguins in.
    public static readonly Weapon GravityGrenade = new()
    {
        Name = "Granada Gravitacional",
        MaxLaunchSpeed = 650f,
        WindScale = 0f,
        ExplosionRadius = 45f,
        MaxDamage = 30,
        KnockbackScale = -1.2f,
        FuseTime = 3f,
        StartingAmmo = 3,
        ProjectileColor = new Color(150, 70, 220),
        ProjectileSize = 9
    };

    // Pistola de Rayos: rayo recto e instantáneo.
    // Ray Gun: straight instant beam.
    public static readonly Weapon RayGun = new()
    {
        Name = "Pistola de Rayos",
        MaxLaunchSpeed = 1600f,
        GravityScale = 0f,
        WindScale = 0f,
        ExplosionRadius = 14f,
        MaxDamage = 28,
        UsesCharge = false,
        StartingAmmo = 2,
        ProjectileColor = Color.Magenta,
        ProjectileSize = 4
    };

    // Arsenal de cada equipo, en el orden de las teclas 1, 2 y 3.
    // Each team's arsenal, in the order of keys 1, 2 and 3.
    public static Weapon[] GetTeamWeapons(Team team) => team == Team.Heroes
        ? new[] { Bazooka, Grenade, Shotgun }
        : new[] { PlasmaRifle, GravityGrenade, RayGun };
}