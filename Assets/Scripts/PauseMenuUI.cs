
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuUI : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Pause Menu Settings")]
    [SerializeField] private GameObject pausePanel;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Awake()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("PausePanel belum dimasukkan!");
        }

        Debug.Log("PauseMenuUI Awake berhasil.");
    }

    private void Update()
    {
        // Deteksi tombol P
        if (!PauseKeyPressed())
            return;

        Debug.Log("TOMBOL P TERDETEKSI");

        // Jika sedang pause, lanjutkan permainan
        if (IsPaused)
        {
            ContinueGame();
            return;
        }

        // Jangan pause ketika player mati
        if (GameManager.I != null && GameManager.I.IsDead)
            return;

        // Jangan pause ketika menu interaksi lain terbuka
        if (WorldObject.MenuOpen ||
            VehicleController.CargoMenuOpen)
            return;

        // Buka pause menu
        PauseGame();
    }

    // =====================================
    // DETEKSI INPUT P
    // =====================================

    private bool PauseKeyPressed()
    {
        bool pressed = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            pressed = true;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.P))
        {
            pressed = true;
        }
        #endif

        return pressed;
    }

    // =====================================
    // PAUSE GAME
    // =====================================

    public void PauseGame()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("GAME BERHASIL DI-PAUSE");
    }

    // =====================================
    // CONTINUE GAME
    // =====================================

    public void ContinueGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("GAME BERHASIL DILANJUTKAN");
    }

    // =====================================
    // EXIT TO MAIN MENU
    // =====================================

    public void ExitToMainMenu()
    {
        Debug.Log("KEMBALI KE MAIN MENU");

        IsPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuScene);
    }

    private void OnDestroy()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
