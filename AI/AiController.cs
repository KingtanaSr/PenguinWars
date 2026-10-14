// CONTROLADOR DE LA IA DEL EQUIPO RIVAL (HÍBRIDA: ESTRATEGA LLM + PLANIFICADOR CLÁSICO)
// AI CONTROLLER FOR THE OPPOSING TEAM (HYBRID: LLM STRATEGIST + CLASSIC PLANNER)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using PenguinWars.Entities;
using PenguinWars.Gameplay;
using PenguinWars.World;

namespace PenguinWars.AI;

// Acción que la IA le pide ejecutar a la partida.
// Action the AI asks the match to execute.
public enum AiAction
{
    None,
    Fire,
    UseMedkit,
    Pass
}

// Máquina de estados del turno de la IA: consultar, pensar, apuntar, cargar y disparar.
// State machine for the AI's turn: consult, think, aim, charge and fire.
public class AiController
{
    private enum State
    {
        Idle,
        AskingAdvisor,
        Thinking,
        Aiming,
        Charging,
        Done
    }

    // Tiempo máximo de espera al estratega antes de usar la IA clásica.
    // Maximum wait for the strategist before falling back to the classic AI.
    private const float AdvisorTimeout = 12f;

    // Tiempo mínimo pensando, para que el jugador alcance a ver qué pasa.
    // Minimum thinking time, so the player can see what is happening.
    private const float MinThinkTime = 1f;

    // Tiempo para girar la mira hasta el objetivo.
    // Time to rotate the aim towards the target.
    private const float AimTime = 0.8f;

    // Misma velocidad de carga que el jugador.
    // Same charging speed as the player.
    private const float ChargeTime = 1.2f;

    // Simulaciones por frame (más = piensa más rápido, pero cuesta más rendimiento).
    // Simulations per frame (more = thinks faster, but costs more performance).
    private const int SimulationsPerFrame = 40;

    // Con esta vida o menos, prefiere curarse si no puede matar a nadie.
    // At this health or below, it prefers to heal if it cannot kill anyone.
    private const int LowHealthThreshold = 40;

    private readonly AiDifficulty _difficulty;
    private readonly Random _random;
    private readonly ShotPlanner _planner = new();

    // Estratega opcional (null = solo IA clásica).
    // Optional strategist (null = classic AI only).
    private readonly IStrategyAdvisor _advisor;

    private State _state = State.Idle;
    private IEnumerator<bool> _search;
    private Penguin _shooter;
    private TeamInventory _inventory;
    private Terrain _terrain;
    private IReadOnlyList<Penguin> _penguins;
    private float _windAcceleration;
    private float _muzzleDistance;
    private ShotCandidate _plan;
    private float _timer;
    private float _startAngle;
    private float _targetAngle;
    private float _targetPower;

    // Consulta en curso al estratega y su contexto.
    // Ongoing strategist request and its context.
    private Task<StrategyDecision> _adviceTask;
    private CancellationTokenSource _adviceCancellation;
    private StrategyContext _context;

    // Objetivo elegido por el estratega y frase pendiente de mostrar.
    // Target chosen by the strategist and line waiting to be shown.
    private Penguin _focusTarget;
    private string _pendingTaunt;

    // Dirección en la que apunta la IA (la partida la usa para dibujar y disparar).
    // Direction the AI is aiming (the match uses it to draw and fire).
    public Vector2 AimDirection { get; private set; } = Vector2.UnitX;

    public float Power { get; private set; }
    public bool IsWaitingForAdvisor => _state == State.AskingAdvisor;
    public bool IsThinking => _state == State.Thinking;
    public bool IsCharging => _state == State.Charging;
    public bool HasPlan => _plan != null;
    public int SelectedWeaponIndex => _plan?.WeaponIndex ?? 0;

    // Quién decidió en este turno, para mostrar en el HUD.
    // Who decided this turn, to display in the HUD.
    public string AdvisorStatus { get; private set; }

    // Meta actual elegida por el estratega (por ejemplo, "Objetivo: Samurái").
    // Current goal chosen by the strategist (for example, "Objetivo: Samurái").
    public string CurrentGoal { get; private set; }

    public AiController(AiDifficulty difficulty, Random random, IStrategyAdvisor advisor = null)
    {
        _difficulty = difficulty;
        _random = random;
        _advisor = advisor;
        AdvisorStatus = advisor == null ? "IA clásica" : $"Estratega LLM: {advisor.Name}";
    }

