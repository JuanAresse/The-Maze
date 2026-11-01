using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private MazeCell _mazeCellPrefab;
    [SerializeField] private int _mazeWidth;
    [SerializeField] private int _mazeDepth;
    [SerializeField] private GameObject _characterPrefab;
    [SerializeField] private CamaraTerceraPersona _cameraP1Controller;
    [SerializeField] private CamaraTerceraPersona _cameraP2Controller;
    [SerializeField] private GameObject _player1ModelPrefab;
    [SerializeField] private GameObject _player2ModelPrefab;
    [SerializeField] private GameObject _powerupPhasePrefab;
    [SerializeField] private GameObject _powerupTrueRadarPrefab;

    private MazeCell[,] _mazeGrid;
    private Vector2Int _spawnPosA;
    private Vector2Int _spawnPosB;
    private Vector2Int _exitPos;

    private void Start()
    {
        if(!GameOptions.SelectionReady)
        {
            MazeSetupDialog.Show(Generate,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MENU"));
            return;
        }
        Generate();
    }

    private void Generate()
    {
        _mazeWidth=_mazeDepth=GameOptions.MazeSize;
        if (!CanGenerate()) return;

        CreateGrid();
        GenerateMaze(null, _mazeGrid[0, 0]);
        DefineEntranceAndExit();
        SpawnPowerups();
        SetupCharacters();
    }

    private bool CanGenerate()
    {
        if (_mazeCellPrefab == null)
        {
            Debug.LogError("MazeGenerator: falta asignar el prefab de celda.");
            enabled = false;
            return false;
        }

        if (_mazeWidth < 3 || _mazeDepth < 3)
        {
            Debug.LogError("MazeGenerator: el laberinto debe medir al menos 3x3.");
            enabled = false;
            return false;
        }

        return true;
    }

    private void CreateGrid()
    {
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];
        for (int x = 0; x < _mazeWidth; x++)
            for (int z = 0; z < _mazeDepth; z++)
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0f, z), Quaternion.identity);
    }

    private void GenerateMaze(MazeCell previous, MazeCell current)
    {
        current.Visit();
        ClearWallsBetween(previous, current);

        foreach (var next in UnvisitedNeighbors(current).OrderBy(_ => Random.value).ToList())
            if (!next.IsVisited) GenerateMaze(current, next);
    }

    private IEnumerable<MazeCell> UnvisitedNeighbors(MazeCell cell)
    {
        Vector2Int pos = CellPosition(cell);
        foreach (Vector2Int dir in Directions())
        {
            Vector2Int next = pos + dir;
            if (IsInside(next) && !_mazeGrid[next.x, next.y].IsVisited)
                yield return _mazeGrid[next.x, next.y];
        }
    }

    private void ClearWallsBetween(MazeCell previous, MazeCell current)
    {
        if (previous == null) return;

        Vector2Int delta = CellPosition(current) - CellPosition(previous);
        ClearWall(previous, delta);
        ClearWall(current, -delta);
    }

    private void DefineEntranceAndExit()
    {
        bool horizontalExit = Random.Range(0, 2) == 0;
        bool farSide = Random.Range(0, 2) == 1;

        if (horizontalExit)
        {
            int entryX = farSide ? _mazeWidth - 1 : 0;
            int exitX = farSide ? 0 : _mazeWidth - 1;
            _spawnPosA = new Vector2Int(entryX, 0);
            _spawnPosB = new Vector2Int(entryX, _mazeDepth - 1);
            _exitPos = new Vector2Int(exitX, Random.Range(0, _mazeDepth));
            ClearWall(_mazeGrid[_exitPos.x, _exitPos.y], farSide ? Vector2Int.left : Vector2Int.right);
        }
        else
        {
            int entryZ = farSide ? _mazeDepth - 1 : 0;
            int exitZ = farSide ? 0 : _mazeDepth - 1;
            _spawnPosA = new Vector2Int(0, entryZ);
            _spawnPosB = new Vector2Int(_mazeWidth - 1, entryZ);
            _exitPos = new Vector2Int(Random.Range(0, _mazeWidth), exitZ);
            ClearWall(_mazeGrid[_exitPos.x, _exitPos.y], farSide ? Vector2Int.down : Vector2Int.up);
        }

        CreateExitZone(_mazeGrid[_exitPos.x, _exitPos.y]);
    }

    private void SetupCharacters()
    {
        if (_characterPrefab == null)
        {
            Debug.LogWarning("MazeGenerator: falta asignar el prefab del personaje.");
            return;
        }

        Character playerA = CreateCharacter(_spawnPosA, "Jugador A", _player1ModelPrefab);
        Character playerB = CreateCharacter(_spawnPosB, "Jugador B", _player2ModelPrefab);
        if (playerA == null || playerB == null) return;

        if (_cameraP1Controller != null) _cameraP1Controller.objetivo = playerA.transform;
        if (_cameraP2Controller != null) _cameraP2Controller.objetivo = playerB.transform;

        Camera camA = CameraFrom(_cameraP1Controller);
        Camera camB = CameraFrom(_cameraP2Controller);
        PickFallbackCameras(ref camA, ref camB);
        ConfigurePlayerCameras(camA, camB);

        var manager = GetComponent<TurnBasedManager>();
        if (manager == null) manager = gameObject.AddComponent<TurnBasedManager>();
        manager.Initialize(playerA, playerB, camA, camB);
    }

    private Character CreateCharacter(Vector2Int cell, string playerName, GameObject modelPrefab)
    {
        var go = Instantiate(_characterPrefab, new Vector3(cell.x, 0.5f, cell.y), Quaternion.identity);
        var character = go.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("El prefab del jugador debe tener componente Character.");
            return null;
        }

        if (modelPrefab != null)
            character.AttachVisual(Instantiate(modelPrefab, go.transform, false));

        character.Init(_mazeGrid, cell, playerName);
        return character;
    }

    private void SpawnPowerups()
    {
        int amount = _mazeWidth / 4;
        var blocked = new HashSet<Vector2Int> { _spawnPosA, _spawnPosB, _exitPos };
        var cells = AllCells().Where(cell => !blocked.Contains(cell)).OrderBy(_ => Random.value).ToList();

        int index = 0;
        for (int i = 0; i < amount && index < cells.Count; i++, index++)
            SpawnPowerupAt(cells[index], Powerup.PowerupType.Phase);
        for (int i = 0; i < amount && index < cells.Count; i++, index++)
            SpawnPowerupAt(cells[index], Powerup.PowerupType.TrueRadar);
    }

    private void SpawnPowerupAt(Vector2Int cellPos, Powerup.PowerupType type)
    {
        GameObject prefab = type == Powerup.PowerupType.Phase ? _powerupPhasePrefab : _powerupTrueRadarPrefab;
        GameObject go = prefab != null
            ? Instantiate(prefab, new Vector3(cellPos.x, 0.5f, cellPos.y), Quaternion.identity)
            : GameObject.CreatePrimitive(type == Powerup.PowerupType.Phase ? PrimitiveType.Cube : PrimitiveType.Sphere);

        if (prefab == null)
        {
            go.transform.position = new Vector3(cellPos.x, 0.5f, cellPos.y);
            go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            if(type==Powerup.PowerupType.Phase)go.transform.rotation=Quaternion.Euler(0,45,30);
        }

        var powerup = go.GetComponent<Powerup>();
        if (powerup == null) powerup = go.AddComponent<Powerup>();
        powerup.Type = type;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) SetMarkerColor(renderer,type == Powerup.PowerupType.Phase ? Color.cyan : Color.magenta);

        go.transform.SetParent(_mazeGrid[cellPos.x, cellPos.y].transform, true);
        var collider = go.GetComponent<Collider>();
        if (collider == null) collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        WorldLabel.Create(go.transform,type==Powerup.PowerupType.Phase?"PHASE":"RADAR",new Vector3(0,.45f,0),type==Powerup.PowerupType.Phase?Color.cyan:Color.magenta);
    }

    private void CreateExitZone(MazeCell cell)
    {
        var exit = new GameObject("ExitZone");
        exit.transform.SetParent(cell.transform, false);
        exit.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        var box = exit.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.9f, 1f, 0.9f);
        exit.AddComponent<ExitZone>();
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name="ExitMarker";marker.transform.SetParent(exit.transform,false);
        marker.transform.localPosition=new Vector3(0,-.47f,0);marker.transform.localScale=new Vector3(.78f,.015f,.78f);
        SetMarkerColor(marker.GetComponent<Renderer>(),new Color(.15f,.80f,.35f));
        Destroy(marker.GetComponent<Collider>());
        WorldLabel.Create(exit.transform,"SALIDA",new Vector3(0,.5f,0),new Color(.4f,1,.5f));
    }

    private void PickFallbackCameras(ref Camera camA, ref Camera camB)
    {
        Camera[] cameras = Camera.allCameras;
        if (camA == null && cameras.Length > 0) camA = cameras[0];
        if (camB == null && cameras.Length > 1) camB = cameras[1];
        if (camB == null) camB = camA;
    }

    private void SetMarkerColor(Renderer renderer,Color color)
    {
        // Primitive defaults use the built-in pipeline's Standard material, which
        // renders pink under URP. Use the same pipeline as the maze instead.
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader!=null)renderer.sharedMaterial=new Material(shader);
        renderer.material.color=color;
    }

    private void ConfigurePlayerCameras(Camera camA, Camera camB)
    {
        if (camA == null) return;

        if (camB != null && camA != camB)
        {
            ConfigureCamera(camA, new Rect(0f, 0f, 1f, 1f));
            ConfigureCamera(camB, new Rect(0f, 0f, 1f, 1f));
            camB.enabled=false;
        }
        else
        {
            ConfigureCamera(camA, new Rect(0f, 0f, 1f, 1f));
        }
    }

    private void ConfigureCamera(Camera camera, Rect rect)
    {
        camera.enabled = true;
        camera.rect = rect;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = new Color(0.6f, 0.65f, 0.7f);
        camera.cullingMask = ~0;
        camera.depth = 0;
    }

    private Camera CameraFrom(CamaraTerceraPersona controller)
    {
        if (controller == null) return null;
        return controller.GetComponent<Camera>() ?? controller.GetComponentInChildren<Camera>();
    }

    private IEnumerable<Vector2Int> AllCells()
    {
        for (int x = 0; x < _mazeWidth; x++)
            for (int z = 0; z < _mazeDepth; z++)
                yield return new Vector2Int(x, z);
    }

    private bool IsInside(Vector2Int pos) => pos.x >= 0 && pos.x < _mazeWidth && pos.y >= 0 && pos.y < _mazeDepth;
    private Vector2Int CellPosition(MazeCell cell) => new Vector2Int((int)cell.transform.position.x, (int)cell.transform.position.z);

    private IEnumerable<Vector2Int> Directions()
    {
        yield return Vector2Int.right;
        yield return Vector2Int.left;
        yield return Vector2Int.up;
        yield return Vector2Int.down;
    }

    private void ClearWall(MazeCell cell, Vector2Int dir)
    {
        if (dir == Vector2Int.right) cell.ClearRightWall();
        else if (dir == Vector2Int.left) cell.ClearLeftWall();
        else if (dir == Vector2Int.up) cell.ClearFrontWall();
        else if (dir == Vector2Int.down) cell.ClearBackWall();
    }
}
