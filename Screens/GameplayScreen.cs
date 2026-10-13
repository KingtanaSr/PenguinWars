// PANTALLA DE PARTIDA
// GAMEPLAY SCREEN

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.Core;
using PenguinWars.Entities;
using PenguinWars.World;

namespace PenguinWars.Screens;

public class GameplayScreen : Screen
{
    // Valores de las explosiones de prueba.
    // Values for the test explosions.
    private const float TestExplosionRadius = 40f;
    private const int TestExplosionDamage = 40;

    // Equipo que controla el jugador; la IA controla el otro.
    // Team controlled by the player; the AI controls the other one.
    private readonly Team _playerTeam;

    private readonly Random _random = new();
    private readonly List<Penguin> _penguins = new();
    private Terrain _terrain;

    // Pingüino controlado actualmente (temporal hasta tener turnos).
    // Currently controlled penguin (temporary until turns exist).
    private int _selectedIndex;

    public GameplayScreen(Game1 game, Team playerTeam) : base(game)
    {
        _playerTeam = playerTeam;
    }

    public override void LoadContent()
    {
        StartNewMatch();
    }

    // Crea un mapa nuevo y coloca a los dos equipos.
    // Creates a new map and places both teams.
    private void StartNewMatch()
    {
        _terrain?.Dispose();
        _terrain = new Terrain(Game.GraphicsDevice, Game1.ScreenWidth, Game1.ScreenHeight, Environment.TickCount);

        _penguins.Clear();

        // Terrestres a la izquierda, alienígenas a la derecha (el centro es el valle de la nave).
        // Earth penguins on the left, aliens on the right (the center is the ship valley).
        SpawnTeam(Team.Heroes, 0.08f);
        SpawnTeam(Team.Aliens, 0.72f);

        // Empezar controlando al primer pingüino del equipo del jugador.
        // Start by controlling the first penguin of the player's team.
        _selectedIndex = _penguins.FindIndex(p => p.Team == _playerTeam);
    }

    // Coloca a los miembros de un equipo separados entre sí, con algo de variación.
    // Places the members of a team apart from each other, with some variation.
    private void SpawnTeam(Team team, float startFraction)
    {
        PenguinKind[] kinds = PenguinProfiles.GetTeamKinds(team);

        for (int i = 0; i < kinds.Length; i++)
        {
            float fraction = startFraction + i * 0.09f + (float)_random.NextDouble() * 0.03f;
            int x = (int)(fraction * Game1.ScreenWidth);
            _penguins.Add(new Penguin(kinds[i], team, x, GetSpawnY(x)));
        }
    }

    // Altura donde aparece un pingüino: el punto más alto del terreno bajo todo su cuerpo.
    // Height where a penguin spawns: the highest terrain point under its whole body.
    private int GetSpawnY(int x)
    {
        int left = x - Penguin.Width / 2;
        return Enumerable.Range(left, Penguin.Width).Min(column => _terrain.GetSurfaceY(column));
    }

    public override void Update(GameTime gameTime)
    {
        // Tiempo transcurrido desde el frame anterior, en segundos.
        // Time elapsed since the previous frame, in seconds.
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Volver al menú con Esc.
        // Return to the menu with Esc.
        if (InputManager.WasKeyPressed(Keys.Escape))
        {
            _terrain.Dispose();
            Game.Screens.ChangeScreen(new MenuScreen(Game));
            return;
        }

        // PRUEBA: R reinicia la partida con un mapa nuevo.
        // TEST: R restarts the match with a new map.
        if (InputManager.WasKeyPressed(Keys.R))
            StartNewMatch();

        // PRUEBA: Tab cambia al siguiente pingüino vivo.
        // TEST: Tab switches to the next living penguin.
        if (InputManager.WasKeyPressed(Keys.Tab))
            SelectNextPenguin();

        // PRUEBA: clic izquierdo crea una explosión que daña y empuja.
        // TEST: left click creates an explosion that damages and pushes.
        if (InputManager.WasLeftClicked)
        {
            Vector2 position = InputManager.MousePosition.ToVector2();
            _terrain.Destroy(position, TestExplosionRadius);

            foreach (Penguin penguin in _penguins)
                penguin.ApplyExplosion(position, TestExplosionRadius, TestExplosionDamage);
        }

        // Si el pingüino seleccionado murió, pasar al siguiente.
        // If the selected penguin died, switch to the next one.
        if (!_penguins[_selectedIndex].IsAlive)
            SelectNextPenguin();

        // Entrada del jugador para el pingüino seleccionado.
        // Player input for the selected penguin.
        int moveDirection = 0;
        if (InputManager.IsKeyDown(Keys.Left)) moveDirection -= 1;
        if (InputManager.IsKeyDown(Keys.Right)) moveDirection += 1;
        bool jumpRequested = InputManager.WasKeyPressed(Keys.Enter);

        // Actualizar a todos; solo el seleccionado recibe la entrada.
        // Update everyone; only the selected one receives input.
        for (int i = 0; i < _penguins.Count; i++)
        {
            bool isSelected = i == _selectedIndex;
            _penguins[i].Update(dt, _terrain, isSelected ? moveDirection : 0, isSelected && jumpRequested);
        }

        _terrain.ApplyChanges();
    }

    // Selecciona el siguiente pingüino vivo de la lista.
    // Selects the next living penguin in the list.
    private void SelectNextPenguin()
    {
        if (!_penguins.Any(p => p.IsAlive))
            return;

        do
        {
            _selectedIndex = (_selectedIndex + 1) % _penguins.Count;
        }
        while (!_penguins[_selectedIndex].IsAlive);
    }

    public override void Draw(SpriteBatch sb)
    {
        DrawSky(sb);
        _terrain.Draw(sb);

        for (int i = 0; i < _penguins.Count; i++)
            _penguins[i].Draw(sb, Game.Pixel, Game.Font, i == _selectedIndex);

        // Información en pantalla.
        // On-screen information.
        Penguin selected = _penguins[_selectedIndex];
        DrawCenteredText(sb, $"Controlando: {selected.Name} ({selected.Health} HP)", 20, Color.White, 0.8f);
        DrawCenteredText(sb, "Flechas: caminar  |  Enter: saltar  |  Tab: cambiar pingüino", 58, Color.LightGray, 0.55f);
        DrawCenteredText(sb, "Clic: explosión de prueba  |  R: nuevo mapa  |  Esc: menú", 82, Color.LightGray, 0.55f);
    }

    // Dibuja un cielo con degradado vertical, de noche arriba a más claro abajo.
    // Draws a sky with a vertical gradient, from night at the top to lighter at the bottom.
    private void DrawSky(SpriteBatch sb)
    {
        const int bands = 72;
        int bandHeight = Game1.ScreenHeight / bands;
        var top = new Color(10, 14, 30);
        var bottom = new Color(60, 85, 125);

        for (int i = 0; i < bands; i++)
        {
            Color color = Color.Lerp(top, bottom, i / (float)(bands - 1));
            DrawRect(sb, new Rectangle(0, i * bandHeight, Game1.ScreenWidth, bandHeight), color);
        }
    }
}