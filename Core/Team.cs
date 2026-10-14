// EQUIPOS DEL JUEGO
// GAME TEAMS

namespace PenguinWars.Core;

public enum Team
{
    // Pingüinos terrestres: policía, samurái, cyber
    // Earth penguins: police, samurai, cyber
    Heroes,

    // Pingüinos extraterrestres
    // Alien penguins
    Aliens
}

// Funciones de ayuda para los equipos.
// Helper functions for teams.
public static class TeamExtensions
{
    // Devuelve el equipo contrario. Se usa así: team.Opponent()
    // Returns the opposing team. Used like this: team.Opponent()
    public static Team Opponent(this Team team) =>
        team == Team.Heroes ? Team.Aliens : Team.Heroes;
}