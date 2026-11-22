using System.Collections;
using UnityEngine;

/// <summary>
/// Opening conversation with Pak Budi.
/// After the conversation, Pak Budi fades out and moves to Destination Point.
/// Attach this script to Pak Budi's ROOT GameObject.
/// </summary>
public class PakBudiDialogue : Interactable
{
    public static bool DialogueOpen { get; private set; }
    public static bool OpeningSequenceCompleted { get; private set; }

    [Header("Opening Dialogue")]
    public bool startAutomatically = true;
    [Min(0.5f)] public float interactionDistance = 3f;
    public string characterName = "Pak Budi";

    [TextArea(2, 4)]
    public string[] dialogueLines =
    {
        "Pak Budi: Selamat pagi. Bisa saya bicara sebentar?",
        "Pemain: Tentu, Pak. Ada apa?",
        "Pak Budi: Saya ingin mengingatkan bahwa SPP anakmu masih belum dibayar.",
        "Pemain: Iya, Pak. Saya belum punya cukup uang untuk membayarnya.",
        "Pak Budi: Sebaiknya segera dilunasi. Jangan sampai pembayaran SPP semakin terlambat.",
        "Pemain: Baik, Pak. Saya akan berusaha mengumpulkan uangnya.",
        "Pak Budi: Kamu bisa mengumpulkan uang dengan memilah dan mendaur ulang sampah. Ada lima tugas yang perlu kamu selesaikan.",
        "Pemain: Baik, Pak. Saya akan menyelesaikan tugas-tugas itu agar bisa membayar SPP anak saya."
    };

    [Header("Move Pak Budi After Dialogue")]
    [Tooltip("Create an Empty GameObject at Pak Budi's later location and assign it here.")]
    public Transform destinationPoint;
    [Min(0.05f)] public float fadeDuration = 0.8f;
    [Min(0f)] public float blackScreenDuration = 0.5f;

    private PlayerController player;
    private GameManager gameManager;
    private int currentLine;
    private bool dialogueActive;
    private bool transitionRunning;
    private float fadeAlpha;

    private bool previousPlayerEnabled;
    private bool previousGameManagerEnabled;
    private float previousTimeScale;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    void Awake()
    {
        prompt = "";
        OpeningSequenceCompleted = false;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        gameManager = GameManager.I;

        if (startAutomatically)
            StartCoroutine(StartOpeningDialogueNextFrame());
    }

    IEnumerator StartOpeningDialogueNextFrame()
    {
        yield return null;
        BeginDialogue();
    }

    public override void Interact()
    {
        if (OpeningSequenceCompleted || DialogueOpen)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null &&
            Vector3.Distance(player.transform.position, transform.position) > interactionDistance)
        {
            if (GameManager.I != null)
                GameManager.I.Notify("Dekati Pak Budi terlebih dahulu.");
            return;
        }

        BeginDialogue();
    }

    void BeginDialogue()
    {
        if (OpeningSequenceCompleted || DialogueOpen ||
            dialogueLines == null || dialogueLines.Length == 0)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (gameManager == null)
            gameManager = GameManager.I;

        currentLine = 0;
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;

        if (player != null)
        {
            previousPlayerEnabled = player.enabled;
            player.enabled = false;
        }

        if (gameManager != null)
        {
            previousGameManagerEnabled = gameManager.enabled;
            gameManager.enabled = false;
        }

        dialogueActive = true;
        DialogueOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void FinishDialogue()
    {
        if (!dialogueActive || transitionRunning)
            return;

        StartCoroutine(FinishSequence());
    }

    IEnumerator FinishSequence()
    {
        transitionRunning = true;
        yield return Fade(0f, 1f);

        MoveCharacterTo(destinationPoint);

        float elapsed = 0f;
        while (elapsed < blackScreenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Fade(1f, 0f);

        OpeningSequenceCompleted = true;
        prompt = "";
        transitionRunning = false;
        RestoreGameplay();
    }

    void MoveCharacterTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("PakBudiDialogue: Destination Point belum diisi. Pak Budi tidak dipindahkan.");
            return;
        }

        CharacterController controller = GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        transform.SetPositionAndRotation(target.position, target.rotation);

        if (controller != null)
            controller.enabled = wasEnabled;
    }

    IEnumerator Fade(float from, float to)
    {
        fadeAlpha = from;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeAlpha = to;
    }

    void RestoreGameplay()
    {
        if (!dialogueActive)
            return;

        dialogueActive = false;
        DialogueOpen = false;
        Time.timeScale = previousTimeScale;

        if (player != null)
            player.enabled = previousPlayerEnabled;

        if (gameManager != null)
            gameManager.enabled = previousGameManagerEnabled;

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    void OnGUI()
    {
        if (transitionRunning || dialogueActive)
            DrawFadeOverlay();

        if (!dialogueActive || transitionRunning)
            return;

        DrawDialoguePanel(
            characterName,
            dialogueLines[currentLine],
            currentLine < dialogueLines.Length - 1 ? "Lanjut" : "Selesai",
            currentLine + 1,
            dialogueLines.Length
        );

        if (Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.Return)
        {
            NextLine();
            Event.current.Use();
        }
    }

    void DrawFadeOverlay()
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    void DrawDialoguePanel(string speaker, string line, string buttonText, int index, int total)
    {
        float width = Mathf.Min(760f, Screen.width - 40f);
        float height = 210f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - 35f;

        GUI.Box(new Rect(x, y, width, height), "");

        GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        nameStyle.normal.textColor = new Color(1f, 0.82f, 0.25f);

        GUIStyle lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true
        };
        lineStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x + 22f, y + 18f, width - 44f, 32f), speaker, nameStyle);
        GUI.Label(new Rect(x + 22f, y + 62f, width - 44f, 82f), line, lineStyle);

        if (GUI.Button(new Rect(x + width - 160f, y + height - 55f, 135f, 35f), buttonText))
            NextLine();

        GUI.Label(new Rect(x + 22f, y + height - 48f, 250f, 25f), index + " / " + total);
    }

    void NextLine()
    {
        if (currentLine < dialogueLines.Length - 1)
            currentLine++;
        else
            FinishDialogue();
    }

    void OnDisable()
    {
        if (dialogueActive)
        {
            StopAllCoroutines();
            transitionRunning = false;
            fadeAlpha = 0f;
            RestoreGameplay();
        }
    }
}
