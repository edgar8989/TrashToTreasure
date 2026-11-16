
using System.Collections.Generic;
using UnityEngine;

// Urutan enum dipertahankan agar kompatibel dengan Scene.
public enum TrashType
{
    PlasticBottle = 0,
    Paper = 1,
    MetalCan = 2,
    FoodWaste = 3,
    DryLeaves = 4,
    PlasticCup = 5,
    Cardboard = 6
}

public enum WasteCategory
{
    Organic,
    Inorganic
}

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public const int TrashTypeCount = 7;
    public const int ProductCount = 4;

    [Header("Status")]
    public int money = 20000;

    [Range(0f, 100f)]
    public float hunger = 100f;

    public float hungerDropPerSecond = 0.5f;

    public int bread = 0;

    [Header("Inventory Pemain")]
    [Min(1)]
    public int inventoryCapacity = 5;

    public List<TrashType> carried = new List<TrashType>();

    [Header("Pengaturan Sampah yang Dibuang")]
    [Tooltip("Opsional. Jika kosong, posisi pemain digunakan otomatis.")]
    public Transform dropOrigin;

    [Min(0f)]
    public float dropForwardDistance = 1.25f;

    // Template dibuat otomatis dari objek TrashItem yang sudah ada di Scene.
    // Tidak perlu mengisi prefab satu per satu melalui Inspector.
    private TrashItem[] discardTemplates = new TrashItem[TrashTypeCount];
    private GameObject templateContainer;

    public int CarriedCount => carried != null ? carried.Count : 0;

    public int FreeInventorySpace =>
        Mathf.Max(0, inventoryCapacity - CarriedCount);

    // Menambahkan sampah hanya jika kapasitas tas masih mencukupi.
    public bool TryAddCarried(TrashType type, int amount = 1)
    {
        if (!IsValidTrashType(type) || amount <= 0 || carried == null)
            return false;

        if (amount > FreeInventorySpace)
            return false;

        for (int i = 0; i < amount; i++)
            carried.Add(type);

        return true;
    }

    [Header("Stok Tempat Pemilahan")]
    public int[] sorted = new int[TrashTypeCount];

    [Header("Produk Jadi")]
    public int[] products = new int[ProductCount];

    public int totalSold = 0;

    [HideInInspector]
    public string prompt = "";

    private string message = "";
    private float messageTimer = 0f;

    public static readonly string[] TrashNames =
    {
        "Botol Plastik",
        "Kertas",
        "Kaleng",
        "Sisa Makanan",
        "Daun Kering",
        "Gelas Plastik",
        "Kardus"
    };

    public static readonly string[] ProductNames =
    {
        "Pot Tanaman",
        "Buku Catatan",
        "Wadah Logam",
        "Kompos"
    };

    public static readonly TrashType[] AllTrashTypes =
    {
        TrashType.PlasticBottle,
        TrashType.PlasticCup,
        TrashType.Cardboard,
        TrashType.Paper,
        TrashType.MetalCan,
        TrashType.FoodWaste,
        TrashType.DryLeaves
    };

    // ==========================================
    // INITIALIZATION
    // ==========================================

    void Awake()
    {
        if (I != null && I != this)
        {
            Debug.LogWarning(
                "Terdapat lebih dari satu GameManager."
            );

            Destroy(gameObject);
            return;
        }

        I = this;

        if (carried == null)
            carried = new List<TrashType>();

        if (sorted == null || sorted.Length != TrashTypeCount)
            sorted = ResizeArray(sorted, TrashTypeCount);

        if (products == null || products.Length != ProductCount)
            products = ResizeArray(products, ProductCount);

        CacheDiscardTemplates();
    }

    // Mengambil contoh objek sampah langsung dari TrashObjects di Scene.
    // Contoh disimpan dalam keadaan nonaktif agar objek aslinya tidak terganggu.
    void CacheDiscardTemplates()
    {
        discardTemplates = new TrashItem[TrashTypeCount];

        templateContainer = new GameObject("__TrashDiscardTemplates");
        templateContainer.hideFlags = HideFlags.HideInHierarchy;
        templateContainer.SetActive(false);

        TrashItem[] sceneTrash = FindObjectsByType<TrashItem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (TrashItem sceneItem in sceneTrash)
        {
            if (sceneItem == null || !IsValidTrashType(sceneItem.type))
                continue;

            int index = (int)sceneItem.type;
            if (discardTemplates[index] != null)
                continue;

            GameObject templateObject = Instantiate(sceneItem.gameObject);
            templateObject.name = "Template_" + GetTrashName(sceneItem.type);
            templateObject.transform.SetParent(templateContainer.transform, false);
            templateObject.SetActive(false);

            TrashItem template = templateObject.GetComponent<TrashItem>();
            if (template == null)
                template = templateObject.GetComponentInChildren<TrashItem>(true);

            if (template != null)
                discardTemplates[index] = template;
            else
                Destroy(templateObject);
        }

        int found = 0;
        for (int i = 0; i < discardTemplates.Length; i++)
        {
            if (discardTemplates[i] != null)
                found++;
        }

        Debug.Log("GameManager: " + found + " dari " + TrashTypeCount +
            " jenis contoh sampah berhasil dibaca dari Scene.");
    }

    int[] ResizeArray(int[] oldArray, int newSize)
    {
        int[] newArray = new int[newSize];

        if (oldArray != null)
        {
            int copyLength = Mathf.Min(
                oldArray.Length,
                newSize
            );

            for (int i = 0; i < copyLength; i++)
                newArray[i] = oldArray[i];
        }

        return newArray;
    }

    // ==========================================
    // TRASH INFORMATION
    // ==========================================

    public static bool IsValidTrashType(TrashType type)
    {
        int index = (int)type;

        return index >= 0 && index < TrashTypeCount;
    }

    public static string GetTrashName(TrashType type)
    {
        if (!IsValidTrashType(type))
            return "Sampah tidak dikenal";

        return TrashNames[(int)type];
    }

    public static WasteCategory GetCategory(TrashType type)
    {
        switch (type)
        {
            case TrashType.FoodWaste:
            case TrashType.DryLeaves:
                return WasteCategory.Organic;

            case TrashType.PlasticBottle:
            case TrashType.PlasticCup:
            case TrashType.Cardboard:
            case TrashType.Paper:
            case TrashType.MetalCan:
            default:
                return WasteCategory.Inorganic;
        }
    }

    // ==========================================
    // INVENTORY
    // ==========================================

    public int CountCarried(TrashType type)
    {
        if (carried == null)
            return 0;

        int count = 0;

        foreach (TrashType item in carried)
        {
            if (item == type)
                count++;
        }

        return count;
    }

    public int CountCategory(
        int[] stock,
        WasteCategory category
    )
    {
        if (stock == null)
            return 0;

        int total = 0;

        int count = Mathf.Min(
            stock.Length,
            TrashTypeCount
        );

        for (int i = 0; i < count; i++)
        {
            if (GetCategory((TrashType)i) == category)
                total += stock[i];
        }

        return total;
    }

    // ==========================================
    // RECYCLING PRODUCT
    // ==========================================

    public int GetProductIndex(TrashType type)
    {
        switch (type)
        {
            case TrashType.PlasticBottle:
            case TrashType.PlasticCup:
                return 0;

            case TrashType.Cardboard:
            case TrashType.Paper:
                return 1;

            case TrashType.MetalCan:
                return 2;

            case TrashType.FoodWaste:
            case TrashType.DryLeaves:
                return 3;

            default:
                return -1;
        }
    }

    // ==========================================
    // MAIN UPDATE
    // ==========================================

    void Update()
    {
        UpdateHunger();
        HandleEating();
        HandleDiscarding();
        UpdateMessage();
    }

    // ==========================================
    // DISCARD SYSTEM - PRESS G
    // ==========================================

    void HandleDiscarding()
    {
        if (SortingBin.MenuOpen ||
            Machine.MenuOpen ||
            SellPoint.MenuOpen ||
            VehicleCargo.MenuOpen ||
            VehicleController.IsPlayerDriving)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.G))
            return;

        DiscardLastCarried();
    }

    public bool DiscardLastCarried()
    {
        if (carried == null || carried.Count == 0)
        {
            Notify("Tas pemain kosong. Tidak ada sampah yang bisa dibuang.");
            return false;
        }

        int lastIndex = carried.Count - 1;
        TrashType discardedType = carried[lastIndex];

        Transform origin = dropOrigin;
        if (origin == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                origin = player.transform;
        }

        if (origin == null)
        {
            Notify("Posisi pemain tidak ditemukan. Sampah tetap berada di tas.");
            return false;
        }

        Vector3 spawnPosition = origin.position +
            origin.forward * dropForwardDistance +
            Vector3.up * 0.5f;

        TrashItem droppedItem = CreateDroppedTrash(
            discardedType,
            spawnPosition
        );

        if (droppedItem == null)
        {
            Notify("Gagal membuat objek sampah. Sampah tetap berada di tas.");
            return false;
        }

        droppedItem.type = discardedType;
        droppedItem.prompt = "[E] Ambil sampah";

        // Hapus dari tas hanya setelah objek sampah berhasil dibuat.
        carried.RemoveAt(lastIndex);

        Rigidbody droppedBody = droppedItem.GetComponent<Rigidbody>();
        if (droppedBody == null)
            droppedBody = droppedItem.gameObject.AddComponent<Rigidbody>();

        droppedBody.AddForce(
            origin.forward * 1.5f + Vector3.up * 0.75f,
            ForceMode.Impulse
        );

        Notify("Membuang " + GetTrashName(discardedType) +
            ". Sampah muncul di depan pemain. Isi tas: " +
            CarriedCount + " / " + inventoryCapacity);
        return true;
    }

    // Membuat sampah dari contoh objek Scene. Jika jenisnya tidak ditemukan,
    // buat objek sederhana agar proses buang tetap berfungsi tanpa Inspector.
    TrashItem CreateDroppedTrash(TrashType type, Vector3 position)
    {
        int index = (int)type;
        TrashItem template =
            discardTemplates != null &&
            index >= 0 && index < discardTemplates.Length
            ? discardTemplates[index]
            : null;

        GameObject droppedObject;

        if (template != null)
        {
            droppedObject = Instantiate(
                template.gameObject,
                position,
                Quaternion.identity,
                null
            );
            droppedObject.name = "Dropped_" + GetTrashName(type);
            droppedObject.SetActive(true);
        }
        else
        {
            // Fallback: tetap bisa diambil kembali dengan tombol E.
            droppedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droppedObject.name = "Dropped_" + GetTrashName(type);
            droppedObject.transform.position = position;
            droppedObject.transform.localScale = Vector3.one * 0.35f;

            Renderer itemRenderer = droppedObject.GetComponent<Renderer>();
            if (itemRenderer != null)
            {
                switch (type)
                {
                    case TrashType.PlasticBottle:
                    case TrashType.PlasticCup:
                        itemRenderer.material.color = new Color(0.2f, 0.65f, 1f);
                        break;
                    case TrashType.Paper:
                    case TrashType.Cardboard:
                        itemRenderer.material.color = new Color(0.75f, 0.58f, 0.32f);
                        break;
                    case TrashType.MetalCan:
                        itemRenderer.material.color = Color.gray;
                        break;
                    case TrashType.FoodWaste:
                        itemRenderer.material.color = new Color(0.45f, 0.7f, 0.25f);
                        break;
                    case TrashType.DryLeaves:
                        itemRenderer.material.color = new Color(0.55f, 0.32f, 0.12f);
                        break;
                }
            }
        }

        TrashItem droppedItem = droppedObject.GetComponent<TrashItem>();
        if (droppedItem == null)
            droppedItem = droppedObject.AddComponent<TrashItem>();

        if (droppedObject.GetComponent<Collider>() == null)
            droppedObject.AddComponent<BoxCollider>();

        droppedItem.type = type;
        droppedItem.prompt = "[E] Ambil sampah";
        return droppedItem;
    }

    // ==========================================
    // HUNGER SYSTEM
    // ==========================================

    void UpdateHunger()
    {
        hunger = Mathf.Max(
            0f,
            hunger - hungerDropPerSecond * Time.deltaTime
        );
    }

    // ==========================================
    // EATING SYSTEM - PRESS F
    // ==========================================

    void HandleEating()
    {
        // Jangan makan ketika menu interaksi terbuka.

        if (SortingBin.MenuOpen ||
            Machine.MenuOpen ||
            SellPoint.MenuOpen ||
            VehicleCargo.MenuOpen)
        {
            return;
        }

        // Tunggu tombol F ditekan.

        if (!Input.GetKeyDown(KeyCode.F))
            return;

        // Periksa apakah pemain memiliki roti.

        if (bread <= 0)
        {
            Notify(
                "Tidak ada roti! Beli roti di FoodShop."
            );

            return;
        }

        // Periksa apakah hunger sudah penuh.

        if (hunger >= 100f)
        {
            Notify("Hunger sudah penuh!");

            return;
        }

        // Konsumsi satu roti.

        bread--;

        // Tambahkan hunger 20 poin.

        float oldHunger = hunger;

        hunger = Mathf.Min(
            100f,
            hunger + 20f
        );

        float restored = hunger - oldHunger;

        // Tampilkan notifikasi.

        Notify(
            "Berhasil makan roti! Hunger +" +
            restored.ToString("0") +
            " | Sisa roti: " + bread
        );

        Debug.Log(
            "MAKAN BERHASIL | Hunger: " +
            hunger +
            " | Roti: " +
            bread
        );
    }

    // ==========================================
    // MESSAGE SYSTEM
    // ==========================================

    void UpdateMessage()
    {
        if (messageTimer > 0f)
            messageTimer -= Time.deltaTime;
    }

    public void Notify(string text)
    {
        message = text;
        messageTimer = 3f;

        Debug.Log(text);
    }

    // ==========================================
    // HUD
    // ==========================================

    void OnGUI()
    {
        GUI.skin.label.fontSize = 14;

        DrawHUDPanel();
        DrawInteraction();
    }

    void DrawHUDPanel()
    {
        // Panel latar agar informasi mudah dibaca dan tidak bertumpuk.
        float panelWidth = Mathf.Min(560f, Screen.width - 20f);
        float panelHeight = 218f;

        

        float x = 22f;
        float width = panelWidth - 24f;
        float y = 42f;
        float line = 23f;

        GUI.Label(
            new Rect(x, y, width, line),
            "Hunger: " + Mathf.CeilToInt(hunger) + " / 100"
        );
        y += line;

        GUI.Label(
            new Rect(x, y, width, line),
            "Uang: Rp" + money.ToString("N0")
        );
        y += line + 5f;

        GUI.Label(
            new Rect(x, y, width, line),
            "Tas: " + CarriedCount + " / " + inventoryCapacity
        );
        y += line;

        GUI.Label(
            new Rect(x, y, width, line),
            "Botol: " + CountCarried(TrashType.PlasticBottle) +
            " | Gelas: " + CountCarried(TrashType.PlasticCup) +
            " | Kardus: " + CountCarried(TrashType.Cardboard) +
            " | Kertas: " + CountCarried(TrashType.Paper)
        );
        y += line;

        GUI.Label(
            new Rect(x, y, width, line),
            "Kaleng: " + CountCarried(TrashType.MetalCan) +
            " | Makanan: " + CountCarried(TrashType.FoodWaste) +
            " | Daun: " + CountCarried(TrashType.DryLeaves) +
            " | Roti: " + bread
        );
        y += line;

        GUI.Label(
            new Rect(x, y, width, line),
            "Tempat Pilah: Organik " +
            CountCategory(sorted, WasteCategory.Organic) +
            " | Anorganik " +
            CountCategory(sorted, WasteCategory.Inorganic)
        );
        y += line;

        GUI.Label(
            new Rect(x, y, width, line),
            "Produk: Pot " + products[0] +
            " | Buku " + products[1] +
            " | Wadah " + products[2] +
            " | Kompos " + products[3] +
            " | Terjual: " + totalSold
        );
        y += line;

        GUI.Label(
            new Rect(x, y, width, line),
            "Tekan G untuk membuang 1 sampah dari tas."
        );
    }

    void DrawInteraction()
    {
        GUI.skin.label.fontSize = 16;

        GUI.Label(
            new Rect(
                Screen.width / 2 - 8,
                Screen.height / 2 - 13,
                30,
                25
            ),
            "+"
        );

        if (!string.IsNullOrEmpty(prompt))
        {
            GUI.Label(
                new Rect(
                    Screen.width / 2 - 220,
                    Screen.height / 2 + 30,
                    440,
                    30
                ),
                prompt
            );
        }

        if (messageTimer > 0f)
        {
            GUI.skin.label.fontSize = 14;

            float messageWidth = Mathf.Min(700f, Screen.width - 40f);
            float messageX = (Screen.width - messageWidth) / 2f;

            GUI.Box(
                new Rect(messageX, Screen.height - 75f, messageWidth, 42f),
                ""
            );

            GUI.Label(
                new Rect(messageX + 10f, Screen.height - 67f, messageWidth - 20f, 28f),
                message
            );
        }
    }

}
