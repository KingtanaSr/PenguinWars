// PANTALLA DE PARTIDA
// GAMEPLAY SCREEN

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenguinWars.Combat;
using PenguinWars.Core;
using PenguinWars.Entities;
using PenguinWars.Gameplay;
using PenguinWars.World;

namespace PenguinWars.Screens;

public class GameplayScreen : Screen
{
    // Segundos para cargar la potencia al máximo.
    // Seconds to charge power to the maximum.
    private const float ChargeTime = 1.2f;

    // Potencia mínima, para que un clic rápido igual dispare.
    // Minimum power, so a quick click still fires.
    private const float MinPower = 0.1f;

    // Distancia desde el pecho del pingüino a la que aparece el proyectil.
    // Distance from the penguin's chest where the projectile appears.
    private const float MuzzleDistance = 22f;

    // Puntos de la mira y separación entre ellos.
    // Aim dots and spacing between them.
    private const int AimDotCount = 8;
    private const float AimDotSpacing = 6f;

    // Opciones del menú de pausa.
    // Pause menu options.
    private readonly string[] _pauseOptions = { "Continuar", "Rendirse", "Salir al menú" };

    // Equipo que controla el jugador; la IA controlará el otro en la Fase 8.
    // Team controlled by the player; the AI will control the other one in Phase 8.
    private readonly Team _playerTeam;

    private readonly Random _random = new();
    private readonly List<Penguin> _penguins = new();
    private readonly List<Explosion> _explosions = new();
    private readonly Wind _wind = new();
    private Terrain _terrain;
    private TurnManager _turns;

    // Proyectil en vuelo (solo puede haber uno a la vez).
    // Projectile in flight (there can only be one at a time).
    private Projectile _projectile;

    // Estado de la carga del disparo.
    // Shot charging state.
    private bool _isCharging;
    private float _power;

    // Estado del menú de pausa.
    // Pause menu state.
    private bool _isPaused;
    private int _pauseSelected;

    public GameplayScreen(Game1 game, Team playerTeam) : base(game)
    {
        _playerTeam = playerTeam;
    }

    public override void LoadContent()
    {
        StartNewMatch();
    }

    // Crea el mapa, coloca a los equipos y empieza el primer turno.
    // Creates the map, places the teams and starts the first turn.
    private void StartNewMatch()
    {
        _terrain?.Dispose();
        _terrain = new Terrain(Game.GraphicsDevice, Game1.ScreenWidth, Game1.ScreenHeight, Environment.TickCount);

        _penguins.Clear();
        _explosions.Clear();
        _projectile = null;
        _isCharging = false;
        _power = 0f;
        _wind.Randomize(_random);

        // Terrestres a la izquierda, alienígenas a la derecha (el centro es el valle de la nave).
        // Earth penguins on the left, aliens on the right (the center is the ship valley).
        SpawnTeam(Team.Heroes, 0.08f);
        SpawnTeam(Team.Aliens, 0.72f);

        // El equipo que empieza se elige al azar.
        // The starting team is chosen at random.
        Team firstTeam = _random.Next(2) == 0 ? Team.Heroes : Team.Aliens;
        _turns = new TurnManager(_penguins, firstTeam);
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

        // Esc abre o cierra el menú de pausa.
        // Esc opens or closes the pause menu.
        if (InputManager.WasKeyPressed(Keys.Escape))
        {
            _isPaused = !_isPaused;
            _pauseSelected = 0;
            _isCharging = false;
            _power = 0f;
            return;
        }

        // Mientras está en pausa, el juego se congela.
        // While paused, the game freezes.
        if (_isPaused)
        {
            UpdatePauseMenu();
            return;
        }

        Penguin current = _turns.CurrentPenguin;
        bool isPlaying = _turns.Phase == TurnPhase.Playing && _projectile == null;
        bool canAct = CanAct(current);

        // P pasa el turno sin disparar.
        // P passes the turn without firing.
        if (isPlaying && !_isCharging && InputManager.WasKeyPressed(Keys.P))
            _turns.EndAction();

        // Movimiento con A y D, salto con Espacio; solo en la fase de juego y sin cargar.
        // Movement with A and D, jump with Space; only in the playing phase and while not charging.
        int moveDirection = 0;
        bool jumpRequested = false;
        if (_turns.Phase == TurnPhase.Playing && _projectile == null && !_isCharging)
        {
            if (InputManager.IsKeyDown(Keys.A)) moveDirection -= 1;
            if (InputManager.IsKeyDown(Keys.D)) moveDirection += 1;
            jumpRequested = InputManager.WasKeyPressed(Keys.Space);
        }

        // Si no camina, el pingüino mira hacia el ratón para apuntar.
        // If it is not walking, the penguin faces the mouse to aim.
        if (moveDirection == 0 && canAct)
            current.FaceTowards(InputManager.MousePosition.X);

        HandleAimAndFire(dt, current, canAct);

        // Actualizar a todos los pingüinos; solo el del turno recibe la entrada.
        // Update all penguins; only the one whose turn it is receives input.
        foreach (Penguin penguin in _penguins)
        {
            bool isCurrent = penguin == current;
            penguin.Update(dt, _terrain, isCurrent ? moveDirection : 0, isCurrent && jumpRequested);
        }

        UpdateProjectile(dt);

        // Actualizar los destellos y quitar los que terminaron.
        // Update the flashes and remove the finished ones.
        foreach (Explosion explosion in _explosions)
            explosion.Update(dt);
        _explosions.RemoveAll(e => e.IsFinished);

        _terrain.ApplyChanges();

        // Avanzar el turno y, si terminó, pasar al siguiente o terminar la partida.
        // Advance the turn and, if it is over, move to the next one or end the match.
        _turns.Update(dt, IsWorldSettled());
        if (_turns.IsTurnOver)
            EndTurn();
    }

