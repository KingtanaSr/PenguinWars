// TIPOS DE PINGÜINOS Y SUS PERFILES
// PENGUIN TYPES AND THEIR PROFILES

using Microsoft.Xna.Framework;
using PenguinWars.Core;

namespace PenguinWars.Entities;

// Cada tipo de pingüino del juego.
// Each penguin type in the game.
public enum PenguinKind
{
    // Equipo terrestre
    // Earth team
    Police,
    Samurai,
    Cyber,

    // Equipo alienígena
    // Alien team
    AlienCommander,
    AlienScout,
    AlienBrute
}

// Datos de cada tipo: nombre, color de su accesorio y equipo al que pertenece.
// Data for each type: name, accessory color and the team it belongs to.
public static class PenguinProfiles
{
    // Nombre que se muestra sobre el pingüino.
    // Name displayed above the penguin.
    public static string GetName(PenguinKind kind) => kind switch
    {
        PenguinKind.Police => "Policía",
        PenguinKind.Samurai => "Samurái",
        PenguinKind.Cyber => "Cyber",
        PenguinKind.AlienCommander => "Comandante",
        PenguinKind.AlienScout => "Explorador",
        PenguinKind.AlienBrute => "Coloso",
        _ => "Pingüino"
    };

    // Color del accesorio (gorra, casco, armadura), basado en el arte conceptual.
    // Accessory color (cap, helmet, armor), based on the concept art.
    public static Color GetAccentColor(PenguinKind kind) => kind switch
    {
        PenguinKind.Police => new Color(40, 70, 160),
        PenguinKind.Samurai => new Color(200, 160, 40),
        PenguinKind.Cyber => new Color(0, 200, 230),
        PenguinKind.AlienCommander => new Color(150, 80, 200),
        PenguinKind.AlienScout => new Color(40, 170, 160),
        PenguinKind.AlienBrute => new Color(60, 170, 60),
        _ => Color.Gray
    };

    // Miembros de cada equipo.
    // Members of each team.
    public static PenguinKind[] GetTeamKinds(Team team) => team == Team.Heroes
        ? new[] { PenguinKind.Police, PenguinKind.Samurai, PenguinKind.Cyber }
        : new[] { PenguinKind.AlienCommander, PenguinKind.AlienScout, PenguinKind.AlienBrute };

    // Color que identifica a cada equipo (borde de la barra de vida).
    // Color that identifies each team (health bar border).
    public static Color GetTeamColor(Team team) =>
        team == Team.Heroes ? Color.CornflowerBlue : Color.LimeGreen;
}