    // Precarga el modelo del estratega, si hay uno.
    // Preloads the strategist's model, if there is one.
    public void WarmUp() => _advisor?.WarmUp();

    // Entrega la frase del estratega una sola vez.
    // Delivers the strategist's line only once.
    public bool TryTakeTaunt(out string taunt)
    {
        taunt = _pendingTaunt;
        _pendingTaunt = null;
        return !string.IsNullOrWhiteSpace(taunt);
    }

    // Empieza el turno: consulta al estratega (si hay) o pasa directo a planear.
    // Starts the turn: asks the strategist (if any) or goes straight to planning.
    public void BeginTurn(Penguin shooter, TeamInventory inventory, Terrain terrain, IReadOnlyList<Penguin> penguins,
        float windAcceleration, float windStrength, float muzzleDistance, int medkitHealAmount)
    {
        _shooter = shooter;
        _inventory = inventory;
        _terrain = terrain;
        _penguins = penguins;
        _windAcceleration = windAcceleration;
        _muzzleDistance = muzzleDistance;
        _plan = null;
        _focusTarget = null;
        _pendingTaunt = null;
        CurrentGoal = null;
        _timer = 0f;
        Power = 0f;
        AimDirection = shooter.FacingRight ? Vector2.UnitX : -Vector2.UnitX;

        if (_advisor == null)
        {
            StartSearch();
            return;
        }

        // El contexto se crea aquí, en el hilo principal; la consulta solo lee sus textos.
        // The context is created here, on the main thread; the request only reads its texts.
        _context = StrategyContext.Build(shooter, penguins, inventory, windStrength, medkitHealAmount);
        _adviceCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(AdvisorTimeout));
        _adviceTask = _advisor.AdviseAsync(_context, _adviceCancellation.Token);
        _state = State.AskingAdvisor;
    }

    // Avanza la máquina de estados y devuelve la acción a ejecutar (o None).
    // Advances the state machine and returns the action to execute (or None).
    public AiAction Update(float dt)
    {
        switch (_state)
        {
            case State.AskingAdvisor:
                return UpdateAskingAdvisor(dt);

            case State.Thinking:
                return UpdateThinking(dt);

            case State.Aiming:
                return UpdateAiming(dt);

            case State.Charging:
                // Cargar la potencia hasta la elegida.
                // Charge the power up to the chosen one.
                Power = Math.Min(_targetPower, Power + dt / ChargeTime);
                if (Power >= _targetPower)
                {
                    _state = State.Done;
                    return AiAction.Fire;
                }
                return AiAction.None;

            default:
                return AiAction.None;
        }
    }

    // Revisa cada frame si el estratega ya respondió, sin bloquear el juego.
    // Checks every frame whether the strategist has answered, without blocking the game.
    private AiAction UpdateAskingAdvisor(float dt)
    {
        _timer += dt;

        if (!_adviceTask.IsCompleted)
        {
            // Si tarda demasiado, se cancela y decide la IA clásica.
            // If it takes too long, it is cancelled and the classic AI decides.
            if (_timer >= AdvisorTimeout)
            {
                _adviceCancellation.Cancel();
                AdvisorStatus = "IA clásica (el LLM no respondió a tiempo)";
                StartSearch();
            }
            return AiAction.None;
        }

        StrategyDecision decision = _adviceTask.IsCompletedSuccessfully ? _adviceTask.Result : null;
        return ApplyAdvice(decision);
    }

    // Valida la respuesta del estratega y la convierte en un plan.
    // Validates the strategist's response and turns it into a plan.
    private AiAction ApplyAdvice(StrategyDecision decision)
    {
        if (decision == null)
        {
            AdvisorStatus = "IA clásica (sin respuesta del LLM)";
            StartSearch();
            return AiAction.None;
        }

        // Solo se aceptan opciones que el juego calculó como válidas.
        // Only options computed as valid by the game are accepted.
        StrategyOption option = _context.FindOption(decision.OptionId);
        if (option == null)
        {
            AdvisorStatus = "IA clásica (respuesta inválida del LLM)";
            StartSearch();
            return AiAction.None;
        }

        AdvisorStatus = $"Estratega LLM: {_advisor.Name}";
        _pendingTaunt = decision.Taunt;

        if (option.Kind == StrategyOptionKind.Heal)
        {
            CurrentGoal = "Curarse";
            _state = State.Done;
            return AiAction.UseMedkit;
        }

        // Atacar: la IA clásica busca el mejor disparo enfocado en ese enemigo.
        // Attack: the classic AI searches for the best shot focused on that enemy.
        _focusTarget = option.Target;
        CurrentGoal = $"Objetivo: {option.Target.Name}";
        StartSearch();
        return AiAction.None;
    }

    // Empieza la búsqueda de disparos (con o sin objetivo prioritario).
    // Starts the shot search (with or without a priority target).
    private void StartSearch()
    {
        _search = _planner.Evaluate(_shooter, _inventory, _terrain, _penguins,
            _windAcceleration, _muzzleDistance, _focusTarget).GetEnumerator();
        _timer = 0f;
        _state = State.Thinking;
    }

    // Ejecuta parte de la búsqueda en cada frame; al terminar, decide.
    // Runs part of the search each frame; when finished, decides.
    private AiAction UpdateThinking(float dt)
    {
        _timer += dt;

        bool isSearching = true;
        for (int i = 0; i < SimulationsPerFrame && isSearching; i++)
            isSearching = _search.MoveNext();

        if (isSearching || _timer < MinThinkTime)
            return AiAction.None;

        return Decide();
    }

    // Elige entre disparar, curarse o pasar, y aplica el error según la dificultad.
    // Chooses between firing, healing or passing, and applies the error based on difficulty.
    private AiAction Decide()
    {
        _plan = _planner.Best;

        bool canHeal = _inventory.Medkits > 0 && _shooter.Health < Penguin.MaxHealth;
        bool isLowHealth = _shooter.Health <= LowHealthThreshold;
        bool hasNoGoodShot = _plan == null || _plan.ExpectedEnemyDamage <= 0f;

        // Si el estratega eligió atacar, se respeta; solo se cura si no hay ningún disparo útil.
        // If the strategist chose to attack, it is respected; it only heals if there is no useful shot.
        bool followsAdvisor = _focusTarget != null;

        if (canHeal && (hasNoGoodShot || (!followsAdvisor && isLowHealth && !_plan.KillsEnemy)))
        {
            CurrentGoal = "Curarse";
            _state = State.Done;
            return AiAction.UseMedkit;
        }

        if (_plan == null)
        {
            _state = State.Done;
            return AiAction.Pass;
        }

        // Error aleatorio de ángulo y potencia según la dificultad.
        // Random angle and power error based on difficulty.
        float angleError = MathHelper.ToRadians(AiDifficultySettings.GetAngleError(_difficulty)) * RandomSigned();
        float powerError = AiDifficultySettings.GetPowerError(_difficulty) * RandomSigned();

        _startAngle = MathF.Atan2(AimDirection.Y, AimDirection.X);
        _targetAngle = MathF.Atan2(_plan.Direction.Y, _plan.Direction.X) + angleError;
        _targetPower = _plan.Weapon.UsesCharge ? Math.Clamp(_plan.Power + powerError, 0.1f, 1f) : 1f;
        _timer = 0f;
        _state = State.Aiming;

        return AiAction.None;
    }

    // Gira la mira gradualmente hacia el ángulo objetivo.
    // Rotates the aim gradually towards the target angle.
    private AiAction UpdateAiming(float dt)
    {
        _timer += dt;
        float t = Math.Min(1f, _timer / AimTime);

        // Interpolar por el camino más corto entre los dos ángulos.
        // Interpolate along the shortest path between the two angles.
        float angle = _startAngle + MathHelper.WrapAngle(_targetAngle - _startAngle) * t;
        AimDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

        if (t < 1f)
            return AiAction.None;

        // Las armas sin carga disparan al terminar de apuntar.
        // Uncharged weapons fire as soon as aiming is done.
        if (!_plan.Weapon.UsesCharge)
        {
            Power = 1f;
            _state = State.Done;
            return AiAction.Fire;
        }

        Power = 0f;
        _state = State.Charging;
        return AiAction.None;
    }

    // Número aleatorio entre -1 y 1.
    // Random number between -1 and 1.
    private float RandomSigned() => (float)(_random.NextDouble() * 2.0 - 1.0);
}