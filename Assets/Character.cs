using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public enum Facing { North, South, East, West }

    public TurnBasedManager manager;
    public PlayerHealth health;

    [SerializeField] private float fixedHeight = 0.5f;
    [SerializeField] private float visualRotateSpeed = 10f;
    [SerializeField] private Vector3 _visualLocalOffset = new Vector3(0f, 0.5f, 0f);

    public Vector2Int CellPosition => _pos;
    public Facing CurrentFacing { get; private set; } = Facing.North;

    public bool queuedPhase;
    public bool queuedTrueRadar;
    public bool activePhase;
    public bool activeTrueRadar;

    private MazeCell[,] _grid;
    private int _width;
    private int _depth;
    private Vector2Int _pos;
    private Rigidbody _rb;
    private Transform _visual;
    private Quaternion _targetVisualRotation = Quaternion.identity;

    public void Init(MazeCell[,] grid, Vector2Int startPos, string playerName)
    {
        _grid = grid;
        _width = grid.GetLength(0);
        _depth = grid.GetLength(1);
        _pos = startPos;
        name = playerName;
        CurrentFacing = Facing.North;

        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        health = GetComponent<PlayerHealth>();
        if (health == null) health = gameObject.AddComponent<PlayerHealth>();
        health.CacheRenderers();

        ApplyPosition();
    }

    private void LateUpdate()
    {
        ApplyPosition(false);
        if (_visual != null)
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, _targetVisualRotation, visualRotateSpeed * Time.deltaTime);
    }

    public void AttachVisual(GameObject visualInstance)
    {
        if (visualInstance == null) return;

        GameObject instance = visualInstance.scene.rootCount == 0 ? Instantiate(visualInstance) : visualInstance;
        Transform root = transform.Find("VisualRoot") ?? new GameObject("VisualRoot").transform;
        root.SetParent(transform, false);
        root.localPosition = _visualLocalOffset;
        root.localRotation = Quaternion.identity;
        root.localScale = InverseScale(transform.localScale);

        instance.transform.SetParent(root, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        _visual = instance.transform;
        _targetVisualRotation = _visual.localRotation;

        var visualRenderers = new HashSet<Renderer>(_visual.GetComponentsInChildren<Renderer>(true));
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            if (!visualRenderers.Contains(renderer)) renderer.enabled = false;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void TeleportTo(Vector2Int cellPos)
    {
        _pos = cellPos;
        ApplyPosition();
    }
#endif

    public bool TryMove(Vector2Int dir, bool usePhase)
    {
        if (_grid == null || dir == Vector2Int.zero) return false;

        SetFacing(DirToFacing(dir));
        Vector2Int destination = _pos + dir;
        if (!IsInside(destination))
        {
            manager?.ShowFeedback("Límite del laberinto: no avanza.");
            return false;
        }

        bool blockedByWall = HasWall(_pos, dir) || HasWall(destination, -dir);
        if (blockedByWall && (!usePhase || !activePhase))
        {
            manager?.ShowFeedback(usePhase ? "Movimiento bloqueado: no tenés Phase disponible." : "Movimiento bloqueado por una pared.");
            return false;
        }

        _pos = destination;
        if (blockedByWall)
        {
            activePhase = false;
            manager?.ShowFeedback("Phase consumido: atravesaste un muro.");
        }
        else
        {
            manager?.ShowFeedback(usePhase && activePhase
                ? "Avanzaste. Phase sigue disponible porque no había muro."
                : "Avanzaste una casilla.");
        }
        ApplyPosition();
        return true;
    }

    public void ExecutePlanned(ActionBlock block)
    {
        switch (block.Kind)
        {
            case ActionKind.Up: TryMove(Vector2Int.up, block.UsePhase); break;
            case ActionKind.Down: TryMove(Vector2Int.down, block.UsePhase); break;
            case ActionKind.Left: TryMove(Vector2Int.left, block.UsePhase); break;
            case ActionKind.Right: TryMove(Vector2Int.right, block.UsePhase); break;
            case ActionKind.Wait: manager?.ShowFeedback("Esperar: permanecés en la misma casilla."); break;
            case ActionKind.Shoot: manager?.TryShoot(this, block.Adjustments, block.UseTrueRadar); break;
        }
    }

    public void ShootAt(Vector2Int targetCell)
    {
        if (manager == null) return;
        if (!manager.RegisterShot(this)) return;
        Character target = this == manager.PlayerA ? manager.PlayerB : manager.PlayerA;
        if (target != null && target.CellPosition == targetCell && target.health != null)
        {
            target.health.TakeDamage(20);
            manager.ShowFeedback("Impacto: -20 de vida al rival.");
        }
        else manager.ShowFeedback("Disparo fallido: el rival no está en esa casilla.");
    }

    public void CollectPowerup(Powerup.PowerupType type)
    {
        if (type == Powerup.PowerupType.Phase) queuedPhase = true;
        if (type == Powerup.PowerupType.TrueRadar) queuedTrueRadar = true;
    }

    public void PromoteQueuedPowerups()
    {
        activePhase |= queuedPhase;
        activeTrueRadar |= queuedTrueRadar;
        queuedPhase = false;
        queuedTrueRadar = false;
    }

    public void ClearActivePowerups()
    {
        activePhase = false;
        activeTrueRadar = false;
    }

    private void ApplyPosition(bool checkExit = true)
    {
        Vector3 world = new Vector3(_pos.x, fixedHeight, _pos.y);
        if (_rb != null) _rb.position = world;
        else transform.position = world;

        if (checkExit)
        {
            if (IsInside(_pos))
            {
                var powerup = _grid[_pos.x, _pos.y].GetComponentInChildren<Powerup>();
                if (powerup != null) powerup.Collect(this);
            }
            CheckExit();
        }
    }

    private bool HasWall(Vector2Int cellPos, Vector2Int dir)
    {
        MazeCell cell = _grid[cellPos.x, cellPos.y];
        if (dir == Vector2Int.right) return cell.HasRightWall();
        if (dir == Vector2Int.left) return cell.HasLeftWall();
        if (dir == Vector2Int.up) return cell.HasFrontWall();
        if (dir == Vector2Int.down) return cell.HasBackWall();
        return true;
    }

    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _depth;
    }

    private void CheckExit()
    {
        if (!IsInside(_pos)) return;
        if (manager != null && _grid[_pos.x, _pos.y].GetComponentInChildren<ExitZone>(true) != null)
            manager.PlayerExited(this);
    }

    private void SetFacing(Facing facing)
    {
        CurrentFacing = facing;
        float yaw = 0f;
        if (facing == Facing.East) yaw = 90f;
        if (facing == Facing.South) yaw = 180f;
        if (facing == Facing.West) yaw = -90f;
        _targetVisualRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private Facing DirToFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Facing.North;
        if (dir == Vector2Int.down) return Facing.South;
        if (dir == Vector2Int.right) return Facing.East;
        return Facing.West;
    }

    private Vector3 InverseScale(Vector3 scale)
    {
        return new Vector3(
            scale.x != 0f ? 1f / scale.x : 1f,
            scale.y != 0f ? 1f / scale.y : 1f,
            scale.z != 0f ? 1f / scale.z : 1f
        );
    }
}
