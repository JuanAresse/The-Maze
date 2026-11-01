using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverUI;
    public TMP_Text resultadoText;
    public Button reiniciarButton;
    public Button mainMenuButton;
    public string mainMenuSceneName = "MENU";

    private bool _gameEnded;

    public void MostrarResultado(string mensaje)
    {
        if (_gameEnded) return;
        _gameEnded = true;

        EnsureUI();
        gameOverUI.SetActive(true);
        resultadoText.text = mensaje;

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ReiniciarJuego()
    {
        ResumeTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenuPrincipal()
    {
        ResumeTime();
        if (string.IsNullOrEmpty(mainMenuSceneName)) SceneManager.LoadScene(0);
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    private void EnsureUI()
    {
        MazeUI.EnsureEventSystem();

        if (gameOverUI == null)
            CreateDefaultUI();

        if (resultadoText == null)
            resultadoText = gameOverUI.GetComponentInChildren<TMP_Text>(true);

        var buttons = gameOverUI.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (reiniciarButton == null && button.name.Contains("Reiniciar")) reiniciarButton = button;
            if (mainMenuButton == null && button.name.Contains("MainMenu")) mainMenuButton = button;
        }

        BindButton(reiniciarButton, ReiniciarJuego);
        BindButton(mainMenuButton, IrAlMenuPrincipal);
    }

    private void CreateDefaultUI()
    {
        gameOverUI = MazeUI.Canvas("GameOverUI", 1000).gameObject;
        var panel = MazeUI.Box("Panel", gameOverUI.transform, new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f), new Color(0f, 0f, 0f, 0.65f));

        resultadoText = CreateText("ResultadoText", panel, string.Empty, 30, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.85f));
        mainMenuButton = CreateButton("MainMenuButton", panel, "Menu Principal", new Vector2(0.15f, 0.05f), new Vector2(0.4f, 0.25f), new Color(0.2f, 0.4f, 0.8f));
        reiniciarButton = CreateButton("ReiniciarButton", panel, "Reiniciar", new Vector2(0.6f, 0.05f), new Vector2(0.85f, 0.25f), new Color(0.2f, 0.6f, 0.2f));
        gameOverUI.SetActive(false);
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _gameEnded = false;
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Vector2 min, Vector2 max)
    {
        var text = MazeUI.Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max, Color color)
    {
        var button = MazeUI.ButtonFrame(name, parent, min, max, color);
        CreateText("Text", button.transform, label, 20, Vector2.zero, Vector2.one);
        return button;
    }
}
