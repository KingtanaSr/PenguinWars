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

    // Vida que recupera un botiquín.
    // Health restored by a medkit.
    private const int MedkitHealAmount = 40;

    // Objetos en el mapa al empezar y probabilidad de suministros por turno.
    // Items on the map at the start and chance of supply drops per turn.
    private const int StartingMedkits = 2;
    private const int StartingCrates = 2;
    private const double SupplyDropChance = 0.35;

    // Opciones del menú de pausa.
    // Pause menu options.
    private readonly string[] _pauseOptions = { "Continuar", "Rendirse", "Salir al menú" };

    // Equipo que controla el jugador; la IA controlará el otro en la Fase 8.
    // Team controlled by the player; the AI will control the other one in Phase 8.
    private readonly Team _playerTeam;

    private readonly Random _random = new();
    private readonly List<Penguin> _penguins = new();
    private readonly List<Item> _items = new();
    private readonly List<Explosion> _explosions = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly Wind _wind = new();
    private Dictionary<Team, TeamInventory> _inventories;
    private Terrain _terrain;
    private TurnManager _turns;

    // Proyectil en juego (solo puede haber uno a la vez).
    // Projectile in play (there can only be one at a time).
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

    // Crea el mapa, coloca equipos y objetos, y empieza el primer turno.
    // Creates the map, places teams and items, and starts the first turn.
    private void StartNewMatch()
    {
        _terrain?.Dispose();
        _terrain = new Terrain(Game.GraphicsDevice, Game1.ScreenWidth, Game1.ScreenHeight, Environment.TickCount);

        _penguins.Clear();
        _items.Clear();
        _explosions.Clear();
        _floatingTexts.Clear();
        _projectile = null;
        _isCharging = false;
        _power = 0f;
        _wind.Randomize(_random);

        // Un inventario por equipo.
        // One inventory per team.
        _inventories = new Dictionary<Team, TeamInventory>
        {
            [Team.Heroes] = new TeamInventory(Team.Heroes),
            [Team.Aliens] = new TeamInventory(Team.Aliens)
        };

        // Terrestres a la izquierda, alienígenas a la derecha (el centro es el valle de la nave).
        // Earth penguins on the left, aliens on the right (the center is the ship valley).
        SpawnTeam(Team.Heroes, 0.08f);
        SpawnTeam(Team.Aliens, 0.72f);

        // Objetos repartidos por el mapa.
        // Items spread across the map.
        for (int i = 0; i < StartingMedkits; i++)
            SpawnItemOnGround(ItemType.Medkit);
        for (int i = 0; i < StartingCrates; i++)
            SpawnItemOnGround(ItemType.AmmoCrate);

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
            _penguins.Add(new Penguin(kinds[i], team, x, GetSurfaceUnder(x, Penguin.Width)));
        }
    }

    // Coloca un objeto sobre el terreno, lejos de los pingüinos.
    // Places an item on the terrain, away from the penguins.
    private void SpawnItemOnGround(ItemType type)
    {
        int x = 0;

        // Intentar varias posiciones hasta encontrar una lejos de todos los pingüinos.
        // Try several positions until finding one away from all penguins.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            x = _random.Next(40, Game1.ScreenWidth - 40);
            if (_penguins.All(p => Math.Abs(p.X - x) > 40))
                break;
        }

        _items.Add(new Item(type, x, GetSurfaceUnder(x, Item.Size)));
    }

    // Lanza un objeto desde el cielo en una posición aleatoria.
    // Drops an item from the sky at a random position.
    private void TrySupplyDrop()
    {
        if (_random.NextDouble() >= SupplyDropChance)
            return;

        ItemType type = _random.Next(2) == 0 ? ItemType.Medkit : ItemType.AmmoCrate;
        int x = _random.Next(40, Game1.ScreenWidth - 40);
        _items.Add(new Item(type, x, Item.Size));
        AddFloatingText("¡Suministros!", new Vector2(x, 110), Color.Gold);
    }

    // Punto más alto del terreno bajo un objeto de cierto ancho.
    // Highest terrain point under an object of a given width.
    private int GetSurfaceUnder(int x, int width)
    {
        int left = x - width / 2;
        return Enumerable.Range(left, width).Min(column => _terrain.GetSurfaceY(column));
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
        TeamInventory inventory = _inventories[current.Team];
        bool isPlaying = _turns.Phase == TurnPhase.Playing && _projectile == null;

        // Elegir arma, usar botiquín o pasar turno (solo sin estar cargando un disparo).
        // Choose weapon, use medkit or pass the turn (only while not charging a shot).
        if (CanAct(current) && !_isCharging)
        {
            HandleWeaponSelection(inventory);

            if (InputManager.WasKeyPressed(Keys.H))
                TryUseMedkit(current, inventory);
        }

        if (isPlaying && !_isCharging && InputManager.WasKeyPressed(Keys.P))
            _turns.EndAction();

        // Recalcular: usar un botiquín o pasar turno termina la acción.
        // Recalculate: using a medkit or passing the turn ends the action.
        bool canAct = CanAct(current);

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

        HandleAimAndFire(dt, current, inventory, canAct);

        // Actualizar a todos los pingüinos; solo el del turno recibe la entrada.
        // Update all penguins; only the one whose turn it is receives input.
        foreach (Penguin penguin in _penguins)
        {
            bool isCurrent = penguin == current;
            penguin.Update(dt, _terrain, isCurrent ? moveDirection : 0, isCurrent && jumpRequested);
        }

        // Los objetos caen si no tienen suelo, y se recogen al tocarlos.
        // Items fall if they have no ground, and are picked up on touch.
        foreach (Item item in _items)
            item.Update(dt, _terrain);
        CollectItems();

        UpdateProjectile(dt);

        // Actualizar efectos y quitar los que terminaron.
        // Update effects and remove the finished ones.
        foreach (Explosion explosion in _explosions)
            explosion.Update(dt);
        _explosions.RemoveAll(e => e.IsFinished);

        foreach (FloatingText text in _floatingTexts)
            text.Update(dt);
        _floatingTexts.RemoveAll(t => t.IsFinished);

        _terrain.ApplyChanges();

        // Avanzar el turno y, si terminó, pasar al siguiente o terminar la partida.
        // Advance the turn and, if it is over, move to the next one or end the match.
        _turns.Update(dt, IsWorldSettled());
        if (_turns.IsTurnOver)
            EndTurn();
    }

    // Solo se puede actuar en la fase de juego, en el suelo y sin proyectiles en juego.
    // Actions are only allowed in the playing phase, on the ground and with no projectile in play.
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

    // Teclas 1, 2 y 3 eligen arma; la rueda del ratón pasa a la siguiente o anterior.
    // Keys 1, 2 and 3 choose a weapon; the mouse wheel moves to the next or previous one.
    private static void HandleWeaponSelection(TeamInventory inventory)
    {
        if (InputManager.WasKeyPressed(Keys.D1)) inventory.Select(0);
        if (InputManager.WasKeyPressed(Keys.D2)) inventory.Select(1);
        if (InputManager.WasKeyPressed(Keys.D3)) inventory.Select(2);

        int scroll = InputManager.ScrollDelta;
        if (scroll > 0)
            inventory.SelectNext(-1);
        else if (scroll < 0)
            inventory.SelectNext(1);
    }

    // Usar un botiquín cura al pingüino del turno y termina el turno.
    // Using a medkit heals the current penguin and ends the turn.
    private void TryUseMedkit(Penguin current, TeamInventory inventory)
    {
        if (inventory.Medkits <= 0 || current.Health >= Penguin.MaxHealth)
            return;

        inventory.UseMedkit();
        current.Heal(MedkitHealAmount);
        AddFloatingText($"+{MedkitHealAmount} HP", current.Center - new Vector2(0f, 45f), Color.LimeGreen);
        _turns.EndAction();
    }

    // Cualquier pingüino que toque un objeto lo recoge para su equipo.
    // Any penguin touching an item picks it up for its team.
    private void CollectItems()
    {
        foreach (Item item in _items)
        {
            if (item.IsRemoved)
                continue;

            Penguin collector = _penguins.FirstOrDefault(p => p.IsAlive && p.Bounds.Intersects(item.Bounds));
            if (collector == null)
                continue;

            TeamInventory inventory = _inventories[collector.Team];
            Vector2 textPosition = item.Center - new Vector2(0f, 30f);

            if (item.Type == ItemType.Medkit)
            {
                inventory.AddMedkit();
                AddFloatingText("+1 Botiquín", textPosition, Color.White);
            }
            else
            {
                Weapon weapon = inventory.AddRandomAmmo(_random);
                AddFloatingText($"+{TeamInventory.CrateAmmoAmount} {weapon.Name}", textPosition, Color.Gold);
            }

            item.Remove();
        }

        _items.RemoveAll(i => i.IsRemoved);
    }

    private void AddFloatingText(string text, Vector2 position, Color color) =>
        _floatingTexts.Add(new FloatingText(text, position, color));

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
        TrySupplyDrop();
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

    // Armas con carga: mantener el clic carga y soltar dispara. Armas sin carga: el clic dispara al instante.
    // Charged weapons: holding the click charges and releasing fires. Uncharged weapons: the click fires instantly.
    private void HandleAimAndFire(float dt, Penguin current, TeamInventory inventory, bool canAct)
    {
        if (!canAct)
        {
            _isCharging = false;
            _power = 0f;
            return;
        }

        Weapon weapon = inventory.SelectedWeapon;

        if (!weapon.UsesCharge)
        {
            if (InputManager.WasLeftClicked)
                Fire(current, inventory);
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
                Fire(current, inventory);
        }
        else
        {
            // Se soltó el botón: disparar con la potencia acumulada.
            // The button was released: fire with the accumulated power.
            Fire(current, inventory);
        }
    }

    // Crea el proyectil del arma seleccionada, gasta munición y termina la acción del turno.
    // Creates the selected weapon's projectile, spends ammo and ends the turn's action.
    private void Fire(Penguin shooter, TeamInventory inventory)
    {
        Weapon weapon = inventory.SelectedWeapon;
        Vector2 direction = GetAimDirection(shooter);
        Vector2 spawnPosition = shooter.GunOrigin + direction * MuzzleDistance;

        // Las armas sin carga siempre disparan a potencia máxima.
        // Uncharged weapons always fire at full power.
        float power = weapon.UsesCharge ? Math.Max(_power, MinPower) : 1f;
        Vector2 velocity = direction * weapon.MaxLaunchSpeed * power;

        _projectile = new Projectile(weapon, spawnPosition, velocity, shooter);
        inventory.ConsumeAmmo(weapon);
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

    // Mueve el proyectil y, si explota, aplica la explosión.
    // Moves the projectile and, if it explodes, applies the explosion.
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

    // Destruye terreno y objetos, daña a los pingüinos cercanos y crea el destello.
    // Destroys terrain and items, damages nearby penguins and creates the flash.
    private void TriggerExplosion(Vector2 position, Weapon weapon)
    {
        _terrain.Destroy(position, weapon.ExplosionRadius);

        foreach (Penguin penguin in _penguins)
            penguin.ApplyExplosion(position, weapon.ExplosionRadius, weapon.MaxDamage, weapon.KnockbackScale);

        // Los objetos dentro del radio se destruyen.
        // Items inside the radius are destroyed.
        foreach (Item item in _items)
        {
            if (Vector2.Distance(item.Center, position) < weapon.ExplosionRadius)
                item.Remove();
        }

        _explosions.Add(new Explosion(position, weapon.ExplosionRadius));
    }

    public override void Draw(SpriteBatch sb)
    {
        DrawSky(sb);
        _terrain.Draw(sb);

        foreach (Item item in _items)
            item.Draw(sb, Game.Pixel);

        Penguin current = _turns.CurrentPenguin;

        // La flecha dorada marca al pingüino del turno.
        // The golden arrow marks the penguin whose turn it is.
        foreach (Penguin penguin in _penguins)
            penguin.Draw(sb, Game.Pixel, Game.Font, penguin == current);

        _projectile?.Draw(sb, Game.Circle, Game.Font);

        foreach (Explosion explosion in _explosions)
            explosion.Draw(sb, Game.Circle);

        foreach (FloatingText text in _floatingTexts)
            text.Draw(sb, Game.Font);

        if (CanAct(current))
            DrawAim(sb, current);

        DrawHud(sb, current);
        DrawTimer(sb);
        DrawWeaponBar(sb, current);
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
        Weapon weapon = _inventories[current.Team].SelectedWeapon;
        string power = _isCharging ? $"  |  Potencia: {(int)(_power * 100)}%" : "";

        DrawCenteredText(sb, $"Turno {_turns.TurnNumber}: {current.Name} ({current.Health} HP)  |  Arma: {weapon.Name}{power}", 20, Color.White, 0.75f);
        DrawCenteredText(sb, "A/D: caminar  |  Espacio: saltar  |  Ratón: apuntar  |  Clic: disparar (mantener para cargar)", 56, Color.LightGray, 0.5f);
        DrawCenteredText(sb, "1-3 o rueda: arma  |  H: botiquín  |  P: pasar turno  |  Esc: pausa", 78, Color.LightGray, 0.5f);
    }

    // Barra de armas del equipo del turno y botiquines disponibles.
    // Weapon bar of the current team and available medkits.
    private void DrawWeaponBar(SpriteBatch sb, Penguin current)
    {
        TeamInventory inventory = _inventories[current.Team];
        const int slotWidth = 170;
        const int slotHeight = 44;
        const int gap = 10;

        int totalWidth = inventory.Weapons.Count * slotWidth + (inventory.Weapons.Count - 1) * gap;
        int startX = (Game1.ScreenWidth - totalWidth) / 2;
        int y = Game1.ScreenHeight - slotHeight - 12;

        for (int i = 0; i < inventory.Weapons.Count; i++)
        {
            Weapon weapon = inventory.Weapons[i];
            int x = startX + i * (slotWidth + gap);
            bool isSelected = i == inventory.SelectedIndex;
            bool hasAmmo = inventory.HasAmmo(weapon);

            // Borde dorado para el arma seleccionada.
            // Golden border for the selected weapon.
            DrawRect(sb, new Rectangle(x - 2, y - 2, slotWidth + 4, slotHeight + 4), isSelected ? Color.Gold : Color.Gray * 0.6f);
            DrawRect(sb, new Rectangle(x, y, slotWidth, slotHeight), Color.Black * 0.75f);

            // Nombre y munición; en gris si no queda munición.
            // Name and ammo; gray if there is no ammo left.
            Color textColor = hasAmmo ? Color.White : Color.DimGray;
            string ammo = weapon.HasUnlimitedAmmo ? "Munición: ilimitada" : $"Munición: {inventory.GetAmmo(weapon)}";

            sb.DrawString(Game.Font, $"{i + 1}. {weapon.Name}", new Vector2(x + 8, y + 4), textColor,
                0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            sb.DrawString(Game.Font, ammo, new Vector2(x + 8, y + 24), Color.LightGray * (hasAmmo ? 1f : 0.5f),
                0f, Vector2.Zero, 0.42f, SpriteEffects.None, 0f);
        }

        // Botiquines del equipo, a la derecha.
        // Team medkits, on the right.
        int medkitX = Game1.ScreenWidth - 190;
        DrawRect(sb, new Rectangle(medkitX, y, 160, slotHeight), Color.Black * 0.75f);
        sb.DrawString(Game.Font, $"Botiquines: {inventory.Medkits}", new Vector2(medkitX + 8, y + 4), Color.White,
            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        sb.DrawString(Game.Font, $"H: usar (+{MedkitHealAmount} HP)", new Vector2(medkitX + 8, y + 24), Color.LightGray,
            0f, Vector2.Zero, 0.42f, SpriteEffects.None, 0f);
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