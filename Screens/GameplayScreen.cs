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

    // Equipo que controla el jugador; la IA controla el otro.
    // Team controlled by the player; the AI controls the other one.
    private readonly Team _playerTeam;

    private readonly Random _random = new();
    private readonly List<Penguin> _penguins = new();
    private readonly List<Explosion> _explosions = new();
    private readonly Wind _wind = new();
    private Terrain _terrain;

    // Proyectil en vuelo (solo puede haber uno a la vez).
    // Projectile in flight (there can only be one at a time).
    private Projectile _projectile;

    // Estado de la carga del disparo.
    // Shot charging state.
    private bool _isCharging;
    private float _power;

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

    // Crea un mapa nuevo, coloca a los equipos y reinicia el estado del combate.
    // Creates a new map, places the teams and resets the combat state.
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

        // PRUEBA: Tab cambia de pingüino, solo si no hay un disparo en curso.
        // TEST: Tab switches penguin, only if no shot is in progress.
        if (InputManager.WasKeyPressed(Keys.Tab) && _projectile == null && !_isCharging)
            SelectNextPenguin();

        // Si el pingüino seleccionado murió, pasar al siguiente.
        // If the selected penguin died, switch to the next one.
        if (!_penguins[_selectedIndex].IsAlive)
            SelectNextPenguin();

        Penguin current = _penguins[_selectedIndex];

        // Solo se puede actuar en el suelo y sin proyectiles en el aire.
        // Actions are only allowed on the ground and with no projectile in the air.
        bool canAct = CanAct(current);

        // Movimiento con A y D, salto con Espacio; bloqueado mientras se carga o vuela un disparo.
        // Movement with A and D, jump with Space; blocked while charging or while a shot is flying.
        int moveDirection = 0;
        bool jumpRequested = false;
        if (_projectile == null && !_isCharging)
        {
            // Para invertir los controles, intercambia Keys.A y Keys.D en estas dos líneas.
            // To invert the controls, swap Keys.A and Keys.D in these two lines.
            if (InputManager.IsKeyDown(Keys.A)) moveDirection -= 1;
            if (InputManager.IsKeyDown(Keys.D)) moveDirection += 1;
            jumpRequested = InputManager.WasKeyPressed(Keys.Space);
        }

        // Si no camina, el pingüino mira hacia el ratón para apuntar.
        // If it is not walking, the penguin faces the mouse to aim.
        if (moveDirection == 0 && canAct)
            current.FaceTowards(InputManager.MousePosition.X);

        HandleAimAndFire(dt, current, canAct);

        // Actualizar a todos los pingüinos; solo el seleccionado recibe la entrada.
        // Update all penguins; only the selected one receives input.
        for (int i = 0; i < _penguins.Count; i++)
        {
            bool isSelected = i == _selectedIndex;
            _penguins[i].Update(dt, _terrain, isSelected ? moveDirection : 0, isSelected && jumpRequested);
        }

        UpdateProjectile(dt);

        // Actualizar los destellos y quitar los que terminaron.
        // Update the flashes and remove the finished ones.
        foreach (Explosion explosion in _explosions)
            explosion.Update(dt);
        _explosions.RemoveAll(e => e.IsFinished);

        _terrain.ApplyChanges();
    }

    private bool CanAct(Penguin penguin) =>
        _projectile == null && penguin.IsAlive && penguin.IsGrounded;

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

    // Crea el proyectil en dirección al ratón con la potencia cargada.
    // Creates the projectile towards the mouse with the charged power.
    private void Fire(Penguin shooter)
    {
        Weapon weapon = Weapon.ForTeam(shooter.Team);
        Vector2 direction = GetAimDirection(shooter);
        Vector2 spawnPosition = shooter.GunOrigin + direction * MuzzleDistance;
        Vector2 velocity = direction * weapon.MaxLaunchSpeed * Math.Max(_power, MinPower);

        _projectile = new Projectile(weapon, spawnPosition, velocity, shooter);
        _isCharging = false;
        _power = 0f;
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

    // Mueve el proyectil; si explota, aplica la explosión; al terminar, cambia el viento.
    // Moves the projectile; if it explodes, applies the explosion; when done, changes the wind.
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

        // Nuevo viento después de cada disparo (en la Fase 6 será por turno).
        // New wind after each shot (in Phase 6 it will be per turn).
        _wind.Randomize(_random);
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

        _projectile?.Draw(sb, Game.Circle);

        foreach (Explosion explosion in _explosions)
            explosion.Draw(sb, Game.Circle);

        Penguin current = _penguins[_selectedIndex];
        if (CanAct(current))
            DrawAim(sb, current);

        DrawHud(sb, current);
        _wind.Draw(sb, Game.Pixel, Game.Font, Game1.ScreenWidth);
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

    // Información y controles en pantalla.
    // On-screen information and controls.
    private void DrawHud(SpriteBatch sb, Penguin current)
    {
        Weapon weapon = Weapon.ForTeam(current.Team);
        string power = _isCharging ? $"  |  Potencia: {(int)(_power * 100)}%" : "";

        DrawCenteredText(sb, $"{current.Name} ({current.Health} HP)  |  Arma: {weapon.Name}{power}", 20, Color.White, 0.75f);
        DrawCenteredText(sb, "A/D: caminar  |  Espacio: saltar  |  Ratón: apuntar  |  Mantener clic: cargar, soltar: disparar", 56, Color.LightGray, 0.5f);
        DrawCenteredText(sb, "Tab: cambiar pingüino  |  R: nuevo mapa  |  Esc: menú", 78, Color.LightGray, 0.5f);
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