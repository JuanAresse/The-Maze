using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlMenu : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string gameplaySceneName = "The maze";
    [SerializeField] private string mainMenuSceneName = "MENU";

    [Header("Paneles UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject pausePanel;

    private bool _isPaused;

    private void Awake()
    {
        ResumeTime();
        ShowPanel(mainMenuPanel);
        if(instructionsPanel!=null)
            foreach(var text in instructionsPanel.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if(text.text.Contains("Para ganar"))
                {
                    text.text="Ganás al alcanzar la salida o eliminar al rival.\n\nElegí un mapa de 4×4, 6×6 u 8×8 antes de jugar. Cada turno tiene 30 segundos para planificar, hasta 8 acciones ejecutadas y un disparo.\n\nArrastrá los iconos a Secuencia. Podés ordenarlos, eliminarlos y desplazarte horizontalmente. Seleccioná Repetir para editar su contenido y cantidad; las acciones interiores también consumen presupuesto.\n\nArriba, abajo, izquierda y derecha son direcciones fijas del laberinto. Las paredes bloquean el movimiento. Esperar consume un paso.\n\nDisparar usa el radar histórico del rival. Sus ajustes son relativos a la orientación registrada. Un impacto resta 20 de los 100 puntos de vida.\n\nPhase y Radar verdadero se recogen para el próximo turno. Activá Phase en un movimiento o Radar verdadero en un disparo. Expiran al terminar el turno.\n\nFinalizar y ejecutar inicia la ruta; Pasar turno la omite. Continuo alterna las rutas guardadas. Escape pausa también el tiempo de edición.";
                    text.enableAutoSizing=true;text.fontSizeMin=11;text.fontSizeMax=20;
                }
    }
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        var turnManager=FindFirstObjectByType<TurnBasedManager>();
        if(turnManager!=null&&turnManager.GameEnded)return;
        TogglePause();
    }

    public void Play() => MazeSetupDialog.Show(()=>LoadScene(gameplaySceneName),()=>ShowPanel(mainMenuPanel));

    public void OpenOptions() => ShowPanel(optionsPanel ?? mainMenuPanel);
    public void OpenInstructions() => ShowPanel(instructionsPanel ?? mainMenuPanel);
    public void BackToMainMenu() => ShowPanel(mainMenuPanel);
    public void Exit() => Application.Quit();

    public void TogglePause()
    {
        if (_isPaused && !pausePanel.activeSelf) BackToPause();
        else if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (pausePanel == null) return;

        ShowPanel(pausePanel);
        Time.timeScale = 0f;
        _isPaused = true;
        ShowCursor();
    }

    public void Resume()
    {
        ShowPanel(null);
        ResumeTime();
    }

    public void RestartLevel() => LoadScene(SceneManager.GetActiveScene().name);
    public void OpenOptionsFromPause() => OpenOptions();
    public void OpenInstructionsFromPause() => OpenInstructions();

    public void BackToPause() => Pause();

    public void BackToMainMenuFromPause() => LoadScene(mainMenuSceneName);

    private void LoadScene(string sceneName)
    {
        ResumeTime();
        SceneManager.LoadScene(sceneName);
    }

    private void ShowPanel(GameObject panel)
    {
        SetActive(mainMenuPanel, false);
        SetActive(optionsPanel, false);
        SetActive(instructionsPanel, false);
        SetActive(pausePanel, false);
        SetActive(panel, true);
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        ShowCursor();
    }

    private static void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private static void SetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

}