    // Solo se puede actuar en la fase de juego, en el suelo y sin proyectiles en el aire.
    // Actions are only allowed in the playing phase, on the ground and with no projectile in the air.
    private bool CanAct(Penguin penguin) =>
        _turns.Phase == TurnPhase.Playing &&
        _projectile == null &&
        penguin.IsAlive &&
        penguin.IsGrounded;

    // El mundo está quieto si no hay proyectiles, explosiones ni pingüinos en el aire.
    // The world is settled if there are no projectiles, explosions or airborne penguins.
    private bool IsWorldSettled() =>
        _projectile == null &&
        _explosions.Count == 0 &&
        _penguins.All(p => !p.IsAlive || p.IsGrounded);

    // Al terminar un turno: revisar si alguien ganó; si no, cambiar el viento y seguir.
    // When a turn ends: check whether someone won; otherwise change the wind and continue.
    private void EndTurn()
    {
        bool heroesAlive = _turns.IsTeamAlive(Team.Heroes);
        bool aliensAlive = _turns.IsTeamAlive(Team.Aliens);

        if (!heroesAlive || !aliensAlive)
        {
            if (heroesAlive)
                GoToGameOver(Team.Heroes, "El equipo rival fue eliminado");
            else if (aliensAlive)
                GoToGameOver(Team.Aliens, "El equipo rival fue eliminado");
            else
                GoToGameOver(null, "Ambos equipos fueron eliminados");
            return;
        }

        _wind.Randomize(_random);
        _turns.BeginNextTurn();
    }

    private void GoToGameOver(Team? winner, string reason)
    {
        _terrain.Dispose();
        Game.Screens.ChangeScreen(new GameOverScreen(Game, _playerTeam, winner, reason));
    }

    // Navegación del menú de pausa.
    // Pause menu navigation.
    private void UpdatePauseMenu()
    {
        if (InputManager.WasKeyPressed(Keys.Down))
            _pauseSelected = (_pauseSelected + 1) % _pauseOptions.Length;
        if (InputManager.WasKeyPressed(Keys.Up))
            _pauseSelected = (_pauseSelected - 1 + _pauseOptions.Length) % _pauseOptions.Length;

        if (!InputManager.WasKeyPressed(Keys.Enter))
            return;

        switch (_pauseSelected)
        {
            case 0:
                // Continuar la partida.
                // Resume the match.
                _isPaused = false;
                break;

            case 1:
                // Rendirse: gana el equipo contrario al del jugador.
                // Surrender: the team opposing the player wins.
                GoToGameOver(_playerTeam.Opponent(), $"Los {PenguinProfiles.GetTeamName(_playerTeam)} se rindieron");
                break;

            case 2:
                // Salir al menú principal.
                // Exit to the main menu.
                _terrain.Dispose();
                Game.Screens.ChangeScreen(new MenuScreen(Game));
                break;
        }
    }

