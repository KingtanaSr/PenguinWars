// CONTEXTO PARA EL ESTRATEGA: RESUMEN DE LA PARTIDA Y OPCIONES VÁLIDAS
// STRATEGIST CONTEXT: MATCH SUMMARY AND VALID OPTIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using PenguinWars.Core;
using PenguinWars.Entities;
using PenguinWars.Gameplay;

namespace PenguinWars.AI;

// Tipos de opción que el estratega puede elegir.
// Option types the strategist can choose.
public enum StrategyOptionKind
{
    Attack,
    Heal
}

// Una opción válida, calculada por el juego.
// A valid option, computed by the game.
public sealed class StrategyOption
{
    public string Id { get; init; }
    public StrategyOptionKind Kind { get; init; }

    // Enemigo objetivo (solo para opciones de ataque).
    // Target enemy (attack options only).
    public Penguin Target { get; init; }

    public string Description { get; init; }
}

// Respuesta del estratega: la opción elegida y una frase en personaje.
// Strategist response: the chosen option and an in-character line.
public sealed record StrategyDecision(string OptionId, string Taunt);

// Copia de la situación en texto, creada en el hilo principal del juego.
// Text snapshot of the situation, created on the game's main thread.
// La consulta al LLM solo lee estos textos, nunca los objetos del juego.
// The LLM request only reads these texts, never the game objects.
public sealed class StrategyContext
{
    public Team Team { get; init; }
    public string Situation { get; init; }
    public IReadOnlyList<StrategyOption> Options { get; init; }

    // Busca una opción por su identificador, sin importar mayúsculas ni espacios.
    // Finds an option by its identifier, ignoring case and spaces.
    public StrategyOption FindOption(string id) =>
        Options.FirstOrDefault(o => string.Equals(o.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase));

    // Construye el resumen de la partida y la lista de opciones.
    // Builds the match summary and the list of options.
    public static StrategyContext Build(Penguin self, IReadOnlyList<Penguin> penguins,
        TeamInventory inventory, float windStrength, int medkitHealAmount)
    {
        var options = new List<StrategyOption>();
        var text = new StringBuilder();

        text.AppendLine($"Tu pingüino: {self.Name}, vida {self.Health}/{Penguin.MaxHealth}.");
        text.AppendLine($"Botiquines del equipo: {inventory.Medkits}.");
        text.AppendLine($"Viento: {DescribeWind(windStrength)}.");
        text.AppendLine("Armas: " + string.Join(", ", inventory.Weapons.Select(w =>
            w.HasUnlimitedAmmo ? $"{w.Name} (ilimitada)" : $"{w.Name} ({inventory.GetAmmo(w)})")));

        // Una opción de ataque por cada enemigo vivo.
        // One attack option for each living enemy.
        text.AppendLine("Enemigos:");
        int number = 1;
        foreach (Penguin enemy in penguins.Where(p => p.IsAlive && p.Team != self.Team))
        {
            int distance = (int)Vector2.Distance(self.Center, enemy.Center);
            string side = enemy.X < self.X ? "a la izquierda" : "a la derecha";
            string height = enemy.Y < self.Y - 20 ? "más arriba"
                          : enemy.Y > self.Y + 20 ? "más abajo"
                          : "a la misma altura";

            text.AppendLine($"- {enemy.Name}: vida {enemy.Health}, a {distance} píxeles {side}, {height}.");
            options.Add(new StrategyOption
            {
                Id = $"A{number++}",
                Kind = StrategyOptionKind.Attack,
                Target = enemy,
                Description = $"Atacar a {enemy.Name}"
            });
        }

        // Curarse solo es una opción si hay botiquines y no tiene la vida llena.
        // Healing is only an option if there are medkits and health is not full.
        if (inventory.Medkits > 0 && self.Health < Penguin.MaxHealth)
        {
            options.Add(new StrategyOption
            {
                Id = "H",
                Kind = StrategyOptionKind.Heal,
                Description = $"Usar un botiquín (+{medkitHealAmount} de vida, termina el turno)"
            });
        }

        text.AppendLine("Opciones disponibles:");
        foreach (StrategyOption option in options)
            text.AppendLine($"- {option.Id}: {option.Description}");

        return new StrategyContext
        {
            Team = self.Team,
            Situation = text.ToString(),
            Options = options
        };
    }

    // Describe el viento en palabras.
    // Describes the wind in words.
    private static string DescribeWind(float strength)
    {
        float magnitude = Math.Abs(strength);
        if (magnitude < 0.2f)
            return "casi sin viento";

        string direction = strength > 0 ? "hacia la derecha" : "hacia la izquierda";
        string force = magnitude < 0.5f ? "débil" : magnitude < 0.8f ? "moderado" : "fuerte";
        return $"{force} {direction}";
    }
}