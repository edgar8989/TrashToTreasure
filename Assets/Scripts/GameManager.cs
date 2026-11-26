using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public const int TrashTypeCount = 7, ProductCount = 4;

    [Header("Status")]
    public int money = 20000;
    [Range(0, 100)] public float hunger = 100f;
    public float hungerDropPerSecond = 0.15f;

    [Header("Tas")]
    public int inventoryCapacity = 5;
    public List<TrashType> carried = new List<TrashType>();
    // Inventaris makanan terpisah dari kapasitas tas sampah/produk.
    // Setiap angka menyimpan indeks jenis makanan.
    public List<int> foods = new List<int>();
    public int foodCapacity = 5;
    [HideInInspector] public int bread; // Data lama: dikonversi menjadi roti saat Awake.
    public int[] products = new int[ProductCount];

    [Header("Tempat Pilah")]
    public int[] sorted = new int[TrashTypeCount];

    [Header("Produk Jatuh")]
    public GameObject[] productPrefabs = new GameObject[ProductCount];
    public GameObject[] trashPrefabs = new GameObject[TrashTypeCount];
    public Transform dropOrigin;
    public float dropForwardDistance = 1.25f;

    [HideInInspector] public string prompt = "";
    public int totalSold;

    public static readonly string[] TrashNames =
    { "Botol Plastik", "Kertas", "Kaleng", "Sisa Makanan", "Daun Kering", "Gelas Plastik", "Kardus" };
    public static readonly string[] ProductNames =
    { "Pot Tanaman", "Buku Catatan", "Wadah Logam", "Kompos" };

    // Indeks makanan dipakai langsung oleh inventaris dan sistem konsumsi.
    public const int FoodCount = 5;
    public static readonly string[] FoodNames =
    { "Roti", "Pisang", "Apel", "Nasi Goreng", "Susu" };
    public static readonly int[] FoodPrices = { 5000, 3000, 4000, 10000, 7000 };
    public static readonly float[] FoodHungerRestore = { 20f, 15f, 25f, 40f, 30f };
    public static readonly TrashType[] AllTrashTypes =
    { TrashType.PlasticBottle, TrashType.PlasticCup, TrashType.Cardboard, TrashType.Paper,
      TrashType.MetalCan, TrashType.FoodWaste, TrashType.DryLeaves };

    string message = "";
    float messageTimer;
    int reservedSlots;

    static GUIStyle hudFrameStyle;
    static GUIStyle hudPanelStyle;
    static GUIStyle hudHeaderStyle;
    static GUIStyle hudRowStyle;
    static GUIStyle hudSmallStyle;
    static GUIStyle hudBadgeStyle;
    static GUIStyle hudPromptStyle;
    static GUIStyle hudMessageStyle;
    static Texture2D hudFrameTexture;
    static Texture2D hudPanelTexture;
    static Texture2D hudHeaderTexture;
    static Texture2D hudDividerTexture;
    static Texture2D hudBadgeTexture;

    // Status kematian pemain
    public bool IsDead { get; private set; }

    public int CarriedCount => carried == null ? 0 : carried.Count;
    public int CarriedProductCount
    {
        get { int n = 0; if (products != null) foreach (int x in products) n += Mathf.Max(0, x); return n; }
    }
    public int TotalInventoryCount => CarriedCount + CarriedProductCount;
    public int FoodInventoryCount => foods == null ? 0 : foods.Count;

    public string FoodInventorySummary()
    {
        if (foods == null || foods.Count == 0) return "Kosong";
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        for (int i = 0; i < foods.Count; i++)
        {
            int foodIndex = foods[i];
            string itemName = foodIndex >= 0 && foodIndex < FoodCount
                ? FoodNames[foodIndex] : "Item tidak valid";
            if (i > 0) result.Append(", ");
            result.Append(i).Append(": ").Append(itemName);
        }
        return result.ToString();
    }
    public int FreeInventorySpace => Mathf.Max(0, inventoryCapacity - TotalInventoryCount - reservedSlots);

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // Batasi laju hunger agar nilai lama yang tersimpan di Inspector
        // (misalnya 0.5) ikut menggunakan kecepatan baru yang lebih lambat.
        hungerDropPerSecond = Mathf.Clamp(hungerDropPerSecond, 0f, 0.15f);
        if (carried == null) carried = new List<TrashType>();
        if (foods == null) foods = new List<int>();
        // Migrasi stok roti dari versi lama agar data pemain tidak hilang.
        for (int i = 0; i < bread && foods.Count < foodCapacity; i++)
            foods.Add(0);
        bread = 0;
        System.Array.Resize(ref sorted, TrashTypeCount);
        System.Array.Resize(ref products, ProductCount);
        if (trashPrefabs == null) trashPrefabs = new GameObject[TrashTypeCount];
        System.Array.Resize(ref trashPrefabs, TrashTypeCount);
        if (productPrefabs == null) productPrefabs = new GameObject[ProductCount];
        System.Array.Resize(ref productPrefabs, ProductCount);
    }

    void Update()
    {
        // Jangan jalankan gameplay lagi setelah karakter mati.
        if (IsDead) return;

        // Hunger hanya berkurang saat pemain sedang bermain aktif.
        // Menu tempat pilah/mesin dan dialog NPC tidak menghabiskan hunger.
        bool isInteracting =
            WorldObject.MenuOpen ||
            VehicleController.CargoMenuOpen ||
            FriendlyNPC.DialogOpen ||
            Time.timeScale <= 0f;

        if (!isInteracting)
            hunger = Mathf.Max(0, hunger - hungerDropPerSecond * Time.deltaTime);

        // Karakter langsung mati ketika hunger mencapai 0.
        if (hunger <= 0f)
        {
            Die();
            return;
        }

        if (messageTimer > 0) messageTimer -= Time.deltaTime;

        if (WorldObject.MenuOpen || VehicleController.CargoMenuOpen || VehicleController.IsPlayerDriving)
            return;

        if (Input.GetKeyDown(KeyCode.F)) Eat();
        if (Input.GetKeyDown(KeyCode.G)) Discard();
    }

    void Die()
    {
        if (IsDead) return;

        IsDead = true;
        hunger = 0f;

        // Menghentikan seluruh gameplay: gerakan, fisika, animasi, dan timer.
        Time.timeScale = 0f;

        // Tampilkan cursor agar pemain tidak terkunci setelah game over.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("KARAKTER MATI: Hunger mencapai 0.");
    }

    void DrawDeathScreen()
    {
        // Latar gelap transparan.
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        GUI.color = oldColor;

        // Judul kematian.
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = Mathf.Clamp(Screen.height / 12, 28, 64);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.red;

        GUI.Label(
            new Rect(0, Screen.height * 0.36f, Screen.width, 90),
            "KARAKTER MATI",
            titleStyle
        );

        // Keterangan.
        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.fontSize = Mathf.Clamp(Screen.height / 35, 14, 24);
        subtitleStyle.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height * 0.49f, Screen.width, 50),
            "Kamu kehabisan energi karena Hunger mencapai 0.",
            subtitleStyle
        );
    }

    public bool TryAddCarried(TrashType type, int amount = 1)
    {
        if (!IsValidTrashType(type) || amount <= 0 || amount > FreeInventorySpace) return false;
        for (int i = 0; i < amount; i++) carried.Add(type);
        return true;
    }

    public int CountCarried(TrashType type)
    {
        int n = 0;
        if (carried != null) foreach (TrashType t in carried) if (t == type) n++;
        return n;
    }

    public int CountCategory(int[] stock, WasteCategory cat)
    {
        int n = 0;
        if (stock == null) return 0;
        for (int i = 0; i < Mathf.Min(stock.Length, TrashTypeCount); i++)
            if (GetCategory((TrashType)i) == cat) n += stock[i];
        return n;
    }

    public int GetProductIndex(TrashType type)
    {
        switch (type)
        {
            case TrashType.PlasticBottle: case TrashType.PlasticCup: return 0;
            case TrashType.Cardboard: case TrashType.Paper: return 1;
            case TrashType.MetalCan: return 2;
            case TrashType.FoodWaste: case TrashType.DryLeaves: return 3;
            default: return -1;
        }
    }

    public bool TryReserveProductSlots(int n)
    {
        if (n <= 0 || n > FreeInventorySpace) return false;
        reservedSlots += n; return true;
    }
    public void ReleaseReservedProductSlots(int n) { reservedSlots = Mathf.Max(0, reservedSlots - n); }
    public bool CompleteReservedProductProduction(int index, int n)
    {
        if (index < 0 || index >= ProductCount || n <= 0) { ReleaseReservedProductSlots(n); return false; }
        ReleaseReservedProductSlots(n); products[index] += n; return true;
    }

    public void Notify(string text) { message = text; messageTimer = 3f; Debug.Log(text); }

    public bool TryBuyFood(int foodIndex)
    {
        if (foodIndex < 0 || foodIndex >= FoodCount) return false;
        if (FoodInventoryCount >= foodCapacity)
        { Notify("Inventaris makanan penuh!"); return false; }
        int price = FoodPrices[foodIndex];
        if (money < price)
        { Notify("Uang tidak cukup untuk membeli " + FoodNames[foodIndex] + "."); return false; }

        money -= price;
        foods.Add(foodIndex);
        Notify("Membeli " + FoodNames[foodIndex] + ". Stok makanan: " + FoodInventoryCount + "/" + foodCapacity);
        return true;
    }

    void Eat()
    {
        if (foods == null || foods.Count == 0)
        { Notify("Inventaris makanan kosong. Kunjungi toko makanan."); return; }
        if (hunger >= 100)
        { Notify("Hunger sudah penuh!"); return; }

        // Konsumsi selalu mengambil item paling awal (index 0).
        int foodIndex = foods[0];
        foods.RemoveAt(0);
        if (foodIndex < 0 || foodIndex >= FoodCount)
        { Notify("Data makanan tidak valid dan telah dihapus dari inventaris."); return; }

        hunger = Mathf.Min(100f, hunger + FoodHungerRestore[foodIndex]);
        Notify("Mengonsumsi " + FoodNames[foodIndex] + ". Hunger: " + Mathf.CeilToInt(hunger) +
            "/100 | Sisa makanan: " + FoodInventoryCount + "/" + foodCapacity);
    }

    Transform DropOrigin()
    {
        if (dropOrigin != null) return dropOrigin;
        PlayerController p = FindFirstObjectByType<PlayerController>();
        return p != null ? p.transform : null;
    }

    void Discard()
    {
        if (CarriedProductCount > 0) DiscardProduct();
        else DiscardTrash();
    }

    void DiscardProduct()
    {
        Transform origin = DropOrigin();
        if (origin == null) { Notify("Posisi pemain tidak ditemukan."); return; }
        for (int i = ProductCount - 1; i >= 0; i--)
        {
            if (products[i] <= 0) continue;
            GameObject prefab = productPrefabs[i];
            GameObject obj = prefab != null ? Instantiate(prefab, origin.position + origin.forward * dropForwardDistance, prefab.transform.rotation)
                : GameObject.CreatePrimitive(i == 1 ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            obj.name = "Dropped_" + ProductNames[i];
            WorldObject item = obj.GetComponent<WorldObject>();
            if (item == null) item = obj.AddComponent<WorldObject>();
            item.objectType = WorldObjectType.ProductPickup;
            item.productIndex = i;
            if (obj.GetComponent<Collider>() == null) obj.AddComponent<BoxCollider>();
            if (obj.GetComponent<Rigidbody>() == null) obj.AddComponent<Rigidbody>();
            obj.GetComponent<Rigidbody>().AddForce(origin.forward * 1.5f + Vector3.up, ForceMode.Impulse);
            products[i]--;
            Notify("Membuang " + ProductNames[i] + ". Tekan E untuk mengambil kembali.");
            return;
        }
    }

    
void DiscardTrash()
{
    if (carried == null || carried.Count == 0)
    {
        Notify("Tas barang kosong.");
        return;
    }

    Transform origin = dropOrigin;

    if (origin == null)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            origin = player.transform;
    }

    if (origin == null)
    {
        Notify("Posisi pemain tidak ditemukan.");
        return;
    }

    int last = carried.Count - 1;
    TrashType type = carried[last];
    int index = (int)type;

    if (index < 0 || index >= trashPrefabs.Length)
    {
        Debug.LogError("Trash Prefab index tidak valid: " + index);
        return;
    }

    Vector3 spawnPosition =
        origin.position +
        origin.forward * dropForwardDistance +
        Vector3.up * 0.8f;

    GameObject prefab = trashPrefabs[index];
    GameObject obj;

    if (prefab != null)
    {
        obj = Instantiate(
            prefab,
            spawnPosition,
            prefab.transform.rotation
        );
    }
    else
    {
        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position = spawnPosition;
        obj.transform.localScale = Vector3.one * 0.35f;

        Debug.LogWarning(
            "Prefab tidak diisi untuk: " + GetTrashName(type)
        );
    }

    if (obj == null)
    {
        Debug.LogError("Gagal membuat objek sampah.");
        return;
    }

    obj.name = "Dropped_" + GetTrashName(type);
    obj.transform.position = spawnPosition;
    obj.SetActive(true);

    WorldObject item = obj.GetComponent<WorldObject>();

    if (item == null)
        item = obj.AddComponent<WorldObject>();

    item.objectType = WorldObjectType.Trash;
    item.trashType = type;
    item.prompt = "[E] Ambil sampah";

    if (obj.GetComponentInChildren<Collider>() == null)
        obj.AddComponent<BoxCollider>();

    Rigidbody rb = obj.GetComponent<Rigidbody>();

    if (rb == null)
        rb = obj.AddComponent<Rigidbody>();

    rb.AddForce(
        origin.forward * 1.5f + Vector3.up,
        ForceMode.Impulse
    );

    carried.RemoveAt(last);

    Debug.Log(
        "BERHASIL DROP: " + obj.name +
        " | Position: " + obj.transform.position
    );

    Notify("Membuang " + GetTrashName(type));
}


    
void OnGUI()
{
    if (IsDead)
    {
        DrawDeathScreen();
        return;
    }

    // Sembunyikan seluruh HUD ketika Pause Menu aktif.
    if (PauseMenuUI.IsPaused)
        return;

    // HUD tetap terlihat saat menu tempat pilah/toko sedang terbuka.
    DrawStatusPanel();

    if (WorldObject.MenuOpen || VehicleController.CargoMenuOpen)
    {
        DrawMessage();
        return;
    }

    GUIStyle crosshairStyle = new GUIStyle(GUI.skin.label);
    crosshairStyle.alignment = TextAnchor.MiddleCenter;
    crosshairStyle.fontSize = 20;
    crosshairStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);

    GUI.Label(
        new Rect(Screen.width / 2 - 12, Screen.height / 2 - 15, 24, 28),
        "+",
        crosshairStyle
    );

    if (!string.IsNullOrEmpty(prompt))
    {
        EnsureHudStyles();

        GUI.Box(
            new Rect(Screen.width / 2 - 190, Screen.height / 2 + 28, 380, 32),
            prompt,
            hudPromptStyle
        );
    }

    DrawMessage();
}


    void EnsureHudStyles()
    {
        if (hudFrameStyle != null) return;

        hudFrameTexture = MakeHudTexture(new Color(0.16f, 0.38f, 0.71f, 1f));
        hudPanelTexture = MakeHudTexture(new Color(0.98f, 0.93f, 0.83f, 0.99f));
        hudHeaderTexture = MakeHudTexture(new Color(0.15f, 0.45f, 0.83f, 1f));
        hudDividerTexture = MakeHudTexture(new Color(0.84f, 0.77f, 0.61f, 0.95f));
        hudBadgeTexture = MakeHudTexture(new Color(0.43f, 0.78f, 0.18f, 1f));

        hudFrameStyle = new GUIStyle(GUI.skin.box);
        hudFrameStyle.normal.background = hudFrameTexture;
        hudFrameStyle.padding = new RectOffset(0, 0, 0, 0);

        hudPanelStyle = new GUIStyle(GUI.skin.box);
        hudPanelStyle.normal.background = hudPanelTexture;
        hudPanelStyle.padding = new RectOffset(0, 0, 0, 0);

        hudHeaderStyle = new GUIStyle(GUI.skin.label);
        hudHeaderStyle.fontSize = 17;
        hudHeaderStyle.fontStyle = FontStyle.Bold;
        hudHeaderStyle.alignment = TextAnchor.MiddleCenter;
        hudHeaderStyle.padding = new RectOffset(10, 10, 2, 2);
        hudHeaderStyle.normal.textColor = Color.white;
        hudHeaderStyle.normal.background = hudHeaderTexture;
        hudHeaderStyle.hover.background = hudHeaderTexture;
        hudHeaderStyle.active.background = hudHeaderTexture;

        hudRowStyle = new GUIStyle(GUI.skin.label);
        hudRowStyle.fontSize = 13;
        hudRowStyle.alignment = TextAnchor.MiddleLeft;
        hudRowStyle.padding = new RectOffset(6, 6, 2, 2);
        hudRowStyle.wordWrap = false;
        hudRowStyle.normal.textColor = new Color(0.23f, 0.16f, 0.10f);

        hudSmallStyle = new GUIStyle(hudRowStyle);
        hudSmallStyle.fontSize = 11;
        hudSmallStyle.normal.textColor = new Color(0.36f, 0.28f, 0.19f);

        hudBadgeStyle = new GUIStyle(GUI.skin.label);
        hudBadgeStyle.fontSize = 12;
        hudBadgeStyle.fontStyle = FontStyle.Bold;
        hudBadgeStyle.alignment = TextAnchor.MiddleCenter;
        hudBadgeStyle.normal.background = hudBadgeTexture;
        hudBadgeStyle.normal.textColor = Color.white;

        hudPromptStyle = new GUIStyle(GUI.skin.box);
        hudPromptStyle.fontSize = 14;
        hudPromptStyle.fontStyle = FontStyle.Bold;
        hudPromptStyle.alignment = TextAnchor.MiddleCenter;
        hudPromptStyle.normal.textColor = Color.white;
        hudPromptStyle.normal.background = hudHeaderTexture;

        hudMessageStyle = new GUIStyle(GUI.skin.label);
        hudMessageStyle.fontSize = 14;
        hudMessageStyle.fontStyle = FontStyle.Bold;
        hudMessageStyle.alignment = TextAnchor.MiddleCenter;
        hudMessageStyle.normal.textColor = Color.white;
    }

    Texture2D MakeHudTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void DrawStatusPanel()
    {
        EnsureHudStyles();

        float width = Mathf.Min(430f, Screen.width - 28f);
        float x = 18f;
        float y = 18f;
        float height = 310f;

        GUI.Box(new Rect(x, y + 10, width, height), GUIContent.none, hudFrameStyle);
        GUI.Box(new Rect(x + 4, y + 14, width - 8, height - 8), GUIContent.none, hudPanelStyle);

        float ribbonWidth = Mathf.Min(250f, width - 40f);
        float ribbonX = x + (width - ribbonWidth) * 0.5f;
        GUI.Box(new Rect(ribbonX, y, ribbonWidth, 38f), GUIContent.none, hudHeaderStyle);
        GUI.Label(new Rect(ribbonX, y + 1, ribbonWidth, 34f), "STATUS PEMAIN", hudHeaderStyle);

        float left = x + 18;
        float rowWidth = width - 36;
        float rowY = y + 52;
        float rowHeight = 24;

        DrawHudRow(left, rowY, rowWidth, rowHeight, "Uang", "Rp" + money.ToString("N0"));
        DrawHudRow(left, rowY + 24, rowWidth, rowHeight, "Hunger", Mathf.CeilToInt(hunger) + "/100");
        DrawHudRow(left, rowY + 48, rowWidth, rowHeight, "Tas (Inventaris)",
            TotalInventoryCount + "/" + inventoryCapacity + " | S:" + CarriedCount + " P:" + CarriedProductCount);
        DrawHudRow(left, rowY + 72, rowWidth, rowHeight, "Makanan (Konsumsi)", FoodInventoryCount.ToString());
        DrawHudRow(left, rowY + 96, rowWidth, rowHeight, "Tempat Pilah (Organik)", CountCategory(sorted, WasteCategory.Organic).ToString());
        DrawHudRow(left, rowY + 120, rowWidth, rowHeight, "Tempat Pilah (Anorganik)", CountCategory(sorted, WasteCategory.Inorganic).ToString());
        DrawHudRow(left, rowY + 144, rowWidth, rowHeight, "Produk Terjual", totalSold.ToString());

        GUI.Label(new Rect(left + 4, rowY + 174, rowWidth - 8, 18),
            "Stok makanan: " + FoodInventoryCount + "/" + foodCapacity + "   |   Tekan F untuk konsumsi", hudSmallStyle);
        GUI.Label(new Rect(left + 4, rowY + 194, rowWidth - 8, 34), FoodInventorySummary(), hudSmallStyle);
    }

    void DrawHudRow(float x, float y, float width, float height, string label, string value)
    {
        EnsureHudStyles();

        GUI.DrawTexture(new Rect(x, y + height - 1, width, 1), hudDividerTexture);

        GUIStyle labelStyle = new GUIStyle(hudRowStyle);
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(x, y, width * 0.54f, height), label, labelStyle);

        GUIStyle valueStyle = new GUIStyle(hudRowStyle);
        valueStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(x + width * 0.54f, y, width * 0.46f, height), value, valueStyle);
    }

    void DrawMessage()
    {
        if (messageTimer <= 0) return;
        EnsureHudStyles();
        float width = Mathf.Min(640f, Screen.width - 40f);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 66f;
        GUI.Box(new Rect(x, y, width, 42f), GUIContent.none, hudFrameStyle);
        GUI.Label(new Rect(x + 10, y + 4, width - 20, 34), message, hudMessageStyle);
    }

    public static bool IsValidTrashType(TrashType t) => (int)t >= 0 && (int)t < TrashTypeCount;
    public static string GetTrashName(TrashType t) => IsValidTrashType(t) ? TrashNames[(int)t] : "Sampah tidak dikenal";
    public static WasteCategory GetCategory(TrashType t) =>
        t == TrashType.FoodWaste || t == TrashType.DryLeaves ? WasteCategory.Organic : WasteCategory.Inorganic;
}
