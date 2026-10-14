// CONFIGURACIÓN GLOBAL DEL JUEGO
// GLOBAL GAME SETTINGS

namespace PenguinWars.Core;

public static class GameSettings
{
    // Si está activado, la IA consulta a un LLM local (Ollama) para decidir su estrategia.
    // If enabled, the AI asks a local LLM (Ollama) to decide its strategy.
    public static bool UseLlmStrategist { get; set; } = true;
}