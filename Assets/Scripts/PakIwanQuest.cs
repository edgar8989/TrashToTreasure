using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pak Iwan starts at the old Pak Budi conversation spot.
/// After offering the job, he fades out and moves to the shop.
/// Quest 1 tracks five newly collected trash items from GameManager.carried.
/// Attach this script to Pak Iwan's ROOT GameObject.
/// </summary>
public class PakIwanQuest : Interactable
{
    private enum QuestState
    {
        WaitingForBudi,
        OfferNotFinished,
        CollectingFiveTrash,
        Quest2Ready,
        RecyclingFiveProducts,
        Quest2Completed
    }

    [Header("Movement After Job Offer")]
    [Tooltip("Create an Empty GameObject in front of Pak Iwan's shop and assign it here.")]
    public Transform shopDestination;
    [Min(0.05f)] public float fadeDuration = 0.8f;
    [Min(0f)] public float blackScreenDuration = 0.5f;

    [Header("Interaction")]
    [Min(0.5f)] public float interactionDistance = 3f;
    public string interactionPrompt = "[E] Bicara dengan Pak Iwan";
    public string characterName = "Pak Iwan";

    [Header("Job Offer Dialogue")]
    [TextArea(2, 4)]
    public string[] initialOfferLines =
    {
        "Pak Iwan: Halo, Nak. Apakah kamu sedang mencari pekerjaan?",
        "Pemain: Iya, Pak. Saya sedang membutuhkan uang.",
        "Pak Iwan: Saya punya pekerjaan. Pertama, kumpulkan 5 sampah di sekitar lingkungan ini.",
        "Pak Iwan: Bawa sampah tersebut ke toko saya. Setelah itu, saya akan memberikan tugas daur ulang.",
        "Pemain: Baik, Pak. Saya akan mengumpulkan sampah dan kembali ke toko Bapak."
    };

    [Header("Reminder Dialogue")]
    [TextArea(2, 4)]
    public string[] reminderLines =
    {
        "Pak Iwan: Sampah yang kamu kumpulkan belum mencapai 5.",
        "Pak Iwan: Terus cari sampah di sekitar lingkungan ini, lalu kembali ke toko saya."
    };

    [Header("Delivery Dialogue")]
    [TextArea(2, 4)]
    public string[] deliveryLines =
    {
        "Pak Iwan: Wah, kamu sudah berhasil mengumpulkan 5 sampah!",
        "Pak Iwan: Baik, saya terima sampah-sampah ini. Terima kasih sudah membantu.",
        "Pemain: Sama-sama, Pak. Apakah ada pekerjaan berikutnya?",
        "Pak Iwan: Ada. Nanti saya akan memberikan tugas selanjutnya."
    };

    [Header("Quest 1")]
    [Min(1)] public int requiredTrash = 5;

    [Header("Quest 2 - Daur Ulang")]
    [Min(1)] public int requiredProducts = 5;
    [TextArea(2, 4)] public string[] quest2OfferLines =
    {
        "Pak Iwan: Bagus, kamu sudah menyelesaikan tugas pertama.",
        "Pak Iwan: Sekarang saya ingin kamu membantu mengolah sampah menjadi barang yang bermanfaat.",
        "Pak Iwan: Pilah sampah organik dan anorganik di tempat pilah, lalu masukkan ke mesin daur ulang.",
        "Pak Iwan: Pilih produk yang bisa dibuat dari setiap jenis sampah.",
        "Pak Iwan: Buatlah 5 produk daur ulang. Jenis produknya bebas.",
        "Pemain: Baik, Pak. Saya akan membuat 5 produk daur ulang.",
        "Pak Iwan: Kalau sudah selesai, bawa semua produk itu kepada saya."
    };
    [TextArea(2, 4)] public string[] quest2ReminderLines =
    {
        "Pak Iwan: Produk daur ulangmu belum mencapai 5.",
        "Pak Iwan: Pilah sampah organik atau anorganik, olah di mesin, lalu buat 5 produk."
    };
    [TextArea(2, 4)] public string[] quest2DeliveryLines =
    {
        "Pak Iwan: Wah, kamu berhasil membuat 5 produk daur ulang!",
        "Pak Iwan: Saya terima produk-produk ini. Kerja bagus!",
        "Pemain: Sama-sama, Pak. Semoga produk ini bermanfaat.",
        "Pak Iwan: Terima kasih sudah membantu mengurangi sampah di lingkungan kita."
    };

