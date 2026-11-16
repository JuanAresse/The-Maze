using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnBasedManager : MonoBehaviour
{
    public Character PlayerA => _players[0];
    public Character PlayerB => _players[1];
    public Character CurrentPlayer => _players[CurrentTurn];
    public int CurrentTurn { get; private set; }
    public bool IsExecuting { get; private set; }
    public bool GameEnded { get; private set; }
    public bool Continuous { get; private set; }
    public float TimeRemaining { get; private set; }
    public int StepNumber { get; private set; }
    public int StepCount { get; private set; }
    public PlanStep ActiveStep { get; private set; }
    public string Feedback { get; private set; } = "";
    public float StepDelay = .25f;
    public float ContinuousTurnDelay = .15f;
    public float EditTimeLimit = 30f;
    public GameOverManager gameOverManager;
    private readonly Character[] _players = new Character[2];
    private readonly Camera[] _cameras = new Camera[2];
    private readonly Vector2Int[] _last = new Vector2Int[2];
    private readonly Vector2Int[] _previous = new Vector2Int[2];
    private readonly Character.Facing[] _lastFacing = new Character.Facing[2];
    private readonly Character.Facing[] _previousFacing = new Character.Facing[2];
    private readonly List<ActionBlock>[] _plans = { new List<ActionBlock>(), new List<ActionBlock>() };
    private readonly bool[] _hasFinishedFirstTurn = new bool[2];
    private MovementBoard _board;
    private bool _initialized;
    private int _shots;
    public List<ActionBlock> CurrentPlan => _plans[CurrentTurn];
    public bool CanShootThisTurn => _hasFinishedFirstTurn[CurrentTurn];
    public bool CanEdit => _initialized && !IsExecuting && !GameEnded && Time.timeScale > 0f;
    public PlanValidation ValidateCurrentPlan() => ActionPlan.Validate(CurrentPlan, CanShootThisTurn);

    public void Initialize(Character a, Character b, Camera camA, Camera camB)
    {
        _players[0] = a; _players[1] = b;
        _cameras[0] = camA; _cameras[1] = camB != null ? camB : camA;
        for (int i = 0; i < 2; i++)
        {
            _players[i].manager = this;
            _last[i] = _previous[i] = _players[i].CellPosition;
            _lastFacing[i] = _previousFacing[i] = _players[i].CurrentFacing;
        }
        _initialized = true;
        _board = gameObject.AddComponent<MovementBoard>();
        _board.Initialize(this);
        SetTurn(0);
    }

    private void Update()
    {
        if (!_initialized || GameEnded || Time.timeScale <= 0f || IsExecuting) return;
        if (Continuous)
        {
            if (ValidateCurrentPlan().IsValid) Execute();
            else { Continuous = false; ShowFeedback("Continuo detenido: la ruta de este jugador necesita revisión."); _board.Refresh(); }
            return;
        }
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        if (TimeRemaining <= 0f) FinishTurn(false, "Se agotó el tiempo. Cambió el turno.");
    }

    public void Execute()
    {
        if (!CanEdit) return;
        var validation = ValidateCurrentPlan();
        if (!validation.IsValid) { ShowFeedback(validation.Error); return; }
        StartCoroutine(ExecutePlan(validation));
    }

    private IEnumerator ExecutePlan(PlanValidation validation)
    {
        IsExecuting = true;
        _shots = 0; StepNumber = 0; StepCount = validation.Steps.Count;
        _board.Refresh();
        foreach (var step in validation.Steps)
        {
            while (Time.timeScale <= 0f && !GameEnded) yield return null;
            if (GameEnded) yield break;
            ActiveStep = step; StepNumber++;
            _board.Highlight(step);
            CurrentPlayer.ExecutePlanned(step.Block);
            if (GameEnded) yield break;
            yield return new WaitForSeconds(StepDelay);
        }
        if (Continuous) yield return new WaitForSeconds(ContinuousTurnDelay);
        IsExecuting = false; ActiveStep = null;
        FinishTurn(true, "Tu turno: prepará hasta 8 acciones.");
    }

    public void PassTurn()
    {
        if (CanEdit) { Continuous = false; FinishTurn(false, "El rival pasó su turno."); }
    }

    private void FinishTurn(bool executed, string message)
    {
        int i = CurrentTurn;
        _hasFinishedFirstTurn[i] = true;
        if (executed)
        {
            _previous[i] = _last[i]; _previousFacing[i] = _lastFacing[i];
            _last[i] = _players[i].CellPosition; _lastFacing[i] = _players[i].CurrentFacing;
        }
        _players[i].ClearActivePowerups();
        SetTurn(1 - i, message);
    }

    private void SetTurn(int index, string message = "Arrastrá acciones. Las direcciones corresponden al laberinto.")
    {
        CurrentTurn = index; _shots = 0;
        TimeRemaining = EditTimeLimit;
        CurrentPlayer.PromoteQueuedPowerups();
        SelectCamera(index);
        if(UnityEngine.EventSystems.EventSystem.current!=null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        _board.SetPlan(CurrentPlan);
        ShowFeedback(CurrentPlayer.activePhase
            ? "Phase disponible: seleccioná un movimiento en Secuencia y activá Phase en esa acción."
            : message);
    }

    private void SelectCamera(int index)
    {
        Camera active = _cameras[index];
        foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            camera.enabled = camera == active;
            if (camera == active) camera.rect = new Rect(0, 0, 1, 1);
        }
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            listener.enabled = active != null && listener.gameObject == active.gameObject;
        if (active != null && active.GetComponent<AudioListener>() == null) active.gameObject.AddComponent<AudioListener>();
    }

    public void ToggleContinuous()
    {
        if (GameEnded || Time.timeScale <= 0f) return;
        Continuous = !Continuous;
        ShowFeedback(Continuous ? "Continuo: alterna las rutas guardadas de ambos jugadores." : "Continuo detenido. La secuencia en curso termina normalmente.");
        _board.Refresh();
    }

    public void ShowFeedback(string message)
    {
        Feedback = message;
        if (_board != null) _board.UpdateStatus();
    }

    public bool RegisterShot(Character shooter)
    {
        if (!CanShootThisTurn) { ShowFeedback(ActionPlan.FirstTurnShotError); return false; }
        if (!IsExecuting || GameEnded || shooter != CurrentPlayer || _shots >= ActionPlan.MaxShots)
        { ShowFeedback("Solo se permite un disparo por turno."); return false; }
        _shots++; return true;
    }

    public void TryShoot(Character shooter, List<RadarAdjustment> adjustments, bool useTrueRadar)
    {
        if (!IsExecuting || !CanShootThisTurn || shooter != CurrentPlayer || _shots >= ActionPlan.MaxShots) return;
        bool consumed = useTrueRadar && shooter.activeTrueRadar;
        var radar = GetLastKnownRadarFor(shooter, useTrueRadar);
        Vector2Int target = radar.pos;
        int facing = radar.facing == Character.Facing.North ? 0 : radar.facing == Character.Facing.East ? 1 : radar.facing == Character.Facing.South ? 2 : 3;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var adjustment in adjustments)
        {
            if (adjustment == RadarAdjustment.TurnLeft) facing = (facing + 3) % 4;
            else if (adjustment == RadarAdjustment.TurnRight) facing = (facing + 1) % 4;
            else
            {
                int offset = adjustment == RadarAdjustment.Forward ? 0 : adjustment == RadarAdjustment.Right ? 1 : adjustment == RadarAdjustment.Backward ? 2 : 3;
                target += directions[(facing + offset) % 4];
            }
        }
        shooter.ShootAt(target);
        if (consumed && !GameEnded) ShowFeedback(Feedback + " Radar verdadero consumido.");
        else if (useTrueRadar && !GameEnded) ShowFeedback(Feedback + " Sin Radar verdadero disponible: se usó el histórico.");
    }

    public (Vector2Int pos, Character.Facing facing) GetLastKnownRadarFor(Character requester, bool useTrueRadar = false)
    {
        int own = requester == PlayerA ? 0 : 1;
        int opponent = 1 - own;
        if (useTrueRadar && requester.activeTrueRadar)
        {
            requester.activeTrueRadar = false;
            return (_players[opponent].CellPosition, _players[opponent].CurrentFacing);
        }
        return (_previous[opponent], _previousFacing[opponent]);
    }

    public void ShowPowerupCollected(Character collector, Powerup.PowerupType type)
    {
        ShowFeedback((type == Powerup.PowerupType.Phase ? "Phase" : "Radar verdadero") + " recogido: disponible en tu próximo turno.");
    }

    public void PlayerLost(Character loser) => EndGame((loser == PlayerA ? "Jugador B" : "Jugador A") + " ganó: el rival se quedó sin vida.");
    public void PlayerExited(Character player) => EndGame((player == PlayerA ? "Jugador A" : "Jugador B") + " ganó: alcanzó la salida.");

    private void EndGame(string message)
    {
        if (GameEnded) return;
        GameEnded = true; Continuous = false; IsExecuting = false;
        StopAllCoroutines();
        if (_board != null) _board.Hide();
        if (gameOverManager == null) gameOverManager = FindFirstObjectByType<GameOverManager>() ?? gameObject.AddComponent<GameOverManager>();
        gameOverManager.MostrarResultado(message);
    }
}
