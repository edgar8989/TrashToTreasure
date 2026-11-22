using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "Game";
    [SerializeField] private GameObject creditsPanel;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplayScene);
    }

    public void ShowCredits() { if (creditsPanel != null) creditsPanel.SetActive(true); }
    public void CloseCredits() { if (creditsPanel != null) creditsPanel.SetActive(false); }
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