    // Mantener el clic carga la potencia; soltarlo (o llegar al máximo) dispara.
    // Holding the click charges power; releasing it (or reaching the maximum) fires.
    private void HandleAimAndFire(float dt, Penguin current, bool canAct)
    {
        if (!canAct)
        {
            _isCharging = false;
            _power = 0f;
            return;
        }

        if (InputManager.WasLeftClicked)
        {
            _isCharging = true;
            _power = 0f;
        }

        if (!_isCharging)
            return;

        if (InputManager.IsLeftButtonDown)
        {
            _power = Math.Min(1f, _power + dt / ChargeTime);

            // Al llegar al máximo dispara solo.
            // When it reaches the maximum, it fires automatically.
            if (_power >= 1f)
                Fire(current);
        }
        else
        {
            // Se soltó el botón: disparar con la potencia acumulada.
            // The button was released: fire with the accumulated power.
            Fire(current);
        }
    }

    // Crea el proyectil hacia el ratón y termina la acción del turno.
    // Creates the projectile towards the mouse and ends the turn's action.
    private void Fire(Penguin shooter)
    {
        Weapon weapon = Weapon.ForTeam(shooter.Team);
        Vector2 direction = GetAimDirection(shooter);
        Vector2 spawnPosition = shooter.GunOrigin + direction * MuzzleDistance;
        Vector2 velocity = direction * weapon.MaxLaunchSpeed * Math.Max(_power, MinPower);

        _projectile = new Projectile(weapon, spawnPosition, velocity, shooter);
        _isCharging = false;
        _power = 0f;

        // Disparar termina la acción: ahora se espera a que todo se calme.
        // Firing ends the action: now we wait for everything to settle.
        _turns.EndAction();
    }

    // Dirección normalizada desde el pecho del pingüino hacia el ratón.
    // Normalized direction from the penguin's chest towards the mouse.
    private static Vector2 GetAimDirection(Penguin penguin)
    {
        Vector2 direction = InputManager.MousePosition.ToVector2() - penguin.GunOrigin;

        // Si el ratón está justo sobre el pecho, apuntar hacia donde mira.
        // If the mouse is right on the chest, aim towards the facing direction.
        if (direction.LengthSquared() < 1f)
            return penguin.FacingRight ? Vector2.UnitX : -Vector2.UnitX;

        direction.Normalize();
        return direction;
    }

    // Mueve el proyectil y, si choca, aplica la explosión.
    // Moves the projectile and, if it hits something, applies the explosion.
    private void UpdateProjectile(float dt)
    {
        if (_projectile == null)
            return;

        _projectile.Update(dt, _terrain, _penguins, _wind.Acceleration);

        if (_projectile.IsActive)
            return;

        if (_projectile.HasExploded)
            TriggerExplosion(_projectile.Position, _projectile.Weapon);

        _projectile = null;
    }

    // Destruye terreno, daña a los pingüinos cercanos y crea el destello.
    // Destroys terrain, damages nearby penguins and creates the flash.
    private void TriggerExplosion(Vector2 position, Weapon weapon)
    {
        _terrain.Destroy(position, weapon.ExplosionRadius);

        foreach (Penguin penguin in _penguins)
            penguin.ApplyExplosion(position, weapon.ExplosionRadius, weapon.MaxDamage);

        _explosions.Add(new Explosion(position, weapon.ExplosionRadius));
    }

    public override void Draw(SpriteBatch sb)
    {
        DrawSky(sb);
        _terrain.Draw(sb);

        Penguin current = _turns.CurrentPenguin;

        // La flecha dorada marca al pingüino del turno.
        // The golden arrow marks the penguin whose turn it is.
        foreach (Penguin penguin in _penguins)
            penguin.Draw(sb, Game.Pixel, Game.Font, penguin == current);

        _projectile?.Draw(sb, Game.Circle);

        foreach (Explosion explosion in _explosions)
            explosion.Draw(sb, Game.Circle);

        if (CanAct(current))
            DrawAim(sb, current);

        DrawHud(sb, current);
        DrawTimer(sb);
        _wind.Draw(sb, Game.Pixel, Game.Font, Game1.ScreenWidth);

        if (_turns.Phase == TurnPhase.Starting)
            DrawTurnBanner(sb, current);

        if (_isPaused)
            DrawPauseMenu(sb);
    }