    private QuestState state = QuestState.WaitingForBudi;
    private PlayerController player;
    private GameManager gameManager;

    private readonly Dictionary<TrashType, int> trackedTrash =
        new Dictionary<TrashType, int>();

    private readonly Dictionary<TrashType, int> previousInventory =
        new Dictionary<TrashType, int>();

    private int collectedCount;
    private int recycledProductCount;
    private readonly int[] trackedQuest2Products = new int[GameManager.ProductCount];
    private string[] activeLines;

    private static PakIwanQuest activeQuest;

    static GUIStyle questFrameStyle, questPanelStyle, questHeaderStyle, questTextStyle;
    static GUIStyle questButtonStyle, questButtonAltStyle, questBadgeStyle;
    static Texture2D questFrameTexture, questPanelTexture, questHeaderTexture, questBadgeTexture;

    // Dibaca oleh PakBudiDialogue untuk membuka Quest 3 setelah dua quest selesai.
    public static bool Quest1Completed { get; private set; }
    public static bool Quest2Completed { get; private set; }

    // Pak Iwan disembunyikan saat game mulai karena ia akan menempati
    // lokasi awal Pak Budi setelah percakapan Pak Budi selesai.
    private Renderer[] npcRenderers;
    private Collider[] npcColliders;
    private bool[] rendererWasEnabled;
    private bool[] colliderWasEnabled;
    private bool npcRevealed;
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
        activeQuest = this;
        Quest1Completed = false;
        Quest2Completed = false;
        prompt = "";
        foreach (TrashType type in GameManager.AllTrashTypes)
        {
            trackedTrash[type] = 0;
            previousInventory[type] = 0;
        }
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        gameManager = GameManager.I;

        // Pak Iwan berada di lokasi A, tetapi tidak terlihat saat Pak Budi
        // masih berbicara. Script tetap aktif agar dapat menunggu giliran muncul.
        npcRenderers = GetComponentsInChildren<Renderer>(true);
        npcColliders = GetComponentsInChildren<Collider>(true);
        rendererWasEnabled = new bool[npcRenderers.Length];
        colliderWasEnabled = new bool[npcColliders.Length];

        for (int i = 0; i < npcRenderers.Length; i++)
        {
            rendererWasEnabled[i] = npcRenderers[i].enabled;
            npcRenderers[i].enabled = false;
        }

        for (int i = 0; i < npcColliders.Length; i++)
        {
            colliderWasEnabled[i] = npcColliders[i].enabled;
            npcColliders[i].enabled = false;
        }

