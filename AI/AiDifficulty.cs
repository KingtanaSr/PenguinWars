// NIVELES DE DIFICULTAD DE LA IA
// AI DIFFICULTY LEVELS

namespace PenguinWars.AI;

public enum AiDifficulty
{
    Easy,
    Normal,
    Hard
}

// Valores de cada dificultad: cuánto se equivoca la IA al apuntar.
// Values for each difficulty: how much the AI misses when aiming.
public static class AiDifficultySettings
{
    // Nombre para mostrar en pantalla.
    // Name to display on screen.
    public static string GetName(AiDifficulty difficulty) => difficulty switch
    {
        AiDifficulty.Easy => "Fácil",
        AiDifficulty.Normal => "Normal",
        AiDifficulty.Hard => "Difícil",
        _ => "Normal"
    };

    // Error máximo de ángulo, en grados.
    // Maximum angle error, in degrees.
    public static float GetAngleError(AiDifficulty difficulty) => difficulty switch
    {
        AiDifficulty.Easy => 9f,
        AiDifficulty.Normal => 4f,
        AiDifficulty.Hard => 1f,
        _ => 4f
    };

    // Error máximo de potencia (0 a 1).
    // Maximum power error (0 to 1).
    public static float GetPowerError(AiDifficulty difficulty) => difficulty switch
    {
        AiDifficulty.Easy => 0.12f,
        AiDifficulty.Normal => 0.05f,
        AiDifficulty.Hard => 0.015f,
        _ => 0.05f
    };
}