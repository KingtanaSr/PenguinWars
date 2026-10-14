// PLANIFICADOR DE DISPAROS: SIMULA CANDIDATOS Y ELIGE EL MEJOR
// SHOT PLANNER: SIMULATES CANDIDATES AND PICKS THE BEST ONE

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PenguinWars.Combat;
using PenguinWars.Entities;
using PenguinWars.Gameplay;
using PenguinWars.World;

namespace PenguinWars.AI;

// Resultado de evaluar un disparo posible.
// Result of evaluating a possible shot.
public sealed class ShotCandidate
{
    public int WeaponIndex { get; init; }
    public Weapon Weapon { get; init; }
    public Vector2 Direction { get; init; }
    public float Power { get; init; }
    public float Score { get; init; }
    public float ExpectedEnemyDamage { get; init; }
    public bool KillsEnemy { get; init; }
}

public class ShotPlanner
{
    // Paso de tiempo de la simulación: el mismo del juego (60 frames por segundo).
    // Simulation time step: the same as the game (60 frames per second).
    private const float SimulationStep = 1f / 60f;

    // Límite de pasos por simulación (10 segundos de vuelo).
    // Step limit per simulation (10 seconds of flight).
    private const int MaxSimulationSteps = 600;

    // Rango y separación de los ángulos probados (0 = horizontal, positivo = hacia arriba).
    // Range and spacing of the tested angles (0 = horizontal, positive = upwards).
    private const float MaxAngleDegrees = 84f;
    private const float AngleStepDegrees = 4f;

    // Pesos de la puntuación.
    // Score weights.
    private const float KillBonus = 40f;
    private const float FriendlyFirePenalty = 1.5f;
    private const float DistancePenalty = 0.02f;

    // Peso extra para el objetivo prioritario elegido por el estratega.
    // Extra weight for the priority target chosen by the strategist.
    private const float FocusDamageWeight = 1f;
    private const float FocusKillBonus = 30f;

    // Lados hacia los que se prueba disparar: izquierda y derecha.
    // Sides to test shooting towards: left and right.
    private static readonly int[] Sides = { -1, 1 };

    // Potencias probadas: de 0.25 a 1.0 para armas con carga; solo 1.0 para las demás.
    // Tested powers: 0.25 to 1.0 for charged weapons; only 1.0 for the others.
    private static readonly float[] ChargedPowers =
        Enumerable.Range(0, 11).Select(i => 0.25f + i * 0.075f).ToArray();
    private static readonly float[] FullPower = { 1f };

    // Mejor disparo encontrado hasta ahora (null si no hay ninguno útil).
    // Best shot found so far (null if there is no useful one).
    public ShotCandidate Best { get; private set; }

    // Evalúa todos los candidatos, devolviendo el control después de cada simulación.
    // Evaluates all candidates, yielding control after each simulation.
    // focusTarget: enemigo prioritario (opcional), por ejemplo el que eligió el LLM.
    // focusTarget: priority enemy (optional), for example the one chosen by the LLM.
    public IEnumerable<bool> Evaluate(Penguin shooter, TeamInventory inventory, Terrain terrain,
        IReadOnlyList<Penguin> penguins, float windAcceleration, float muzzleDistance, Penguin focusTarget = null)
    {
        Best = null;

        if (!penguins.Any(p => p.IsAlive && p.Team != shooter.Team))
            yield break;

        for (int weaponIndex = 0; weaponIndex < inventory.Weapons.Count; weaponIndex++)
        {
            Weapon weapon = inventory.Weapons[weaponIndex];
            if (!inventory.HasAmmo(weapon))
                continue;

            float[] powers = weapon.UsesCharge ? ChargedPowers : FullPower;

            foreach (int side in Sides)
            {
                for (float angle = -MaxAngleDegrees; angle <= MaxAngleDegrees; angle += AngleStepDegrees)
                {
                    // Dirección a partir del ángulo; en pantalla, Y negativa es hacia arriba.
                    // Direction from the angle; on screen, negative Y is upwards.
                    float radians = MathHelper.ToRadians(angle);
                    var direction = new Vector2(MathF.Cos(radians) * side, -MathF.Sin(radians));

                    foreach (float power in powers)
                    {
                        ShotCandidate candidate = Simulate(shooter, weaponIndex, weapon, direction, power,
                            terrain, penguins, windAcceleration, muzzleDistance, focusTarget);

                        if (candidate != null && (Best == null || candidate.Score > Best.Score))
                            Best = candidate;

                        yield return true;
                    }
                }
            }
        }
    }

