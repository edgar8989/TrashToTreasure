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

    void OnGUI()
    {
        if (activeNPC != this || !DialogOpen) return;

        float width = Mathf.Min(620f, Screen.width - 40f);
        float height = 190f;
        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - height - 35f,
            width,
            height
        );

        GUI.Box(panel, "PERKENALAN");

        GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        GUIStyle greetingStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            wordWrap = true
        };

        GUI.Label(new Rect(panel.x + 20, panel.y + 35, width - 40, 35), npcName, nameStyle);
        GUI.Label(new Rect(panel.x + 25, panel.y + 78, width - 50, 55), greeting, greetingStyle);

        if (GUI.Button(new Rect(panel.x + width * 0.32f, panel.y + 140, width * 0.36f, 32), "Senang bertemu denganmu! (E)"))
        {
            CloseDialog();
        }
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
