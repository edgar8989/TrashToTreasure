using UnityEngine;

/// <summary>
/// NPC perkenalan sederhana yang tidak mengubah quest, inventaris, uang,
/// maupun progression game. Pasang pada root GameObject setiap NPC.
/// </summary>
public class FriendlyNPC : Interactable
{
    public static bool DialogOpen { get; private set; }

    [Header("Identitas NPC")]
    public string npcName = "Pak Agus";

    [TextArea(2, 4)]
    public string greeting = "Halo! Senang bertemu denganmu.";

    static FriendlyNPC activeNPC;
    static float previousTimeScale = 1f;

    static GUIStyle frameStyle, panelStyle, headerStyle, textStyle, buttonStyle;
    static Texture2D frameTexture, panelTexture, headerTexture, buttonTexture;

    void Reset()
    {
        prompt = "[E] Berkenalan";
    }

    void OnValidate()
    {
        prompt = "[E] Berkenalan dengan " + npcName;
    }

    public override void Interact()
    {
        if (DialogOpen) return;

        activeNPC = this;
        DialogOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (activeNPC != this || !DialogOpen) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            CloseDialog();
        }
    }

    void EnsureDialogStyles()
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

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 16;
        textStyle.wordWrap = true;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = new Color(0.22f, 0.16f, 0.12f);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonTexture;
        buttonStyle.active.background = buttonTexture;
    }

    Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void OnGUI()
    {
        if (activeNPC != this || !DialogOpen) return;

        EnsureDialogStyles();

        float width = Mathf.Min(700f, Screen.width - 40f);
        float height = 200f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - 35f;

        GUI.Box(new Rect(x, y + 12, width, height), GUIContent.none, frameStyle);
        GUI.Box(new Rect(x + 5, y + 17, width - 10, height - 10), GUIContent.none, panelStyle);

        float ribbonWidth = Mathf.Min(260f, width - 140f);
        GUI.Box(new Rect(x + 24f, y, ribbonWidth, 42f), GUIContent.none, headerStyle);
        GUI.Label(new Rect(x + 24f, y + 1f, ribbonWidth, 38f), npcName.ToUpper(), headerStyle);

        GUI.Label(new Rect(x + 26f, y + 74f, width - 52f, 58f), greeting, textStyle);

        if (GUI.Button(new Rect(x + width * 0.5f - 75f, y + height - 52f, 150f, 34f), "SELESAI", buttonStyle))
            CloseDialog();
    }

    void CloseDialog()
    {
        if (activeNPC != this) return;

        DialogOpen = false;
        activeNPC = null;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        if (activeNPC == this && DialogOpen)
        {
            DialogOpen = false;
            activeNPC = null;
            Time.timeScale = previousTimeScale;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
