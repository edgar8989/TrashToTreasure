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
        "Pak Budi: Selamat pagi. Saya ingin mengingatkan bahwa SPP anakmu belum dibayar.",
        "Pemain: Iya, Pak. Saya belum memiliki cukup uang untuk membayarnya.",
        "Pak Budi: Kamu bisa mengumpulkan uang dengan memilah sampah dan membuat produk daur ulang.",
        "Pemain: Baik, Pak. Saya akan menyelesaikan tugas yang diberikan.",
        "Pak Budi: Selesaikan tugas dari Pak Iwan terlebih dahulu. Setelah itu, temui saya untuk membayar SPP sebesar Rp250.000."
    };

    [Header("Move Pak Budi After Dialogue")]
    [Tooltip("Create an Empty GameObject at Pak Budi's later location and assign it here.")]
    public Transform destinationPoint;
    [Min(0.05f)] public float fadeDuration = 0.8f;
    [Min(0f)] public float blackScreenDuration = 0.5f;

    private PlayerController player;
    private GameManager gameManager;
    private int currentLine;
    private string[] activeLines;
    private bool dialogueActive;
    private bool openingDialogueActive;
    private bool paymentPending;
    private bool transitionRunning;
    private float fadeAlpha;

    private bool previousPlayerEnabled;
    private bool previousGameManagerEnabled;
    private float previousTimeScale;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    static GUIStyle frameStyle, panelStyle, headerStyle, lineStyle, buttonStyle, badgeStyle;
    static Texture2D frameTexture, panelTexture, headerTexture, buttonTexture;

    [Header("Quest 3 - Pembayaran SPP")]
    public int sppPayment = 250000;
    public static bool Quest3Completed { get; private set; }

    [TextArea(2, 4)]
    public string[] prerequisiteLines =
    {
        "Pak Budi: Sebelum membayar SPP, kamu harus menyelesaikan Quest 1 dan Quest 2 dari Pak Iwan terlebih dahulu.",
        "Pak Budi: Setelah kedua tugas selesai, kembali lagi ke sekolah untuk membayar SPP.",
        "Pemain: Baik, Pak. Saya akan menyelesaikan kedua tugas itu terlebih dahulu."
    };

    [TextArea(2, 4)]
    public string[] insufficientMoneyLines =
    {
        "Pak Budi: Tugas dari Pak Iwan sudah selesai. Sekarang kita perlu menyelesaikan pembayaran SPP.",
        "Pak Budi: Biaya SPP adalah Rp250.000, tetapi uangmu saat ini belum mencukupi.",
        "Pemain: Baik, Pak. Saya akan mengumpulkan uang yang masih kurang lalu kembali lagi."
    };

    [TextArea(2, 4)]
    public string[] paymentLines =
    {
        "Pak Budi: Selamat, kamu sudah menyelesaikan dua tugas dari Pak Iwan.",
        "Pak Budi: Sekarang apakah kamu siap membayar SPP sebesar Rp250.000?",
        "Pemain: Iya, Pak. Saya menyerahkan uang SPP sebesar Rp250.000.",
        "Pak Budi: Baik, uangnya saya terima. Saya akan mencatat pembayaran SPP-mu."
    };

    [TextArea(2, 4)]
    public string[] paymentSuccessLines =
    {
        "Pak Budi: Pembayaran SPP sebesar Rp250.000 sudah berhasil dan tercatat.",
        "Pak Budi: Terima kasih sudah menyelesaikan kewajibanmu. Semua quest telah selesai.",
        "Pemain: Terima kasih, Pak. Saya senang sudah menyelesaikan semua tugas."
    };

    [TextArea(2, 4)]
    public string[] alreadyPaidLines =
    {
        "Pak Budi: Pembayaran SPP-mu sudah tercatat. Terima kasih.",
        "Pemain: Sama-sama, Pak."
    };

    void Awake()
    {
        prompt = "";
        OpeningSequenceCompleted = false;
        Quest3Completed = false;

        // Unity dapat mempertahankan array 8 baris dari Inspector versi lama.
        // Potong menjadi 5 agar percakapan awal selalu berisi tepat 5 baris.
        if (dialogueLines != null && dialogueLines.Length > 5)
            System.Array.Resize(ref dialogueLines, 5);
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

    void Update()
    {
        if (OpeningSequenceCompleted && !DialogueOpen)
            RefreshPrompt();
    }

    // Menjaga teks interaksi Pak Budi sesuai progres Quest 3.
    void RefreshPrompt()
    {
        if (!OpeningSequenceCompleted)
        {
            prompt = "";
            return;
        }

        if (Quest3Completed)
            prompt = "[E] SPP sudah dibayar";
        else if (!PakIwanQuest.Quest1Completed || !PakIwanQuest.Quest2Completed)
            prompt = "[E] Bicara dengan Pak Budi";
        else
            prompt = "[E] Bayar SPP kepada Pak Budi";
    }

    public override void Interact()
    {
        if (DialogueOpen)
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

        if (!OpeningSequenceCompleted)
        {
            BeginDialogue(dialogueLines, true, false);
            return;
        }

        if (Quest3Completed)
        {
            BeginDialogue(alreadyPaidLines, false, false);
            return;
        }

        if (!PakIwanQuest.Quest1Completed || !PakIwanQuest.Quest2Completed)
        {
            BeginDialogue(prerequisiteLines, false, false);
            return;
        }

        if (gameManager == null)
            gameManager = GameManager.I;

        if (gameManager == null || gameManager.money < sppPayment)
        {
            BeginDialogue(insufficientMoneyLines, false, false);
            return;
        }

        BeginDialogue(paymentLines, false, true);
    }

    void BeginDialogue()
    {
        BeginDialogue(dialogueLines, true, false);
    }

    void BeginDialogue(string[] lines, bool isOpeningDialogue, bool payOnFinish)
    {
        if (DialogueOpen || lines == null || lines.Length == 0)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        if (gameManager == null)
            gameManager = GameManager.I;

        currentLine = 0;
        activeLines = lines;
        openingDialogueActive = isOpeningDialogue;
        paymentPending = payOnFinish;
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
        RefreshPrompt();
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
        activeLines = null;
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
        if (PauseMenuUI.IsPaused)
        return;

        if (transitionRunning || dialogueActive)
            DrawFadeOverlay();

        if (!dialogueActive || transitionRunning)
            return;

        DrawDialoguePanel(
            characterName,
            activeLines[currentLine],
            currentLine < activeLines.Length - 1 ? "Lanjut" : "Selesai",
            currentLine + 1,
            activeLines.Length
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

    void EnsureDialogueStyles()
    {
        if (frameStyle != null) return;

        frameTexture = MakeTexture(new Color(0.16f, 0.38f, 0.71f, 1f));
        panelTexture = MakeTexture(new Color(0.98f, 0.93f, 0.83f, 0.98f));
        headerTexture = MakeTexture(new Color(0.15f, 0.46f, 0.84f, 1f));
        buttonTexture = MakeTexture(new Color(0.43f, 0.78f, 0.18f, 1f));

        frameStyle = new GUIStyle(GUI.skin.box);
        frameStyle.normal.background = frameTexture;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 17;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = Color.white;
        headerStyle.normal.background = headerTexture;

        lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.fontSize = 16;
        lineStyle.wordWrap = true;
        lineStyle.normal.textColor = new Color(0.22f, 0.16f, 0.12f);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonTexture;
        buttonStyle.active.background = buttonTexture;

        badgeStyle = new GUIStyle(GUI.skin.box);
        badgeStyle.fontSize = 13;
        badgeStyle.fontStyle = FontStyle.Bold;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.normal.textColor = Color.white;
        badgeStyle.normal.background = buttonTexture;
    }

    Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void DrawDialoguePanel(string speaker, string line, string buttonText, int index, int total)
    {
        EnsureDialogueStyles();

        float width = Mathf.Min(780f, Screen.width - 40f);
        float height = 222f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - 35f;

        GUI.Box(new Rect(x, y + 12, width, height), GUIContent.none, frameStyle);
        GUI.Box(new Rect(x + 5, y + 17, width - 10, height - 10), GUIContent.none, panelStyle);

        float ribbonWidth = Mathf.Min(260f, width - 140f);
        GUI.Box(new Rect(x + 24f, y, ribbonWidth, 42f), GUIContent.none, headerStyle);
        GUI.Label(new Rect(x + 24f, y + 1f, ribbonWidth, 38f), speaker.ToUpper(), headerStyle);

        GUI.Label(new Rect(x + 26f, y + 66f, width - 52f, 84f), line, lineStyle);

        if (GUI.Button(new Rect(x + width - 176f, y + height - 56f, 148f, 36f), buttonText.ToUpper(), buttonStyle))
            NextLine();

        GUI.Box(new Rect(x + 28f, y + height - 56f, 82f, 32f), GUIContent.none, badgeStyle);
        GUI.Label(new Rect(x + 28f, y + height - 56f, 82f, 32f), index + " / " + total, badgeStyle);
    }

    void NextLine()
    {
        if (activeLines == null || activeLines.Length == 0)
            return;

        if (currentLine < activeLines.Length - 1)
        {
            currentLine++;
            return;
        }

        if (openingDialogueActive)
        {
            FinishDialogue();
            return;
        }

        if (paymentPending)
        {
            if (gameManager == null)
                gameManager = GameManager.I;

            if (gameManager == null || gameManager.money < sppPayment)
            {
                paymentPending = false;
                activeLines = insufficientMoneyLines;
                currentLine = 0;
                return;
            }

            gameManager.money -= sppPayment;
            Quest3Completed = true;
            paymentPending = false;
            RefreshPrompt();
            if (gameManager != null)
                gameManager.Notify("Pembayaran SPP berhasil.");

            // Tampilkan dialog konfirmasi penerimaan setelah uang dibayarkan.
            activeLines = paymentSuccessLines;
            currentLine = 0;
            return;
        }

        CloseFollowUpDialogue();
    }

    void CloseFollowUpDialogue()
    {
        if (!dialogueActive)
            return;

        dialogueActive = false;
        DialogueOpen = false;
        activeLines = null;
        Time.timeScale = previousTimeScale;

        if (player != null)
            player.enabled = previousPlayerEnabled;
        if (gameManager != null)
            gameManager.enabled = previousGameManagerEnabled;

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
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
