// SISTEMA DE TURNOS
// TURN SYSTEM

using System;
using System.Collections.Generic;
using System.Linq;
using PenguinWars.Core;
using PenguinWars.Entities;

namespace PenguinWars.Gameplay;

// Fases de cada turno.
// Phases of each turn.
public enum TurnPhase
{
    // Aviso de inicio: nadie puede actuar todavía.
    // Start notice: nobody can act yet.
    Starting,

    // El pingüino activo puede moverse, apuntar y disparar.
    // The active penguin can move, aim and fire.
    Playing,

    // Se espera a que el mundo se calme (proyectiles, explosiones, caídas).
    // Waiting for the world to settle (projectiles, explosions, falls).
    Resolving
}

public class TurnManager
{
    // Segundos que dura la fase de juego de cada turno.
    // Seconds that each turn's playing phase lasts.
    public const float TurnDuration = 45f;

    // Duración del aviso de inicio de turno.
    // Duration of the turn start notice.
    private const float StartDuration = 1.5f;

    // Tiempo que el mundo debe estar quieto antes de cambiar de turno.
    // Time the world must stay still before switching turns.
    private const float SettleDelay = 0.8f;

    // Miembros de cada equipo, en orden.
    // Members of each team, in order.
    private readonly Dictionary<Team, List<Penguin>> _teams;

    // Posición de la rotación de cada equipo.
    // Rotation position of each team.
    private readonly Dictionary<Team, int> _nextMemberIndex = new()
    {
        [Team.Heroes] = 0,
        [Team.Aliens] = 0
    };

    // Temporizador de la fase actual (inicio o resolución).
    // Timer of the current phase (starting or resolving).
    private float _phaseTimer;

    public Team CurrentTeam { get; private set; }
    public Penguin CurrentPenguin { get; private set; }
    public TurnPhase Phase { get; private set; }
    public float TimeLeft { get; private set; }
    public int TurnNumber { get; private set; }

    // El turno terminó cuando la resolución acabó y el mundo se mantuvo quieto.
    // The turn is over when resolving finished and the world stayed still.
    public bool IsTurnOver => Phase == TurnPhase.Resolving && _phaseTimer <= 0f;

    public TurnManager(IEnumerable<Penguin> penguins, Team firstTeam)
    {
        // Agrupar a los pingüinos por equipo.
        // Group the penguins by team.
        _teams = penguins.GroupBy(p => p.Team).ToDictionary(g => g.Key, g => g.ToList());
        StartTurnFor(firstTeam);
    }

    // Pasa el turno al equipo contrario.
    // Passes the turn to the opposing team.
    public void BeginNextTurn() => StartTurnFor(CurrentTeam.Opponent());

    private void StartTurnFor(Team team)
    {
        CurrentTeam = team;
        CurrentPenguin = PickNextMember(team);
        Phase = TurnPhase.Starting;
        _phaseTimer = StartDuration;
        TimeLeft = TurnDuration;
        TurnNumber++;
    }

    // Elige al siguiente miembro vivo del equipo, rotando en orden.
    // Picks the next living member of the team, rotating in order.
    private Penguin PickNextMember(Team team)
    {
        List<Penguin> members = _teams[team];

        for (int i = 0; i < members.Count; i++)
        {
            int index = _nextMemberIndex[team] % members.Count;
            _nextMemberIndex[team] = index + 1;

            if (members[index].IsAlive)
                return members[index];
        }

        // No quedan miembros vivos (no debería pasar: se revisa antes).
        // No living members left (should not happen: it is checked before).
        return null;
    }

    public bool IsTeamAlive(Team team) => _teams[team].Any(p => p.IsAlive);

    // Termina la acción del turno: al disparar, pasar, quedarse sin tiempo o morir.
    // Ends the turn's action: when firing, passing, running out of time or dying.
    public void EndAction()
    {
        if (Phase != TurnPhase.Playing)
            return;

        Phase = TurnPhase.Resolving;
        _phaseTimer = SettleDelay;
    }

    // Avanza la fase actual. isWorldSettled indica si ya no hay nada en movimiento.
    // Advances the current phase. isWorldSettled indicates whether nothing is moving anymore.
    public void Update(float dt, bool isWorldSettled)
    {
        switch (Phase)
        {
            case TurnPhase.Starting:
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                    Phase = TurnPhase.Playing;
                break;

            case TurnPhase.Playing:
                TimeLeft = Math.Max(0f, TimeLeft - dt);

                // Se acabó el tiempo o el pingüino activo murió (por ejemplo, se ahogó).
                // Time ran out or the active penguin died (for example, it drowned).
                if (TimeLeft <= 0f || !CurrentPenguin.IsAlive)
                    EndAction();
                break;

            case TurnPhase.Resolving:
                // Si algo se mueve, reiniciar la espera; si todo está quieto, descontar.
                // If something moves, restart the wait; if everything is still, count down.
                if (isWorldSettled)
                    _phaseTimer -= dt;
                else
                    _phaseTimer = SettleDelay;
                break;
        }
    }
}