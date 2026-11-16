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
    [SerializeField] private TMPro.TMP_Text instructionsText;
    [SerializeField] private GameObject pausePanel;

    private const string Instructions =
        "Ganás al llegar a la SALIDA o al dejar al rival sin vida. Elegí un laberinto de 4×4, 6×6 u 8×8.\n\n" +
        "Tenés 30 segundos para armar tu ruta. Hacé clic en los bloques o arrastralos a Secuencia. Podés reordenarlos o borrarlos con ×. Cada turno admite hasta 8 acciones, contando cada paso de Repetir. Una pared o el borde bloquean el movimiento y consumen el paso.\n\n" +
        "Cada jugador puede disparar a partir de su segundo turno, una vez por turno. El radar normal parte de la posición y orientación del rival antes de su último turno ejecutado. Seleccioná el bloque Disparar en Secuencia para desplazar la casilla objetivo o girar la referencia para el siguiente desplazamiento. Acertar resta 20 de vida.\n\n" +
        "Los poderes se activan en tu próximo turno y vencen al terminarlo. Marcá Phase en un movimiento para atravesar un muro o Radar verdadero en un disparo para usar la posición y orientación actuales del rival.\n\n" +
        "Añadí al menos una acción para usar Finalizar y ejecutar. Si una ruta con bloques es inválida, revisá el aviso debajo de Secuencia. Pasar turno omite la ruta, Continuo alterna las rutas guardadas y Escape pausa el juego.";

    private bool _isPaused;

    private void Awake()
    {
        ResumeTime();
        ShowPanel(mainMenuPanel);
        if (instructionsText != null)
        {
            instructionsText.text = Instructions;
            instructionsText.enableAutoSizing = true;
            instructionsText.fontSizeMin = 11;
            instructionsText.fontSizeMax = 20;
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