    // Mira: puntos en la dirección del disparo que se llenan con la potencia, y una cruz en el ratón.
    // Aim: dots in the shot direction that fill up with power, and a cross at the mouse.
    private void DrawAim(SpriteBatch sb, Penguin penguin)
    {
        Vector2 direction = GetAimDirection(penguin);

        for (int i = 0; i < AimDotCount; i++)
        {
            Vector2 position = penguin.GunOrigin + direction * (16f + i * AimDotSpacing);
            float threshold = i / (float)AimDotCount;

            // Los puntos cargados van de amarillo a rojo; el resto son blancos tenues.
            // Charged dots go from yellow to red; the rest are faint white.
            Color color = _isCharging && _power > threshold
                ? Color.Lerp(Color.Yellow, Color.Red, threshold)
                : Color.White * 0.5f;

            DrawRect(sb, new Rectangle((int)position.X - 1, (int)position.Y - 1, 3, 3), color);
        }

        // Cruz en la posición del ratón.
        // Cross at the mouse position.
        Point mouse = InputManager.MousePosition;
        DrawRect(sb, new Rectangle(mouse.X - 6, mouse.Y, 13, 1), Color.White);
        DrawRect(sb, new Rectangle(mouse.X, mouse.Y - 6, 1, 13), Color.White);
    }

    // Información del turno y controles en pantalla.
    // Turn information and on-screen controls.
    private void DrawHud(SpriteBatch sb, Penguin current)
    {
        Weapon weapon = Weapon.ForTeam(current.Team);
        string power = _isCharging ? $"  |  Potencia: {(int)(_power * 100)}%" : "";

        DrawCenteredText(sb, $"Turno {_turns.TurnNumber}: {current.Name} ({current.Health} HP)  |  Arma: {weapon.Name}{power}", 20, Color.White, 0.75f);
        DrawCenteredText(sb, "A/D: caminar  |  Espacio: saltar  |  Ratón: apuntar  |  Mantener clic: cargar, soltar: disparar", 56, Color.LightGray, 0.5f);
        DrawCenteredText(sb, "P: pasar turno  |  Esc: pausa", 78, Color.LightGray, 0.5f);
    }

    // Tiempo restante del turno, arriba a la izquierda; en rojo los últimos 10 segundos.
    // Remaining turn time, top left; red during the last 10 seconds.
    private void DrawTimer(SpriteBatch sb)
    {
        int seconds = (int)MathF.Ceiling(_turns.TimeLeft);
        Color color = seconds <= 10 ? Color.Red : Color.White;

        DrawRect(sb, new Rectangle(25, 15, 90, 50), Color.Black * 0.4f);
        sb.DrawString(Game.Font, "Tiempo", new Vector2(35, 18), Color.LightGray,
            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        sb.DrawString(Game.Font, seconds.ToString(), new Vector2(35, 32), color,
            0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
    }

    // Aviso al inicio de cada turno.
    // Notice at the start of each turn.
    private void DrawTurnBanner(SpriteBatch sb, Penguin current)
    {
        string owner = current.Team == _playerTeam ? "Tu equipo" : "Equipo rival";

        DrawRect(sb, new Rectangle(0, 230, Game1.ScreenWidth, 115), Color.Black * 0.55f);
        DrawCenteredText(sb, $"Turno de {current.Name}", 245, PenguinProfiles.GetTeamColor(current.Team), 1.3f);
        DrawCenteredText(sb, $"{PenguinProfiles.GetTeamName(current.Team)}  ({owner})", 300, Color.White, 0.75f);
    }

    // Menú de pausa sobre el juego oscurecido.
    // Pause menu over the darkened game.
    private void DrawPauseMenu(SpriteBatch sb)
    {
        DrawRect(sb, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), Color.Black * 0.6f);
        DrawCenteredText(sb, "PAUSA", 220, Color.White, 1.5f);

        for (int i = 0; i < _pauseOptions.Length; i++)
        {
            bool isSelected = i == _pauseSelected;
            string text = isSelected ? $"> {_pauseOptions[i]} <" : _pauseOptions[i];
            DrawCenteredText(sb, text, 320 + i * 50, isSelected ? Color.Gold : Color.White);
        }

        DrawCenteredText(sb, "Flechas para moverte, Enter para elegir, Esc para continuar", 520, Color.Gray, 0.6f);
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