        npcRevealed = false;
        RefreshPrompt();
    }

    void Update()
    {
        if (!npcRevealed && PakBudiDialogue.OpeningSequenceCompleted)
            RevealNpc();

        if (state == QuestState.WaitingForBudi)
        {
            RefreshPrompt();
            return;
        }

        if (state == QuestState.CollectingFiveTrash && !dialogueActive && !transitionRunning)
            TrackInventoryChanges();

        if (!dialogueActive && !transitionRunning)
            RefreshPrompt();
    }

    void RevealNpc()
    {
        npcRevealed = true;

        for (int i = 0; i < npcRenderers.Length; i++)
            npcRenderers[i].enabled = rendererWasEnabled[i];

        for (int i = 0; i < npcColliders.Length; i++)
            npcColliders[i].enabled = colliderWasEnabled[i];

        // Setelah Pak Budi selesai berpindah, Pak Iwan langsung memulai
        // percakapan tawaran pekerjaan tanpa pemain menekan tombol E.
        state = QuestState.OfferNotFinished;
        RefreshPrompt();
        BeginDialogue(initialOfferLines);
    }

    public override void Interact()
    {
        if (dialogueActive || transitionRunning)
            return;

        if (!PakBudiDialogue.OpeningSequenceCompleted)
        {
            if (GameManager.I != null)
                GameManager.I.Notify("Selesaikan percakapan dengan Pak Budi terlebih dahulu.");
            return;
        }

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null &&
            Vector3.Distance(player.transform.position, transform.position) > interactionDistance)
        {
            if (GameManager.I != null)
                GameManager.I.Notify("Dekati Pak Iwan terlebih dahulu.");
            return;
        }

        if (state == QuestState.WaitingForBudi)
        {
            state = QuestState.OfferNotFinished;
            BeginDialogue(initialOfferLines);
        }
        else if (state == QuestState.OfferNotFinished)
        {
            BeginDialogue(initialOfferLines);
        }
        else if (state == QuestState.CollectingFiveTrash)
        {
            TrackInventoryChanges();

            if (collectedCount >= requiredTrash)
                DeliverQuestTrash();
            else
                BeginDialogue(reminderLines);
        }
        else if (state == QuestState.Quest2Ready)
        {
            BeginDialogue(quest2OfferLines);
        }
        else if (state == QuestState.RecyclingFiveProducts)
        {
            if (recycledProductCount >= requiredProducts)
                DeliverQuest2Products();
            else
                BeginDialogue(quest2ReminderLines);
        }
        else if (state == QuestState.Quest2Completed)
        {
            if (GameManager.I != null)
                GameManager.I.Notify("Quest 2 sudah selesai. Terima kasih sudah mendaur ulang sampah!");
        }
    }

    void BeginDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (gameManager == null)
            gameManager = GameManager.I;

        activeLines = lines;
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
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void NextLine()
    {
        if (currentLine < activeLines.Length - 1)
        {
            currentLine++;
            return;
        }

        if (state == QuestState.OfferNotFinished && activeLines == initialOfferLines)
        {
            // Terima pekerjaan, lalu pindahkan Pak Iwan ke depan toko.
            StartCoroutine(AcceptJobAndMoveToShop());
            return;
        }

        if (activeLines == quest2OfferLines)
        {
            state = QuestState.RecyclingFiveProducts;
            recycledProductCount = 0;
            System.Array.Clear(trackedQuest2Products, 0, trackedQuest2Products.Length);
            CloseDialogue();
            if (gameManager != null)
                gameManager.Notify("Quest 2 dimulai: daur ulang sampah menjadi 5 produk.");
            return;
        }

        if (activeLines == quest2DeliveryLines)
        {
            if (RemoveQuest2Products())
            {
                state = QuestState.Quest2Completed;
                Quest2Completed = true;
                if (gameManager != null)
                    gameManager.Notify("Quest 2 selesai! Lima produk daur ulang sudah diserahkan kepada Pak Iwan.");
            }
            else if (gameManager != null)
            {
                gameManager.Notify("Penyerahan produk gagal. Pastikan 5 produk masih ada di tas.");
            }
        }

        if (activeLines == deliveryLines)
        {
            // Sampah baru benar-benar dihapus setelah seluruh dialog penyerahan
            // selesai. Jika dialog ditutup sebelum waktunya, inventaris tetap aman.
            if (RemoveDeliveredQuestTrash())
            {
                state = QuestState.Quest2Ready;
                Quest1Completed = true;
                if (gameManager != null)
                    gameManager.Notify("Quest 1 selesai! Bicara lagi dengan Pak Iwan untuk menerima Quest 2.");
            }
            else
            {
                if (gameManager != null)
                    gameManager.Notify("Penyerahan gagal. Pastikan 5 sampah masih ada di tas, lalu bicara lagi dengan Pak Iwan.");
            }
        }

        CloseDialogue();
    }

    IEnumerator AcceptJobAndMoveToShop()
    {
        transitionRunning = true;
        yield return Fade(0f, 1f);

        MoveCharacterToShop();

        float elapsed = 0f;
        while (elapsed < blackScreenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Aktifkan status quest saat layar masih hitam, sebelum pemain melihat toko.
        state = QuestState.CollectingFiveTrash;
        collectedCount = 0;
        foreach (TrashType type in GameManager.AllTrashTypes)
        {
            trackedTrash[type] = 0;
            previousInventory[type] = gameManager != null
                ? gameManager.CountCarried(type)
                : 0;
        }

        yield return Fade(1f, 0f);
        transitionRunning = false;
        CloseDialogue();

        if (gameManager != null)
            gameManager.Notify("Quest 1 dimulai: kumpulkan 5 sampah, lalu bawa ke toko Pak Iwan.");
    }

    void MoveCharacterToShop()
    {
        if (shopDestination == null)
        {
            Debug.LogWarning("PakIwanQuest: Shop Destination belum diisi. Pak Iwan tidak dipindahkan.");
            return;
        }

        CharacterController controller = GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        transform.SetPositionAndRotation(shopDestination.position, shopDestination.rotation);

        if (controller != null)
            controller.enabled = wasEnabled;
    }

    void TrackInventoryChanges()
    {
        if (gameManager == null)
            gameManager = GameManager.I;

        if (gameManager == null || gameManager.carried == null)
            return;

        foreach (TrashType type in GameManager.AllTrashTypes)
        {
            int current = gameManager.CountCarried(type);
            int previous = previousInventory[type];
            int difference = current - previous;

            if (difference > 0)
            {
                int add = Mathf.Min(difference, requiredTrash - collectedCount);
                trackedTrash[type] += add;
                collectedCount += add;
            }
            else if (difference < 0)
            {
                int removed = Mathf.Min(-difference, trackedTrash[type]);
                trackedTrash[type] -= removed;
                collectedCount -= removed;
            }

            previousInventory[type] = current;
        }

        collectedCount = Mathf.Clamp(collectedCount, 0, requiredTrash);
    }

    void DeliverQuestTrash()
    {
        if (gameManager == null)
            gameManager = GameManager.I;

        if (gameManager == null || gameManager.carried == null)
            return;

        // Pastikan kelima sampah yang dihitung quest masih ada di tas pemain.
        int availableTrackedTrash = 0;
        foreach (TrashType type in GameManager.AllTrashTypes)
            availableTrackedTrash += Mathf.Min(trackedTrash[type], gameManager.CountCarried(type));

        if (availableTrackedTrash < requiredTrash)
        {
            TrackInventoryChanges();
            BeginDialogue(reminderLines);
            return;
        }

        // Jangan hapus inventaris di sini. Penghapusan dilakukan setelah dialog
        // penyerahan selesai pada NextLine().
        BeginDialogue(deliveryLines);
    }

    bool RemoveDeliveredQuestTrash()
    {
        if (gameManager == null)
            gameManager = GameManager.I;

        if (gameManager == null || gameManager.carried == null)
            return false;

        int removedTotal = 0;
        foreach (TrashType type in GameManager.AllTrashTypes)
        {
            int amountToRemove = trackedTrash[type];
            for (int i = gameManager.carried.Count - 1; i >= 0 && amountToRemove > 0; i--)
            {
                if (gameManager.carried[i] != type)
                    continue;

                gameManager.carried.RemoveAt(i);
                amountToRemove--;
                removedTotal++;
            }

            trackedTrash[type] = 0;
            previousInventory[type] = gameManager.CountCarried(type);
        }

        collectedCount = 0;
        if (removedTotal < requiredTrash)
        {
            // Perlindungan tambahan jika inventaris berubah secara tidak terduga.
            state = QuestState.CollectingFiveTrash;
            TrackInventoryChanges();
            if (gameManager != null)
                gameManager.Notify("Sampah belum lengkap. Silakan kumpulkan kembali hingga 5.");
            return false;
        }

        return true;
    }

    // Dipanggil oleh mesin hanya setelah proses daur ulang berhasil selesai.
    public static void RegisterRecycledProducts(int productIndex, int amount)
    {
        if (activeQuest == null || amount <= 0 ||
            activeQuest.state != QuestState.RecyclingFiveProducts ||
            productIndex < 0 || productIndex >= GameManager.ProductCount)
            return;

        int remaining = Mathf.Max(0, activeQuest.requiredProducts - activeQuest.recycledProductCount);
        int credited = Mathf.Min(amount, remaining);
        if (credited <= 0) return;

        activeQuest.trackedQuest2Products[productIndex] += credited;
        activeQuest.recycledProductCount += credited;
        activeQuest.RefreshPrompt();

        if (GameManager.I != null)
            GameManager.I.Notify("Produk daur ulang bertambah: " + activeQuest.recycledProductCount + "/" + activeQuest.requiredProducts);
    }

    void DeliverQuest2Products()
    {
        if (gameManager == null) gameManager = GameManager.I;
        if (gameManager == null || gameManager.products == null) return;

        int availableTracked = 0;
        for (int i = 0; i < GameManager.ProductCount; i++)
            availableTracked += Mathf.Min(trackedQuest2Products[i], gameManager.products[i]);

        if (availableTracked < requiredProducts)
        {
            BeginDialogue(quest2ReminderLines);
            if (gameManager != null)
                gameManager.Notify("Produk hasil daur ulang tidak lengkap di tas. Buat kembali produk yang kurang.");
            return;
        }

        BeginDialogue(quest2DeliveryLines);
    }

    bool RemoveQuest2Products()
    {
        if (gameManager == null) gameManager = GameManager.I;
        if (gameManager == null || gameManager.products == null) return false;

        int totalRemoved = 0;
        for (int i = 0; i < GameManager.ProductCount; i++)
        {
            int amountToRemove = Mathf.Min(trackedQuest2Products[i], gameManager.products[i]);
            gameManager.products[i] -= amountToRemove;
            totalRemoved += amountToRemove;
            trackedQuest2Products[i] = 0;
        }

        if (totalRemoved < requiredProducts) return false;
        return true;
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

    void CloseDialogue()
    {
        dialogueActive = false;
        Time.timeScale = previousTimeScale;

        if (player != null)
            player.enabled = previousPlayerEnabled;

        if (gameManager != null)
            gameManager.enabled = previousGameManagerEnabled;

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        RefreshPrompt();
    }

    void RefreshPrompt()
    {
        if (!PakBudiDialogue.OpeningSequenceCompleted)
        {
            prompt = "";
            return;
        }

        switch (state)
        {
            case QuestState.WaitingForBudi:
            case QuestState.OfferNotFinished:
                prompt = interactionPrompt;
                break;

            case QuestState.CollectingFiveTrash:
                prompt = collectedCount >= requiredTrash
                    ? "[E] Serahkan 5 sampah kepada Pak Iwan"
                    : "[E] Bicara dengan Pak Iwan (" + collectedCount + "/" + requiredTrash + ")";
                break;

            case QuestState.Quest2Ready:
                prompt = "[E] Terima Quest 2 dari Pak Iwan";
                break;

            case QuestState.RecyclingFiveProducts:
                prompt = recycledProductCount >= requiredProducts
                    ? "[E] Serahkan 5 produk daur ulang kepada Pak Iwan"
                    : "[E] Bicara dengan Pak Iwan (" + recycledProductCount + "/" + requiredProducts + " produk)";
                break;

            case QuestState.Quest2Completed:
                prompt = "[E] Pak Iwan";
                break;
        }
    }

    void OnGUI()
    {
        if (transitionRunning)
        {
            DrawFadeOverlay();
            return;
        }

        if (!dialogueActive && state == QuestState.CollectingFiveTrash)
        {
            DrawQuestTracker("QUEST 1", "Kumpulkan sampah", collectedCount, requiredTrash);
        }
        else if (!dialogueActive && state == QuestState.RecyclingFiveProducts)
        {
            DrawQuestTracker("QUEST 2", "Produk dibuat", recycledProductCount, requiredProducts);
        }
        else if (!dialogueActive && state == QuestState.Quest2Completed &&
                 !PakBudiDialogue.Quest3Completed)
        {
            EnsureQuestStyles();
            float quest3Width = Mathf.Min(380f, Screen.width - 36f);
            float quest3X = Screen.width - quest3Width - 18f;
            float quest3Y = 18f;
            GUI.Box(new Rect(quest3X, quest3Y + 10f, quest3Width, 116f),
                GUIContent.none, questFrameStyle);

            GUI.Box(new Rect(quest3X + 4f, quest3Y + 14f,
                quest3Width - 8f, 108f),
                GUIContent.none, questPanelStyle);

            GUI.Box(new Rect(quest3X + 18f, quest3Y,
                quest3Width - 70f, 42f),
                GUIContent.none, questHeaderStyle);

            GUI.Label(new Rect(quest3X + 18f, quest3Y + 1f,
                quest3Width - 70f, 38f),
                "QUEST 3", questHeaderStyle);

            GUI.Label(new Rect(quest3X + 24f, quest3Y + 58f,
                quest3Width - 48f, 42f),
                "Kumpulkan uang sebanyak Rp250.000", questTextStyle);
        }

        if (!dialogueActive || activeLines == null)
            return;

        EnsureQuestStyles();

        float panelWidth = Mathf.Min(780f, Screen.width - 40f);
        float panelHeight = 222f;
        float x = (Screen.width - panelWidth) * 0.5f;
        float y = Screen.height - panelHeight - 35f;

        GUI.Box(new Rect(x, y + 12, panelWidth, panelHeight), GUIContent.none, questFrameStyle);
        GUI.Box(new Rect(x + 5, y + 17, panelWidth - 10, panelHeight - 10), GUIContent.none, questPanelStyle);

        float ribbonWidth = Mathf.Min(280f, panelWidth - 140f);
        float ribbonX = x + 24f;
        GUI.Box(new Rect(ribbonX, y, ribbonWidth, 42f), GUIContent.none, questHeaderStyle);
        GUI.Label(new Rect(ribbonX, y + 1, ribbonWidth, 38f), characterName.ToUpper(), questHeaderStyle);

        GUI.Label(new Rect(x + 24f, y + 64f, panelWidth - 48f, 86f), activeLines[currentLine], questTextStyle);

        string buttonText = currentLine < activeLines.Length - 1 ? "LANJUT" : "SELESAI";
        if (GUI.Button(new Rect(x + panelWidth - 178f, y + panelHeight - 56f, 150f, 36f), buttonText, questButtonStyle))
            NextLine();

        GUI.Box(new Rect(x + 26f, y + panelHeight - 55f, 82f, 32f), GUIContent.none, questBadgeStyle);
        GUI.Label(new Rect(x + 26f, y + panelHeight - 55f, 82f, 32f), (currentLine + 1) + " / " + activeLines.Length, questBadgeStyle);

        if (Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.Return)
        {
            NextLine();
            Event.current.Use();
        }
    }

    void EnsureQuestStyles()
    {
        if (questFrameStyle != null) return;

        questFrameTexture = CreateQuestTexture(new Color(0.16f, 0.38f, 0.71f, 1f));
        questPanelTexture = CreateQuestTexture(new Color(0.98f, 0.93f, 0.83f, 0.99f));
        questHeaderTexture = CreateQuestTexture(new Color(0.15f, 0.46f, 0.84f, 1f));
        questBadgeTexture = CreateQuestTexture(new Color(0.43f, 0.78f, 0.18f, 1f));

        questFrameStyle = new GUIStyle(GUI.skin.box);
        questFrameStyle.normal.background = questFrameTexture;

        questPanelStyle = new GUIStyle(GUI.skin.box);
        questPanelStyle.normal.background = questPanelTexture;

        questHeaderStyle = new GUIStyle(GUI.skin.label);
        questHeaderStyle.fontSize = 17;
        questHeaderStyle.fontStyle = FontStyle.Bold;
        questHeaderStyle.alignment = TextAnchor.MiddleCenter;
        questHeaderStyle.normal.textColor = Color.white;
        questHeaderStyle.normal.background = questHeaderTexture;
        questHeaderStyle.hover.background = questHeaderTexture;
        questHeaderStyle.active.background = questHeaderTexture;

        questTextStyle = new GUIStyle(GUI.skin.label);
        questTextStyle.fontSize = 16;
        questTextStyle.wordWrap = true;
        questTextStyle.normal.textColor = new Color(0.22f, 0.16f, 0.12f);

        questButtonStyle = new GUIStyle(GUI.skin.button);
        questButtonStyle.fontSize = 14;
        questButtonStyle.fontStyle = FontStyle.Bold;
        questButtonStyle.alignment = TextAnchor.MiddleCenter;
        questButtonStyle.normal.textColor = Color.white;
        questButtonStyle.normal.background = questBadgeTexture;
        questButtonStyle.hover.background = questBadgeTexture;
        questButtonStyle.active.background = questBadgeTexture;

        questButtonAltStyle = new GUIStyle(questButtonStyle);
        questButtonAltStyle.normal.background = questHeaderTexture;
        questButtonAltStyle.hover.background = questHeaderTexture;
        questButtonAltStyle.active.background = questHeaderTexture;

        questBadgeStyle = new GUIStyle(GUI.skin.box);
        questBadgeStyle.fontSize = 13;
        questBadgeStyle.fontStyle = FontStyle.Bold;
        questBadgeStyle.alignment = TextAnchor.MiddleCenter;
        questBadgeStyle.normal.textColor = Color.white;
        questBadgeStyle.normal.background = questBadgeTexture;
    }

    Texture2D CreateQuestTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void DrawQuestTracker(string title, string description, int current, int target)
    {
        EnsureQuestStyles();
        float width = Mathf.Min(380f, Screen.width - 36f);
        float x = Screen.width - width - 18f;
        float y = 18f;

        GUI.Box(new Rect(x, y + 10f, width, 116f), GUIContent.none, questFrameStyle);
        GUI.Box(new Rect(x + 4f, y + 14f, width - 8f, 108f), GUIContent.none, questPanelStyle);
        GUI.Box(new Rect(x + 18f, y, width - 70f, 42f), GUIContent.none, questHeaderStyle);
        GUI.Label(new Rect(x + 18f, y + 1f, width - 70f, 38f), title, questHeaderStyle);

        GUI.Label(new Rect(x + 24f, y + 58f, width - 150f, 34f), description + ":", questTextStyle);
        GUI.Box(new Rect(x + width - 112f, y + 56f, 82f, 34f), GUIContent.none, questBadgeStyle);
        GUI.Label(new Rect(x + width - 112f, y + 56f, 82f, 34f), current + "/" + target, questBadgeStyle);
    }

    void DrawFadeOverlay()
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    void OnDestroy()
    {
        if (activeQuest == this) activeQuest = null;
    }

    void OnDisable()
    {
        if (dialogueActive)
            CloseDialogue();

        if (transitionRunning)
        {
            StopAllCoroutines();
            transitionRunning = false;
            fadeAlpha = 0f;
            CloseDialogue();
        }
    }
}