    // Simula un disparo con la misma clase Projectile del juego, sin dibujarlo.
    // Simulates a shot with the game's own Projectile class, without drawing it.
    private static ShotCandidate Simulate(Penguin shooter, int weaponIndex, Weapon weapon, Vector2 direction,
        float power, Terrain terrain, IReadOnlyList<Penguin> penguins, float windAcceleration,
        float muzzleDistance, Penguin focusTarget)
    {
        Vector2 spawnPosition = shooter.GunOrigin + direction * muzzleDistance;
        Vector2 velocity = direction * weapon.MaxLaunchSpeed * power;
        var projectile = new Projectile(weapon, spawnPosition, velocity, shooter);

        for (int step = 0; step < MaxSimulationSteps && projectile.IsActive; step++)
            projectile.Update(SimulationStep, terrain, penguins, windAcceleration);

        // Si no explotó (se salió del mapa o tardó demasiado), no sirve.
        // If it did not explode (left the map or took too long), it is useless.
        if (projectile.IsActive || !projectile.HasExploded)
            return null;

        return ScoreImpact(shooter, weaponIndex, weapon, direction, power, projectile.Position, penguins, focusTarget);
    }

    // Puntúa un impacto: daño a enemigos y muertes, menos fuego amigo y distancia.
    // Scores an impact: damage to enemies and kills, minus friendly fire and distance.
    private static ShotCandidate ScoreImpact(Penguin shooter, int weaponIndex, Weapon weapon, Vector2 direction,
        float power, Vector2 impact, IReadOnlyList<Penguin> penguins, Penguin focusTarget)
    {
        float enemyDamage = 0f;
        float friendlyDamage = 0f;
        float focusDamage = 0f;
        float closestEnemy = float.MaxValue;
        bool killsEnemy = false;
        bool killsFriend = false;
        bool killsFocus = false;

        foreach (Penguin penguin in penguins)
        {
            if (!penguin.IsAlive)
                continue;

            float damage = EstimateDamage(penguin, impact, weapon);

            // El propio tirador cuenta como aliado.
            // The shooter itself counts as an ally.
            if (penguin.Team == shooter.Team)
            {
                friendlyDamage += damage;
                if (damage >= penguin.Health)
                    killsFriend = true;
            }
            else
            {
                enemyDamage += damage;
                if (damage >= penguin.Health)
                    killsEnemy = true;

                if (penguin == focusTarget)
                {
                    focusDamage = damage;
                    killsFocus = damage >= penguin.Health;
                }

                // Con objetivo prioritario, la distancia se mide a él; si no, al enemigo más cercano.
                // With a priority target, distance is measured to it; otherwise, to the closest enemy.
                if (focusTarget == null || penguin == focusTarget)
                    closestEnemy = Math.Min(closestEnemy, Vector2.Distance(penguin.Center, impact));
            }
        }

        float score = enemyDamage
            + (killsEnemy ? KillBonus : 0f)
            + FocusDamageWeight * focusDamage
            + (killsFocus ? FocusKillBonus : 0f)
            - FriendlyFirePenalty * friendlyDamage
            - (killsFriend ? KillBonus * 2f : 0f)
            - DistancePenalty * closestEnemy;

        return new ShotCandidate
        {
            WeaponIndex = weaponIndex,
            Weapon = weapon,
            Direction = direction,
            Power = power,
            Score = score,
            ExpectedEnemyDamage = enemyDamage,
            KillsEnemy = killsEnemy
        };
    }

    // Misma fórmula de daño que Penguin.ApplyExplosion (sin contar el daño por caída).
    // Same damage formula as Penguin.ApplyExplosion (not counting fall damage).
    private static float EstimateDamage(Penguin penguin, Vector2 impact, Weapon weapon)
    {
        float reach = weapon.ExplosionRadius * 1.5f;
        float distance = Vector2.Distance(penguin.Center, impact);

        if (distance >= reach)
            return 0f;

        return (int)(weapon.MaxDamage * (1f - distance / reach));
    }
}