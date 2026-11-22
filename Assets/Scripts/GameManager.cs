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
    public int bread;
    public int foodCapacity = 5;
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
    public static readonly TrashType[] AllTrashTypes =
    { TrashType.PlasticBottle, TrashType.PlasticCup, TrashType.Cardboard, TrashType.Paper,
      TrashType.MetalCan, TrashType.FoodWaste, TrashType.DryLeaves };

    string message = "";
    float messageTimer;
    int reservedSlots;

    // Status kematian pemain
    public bool IsDead { get; private set; }

    public int CarriedCount => carried == null ? 0 : carried.Count;
    public int CarriedProductCount
    {
        get { int n = 0; if (products != null) foreach (int x in products) n += Mathf.Max(0, x); return n; }
    }
    public int TotalInventoryCount => CarriedCount + CarriedProductCount;
    public int FreeInventorySpace => Mathf.Max(0, inventoryCapacity - TotalInventoryCount - reservedSlots);

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // Batasi laju hunger agar nilai lama yang tersimpan di Inspector
        // (misalnya 0.5) ikut menggunakan kecepatan baru yang lebih lambat.
        hungerDropPerSecond = Mathf.Clamp(hungerDropPerSecond, 0f, 0.15f);
        if (carried == null) carried = new List<TrashType>();
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

    void Eat()
    {
        if (bread <= 0) { Notify("Tidak ada roti! Beli di toko roti."); return; }
        if (hunger >= 100) { Notify("Hunger sudah penuh!"); return; }
        bread--; hunger = Mathf.Min(100, hunger + 20);
        Notify("Makan roti berhasil. Hunger: " + Mathf.CeilToInt(hunger) + " | Sisa roti: " + bread);
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
        // OnGUI tetap dipanggil ketika Time.timeScale = 0,
        // sehingga layar kematian tetap bisa ditampilkan.
        if (IsDead)
        {
            DrawDeathScreen();
            return;
        }

        if (WorldObject.MenuOpen || VehicleController.CargoMenuOpen)
        {
            DrawMessage(); return;
        }

        float w = Mathf.Min(500, Screen.width - 40);
        GUI.Box(new Rect(20, 20, w, 165), "STATUS PEMAIN");
        GUI.Label(new Rect(32, 48, w - 24, 20), "Uang: Rp" + money.ToString("N0") + " | Hunger: " + Mathf.CeilToInt(hunger) + "/100");
        GUI.Label(new Rect(32, 71, w - 24, 20), "Tas barang: " + TotalInventoryCount + "/" + inventoryCapacity +
            " (Sampah: " + CarriedCount + ", Produk: " + CarriedProductCount + ")");
        GUI.Label(new Rect(32, 94, w - 24, 20), "Makanan: " + bread + "/" + foodCapacity + " (slot terpisah)");
        GUI.Label(new Rect(32, 117, w - 24, 20), "Tempat pilah: Organik " + CountCategory(sorted, WasteCategory.Organic) +
            " | Anorganik " + CountCategory(sorted, WasteCategory.Inorganic));
        GUI.Label(new Rect(32, 140, w - 24, 20), "Produk terjual: " + totalSold + " | F: makan | G: buang");
        GUI.Label(new Rect(Screen.width/2 - 8, Screen.height/2 - 15, 25, 25), "+");
        if (!string.IsNullOrEmpty(prompt))
            GUI.Label(new Rect(Screen.width/2 - 200, Screen.height/2 + 28, 400, 28), prompt);
        DrawMessage();
    }

    void DrawMessage()
    {
        if (messageTimer <= 0) return;
        float w = Mathf.Min(700, Screen.width - 40);
        GUI.Box(new Rect((Screen.width-w)/2, Screen.height-65, w, 40), "");
        GUI.Label(new Rect((Screen.width-w)/2+10, Screen.height-58, w-20, 28), message);
    }

    public static bool IsValidTrashType(TrashType t) => (int)t >= 0 && (int)t < TrashTypeCount;
    public static string GetTrashName(TrashType t) => IsValidTrashType(t) ? TrashNames[(int)t] : "Sampah tidak dikenal";
    public static WasteCategory GetCategory(TrashType t) =>
        t == TrashType.FoodWaste || t == TrashType.DryLeaves ? WasteCategory.Organic : WasteCategory.Inorganic;
}
