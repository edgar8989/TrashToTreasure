using System.Collections;
using UnityEngine;

public class WorldObject : Interactable
{
    public static bool MenuOpen { get; private set; }
    static WorldObject active;

    // Gaya UI biru-krem yang konsisten dengan panel Status Pemain dan Quest.
    static GUIStyle menuFrameStyle, menuPanelStyle, menuHeaderStyle;
    static GUIStyle menuLabelStyle, menuButtonStyle, menuBoxStyle;
    static Texture2D menuFrameTexture, menuPanelTexture, menuHeaderTexture;
    static Texture2D menuButtonTexture, menuButtonHoverTexture, menuRowTexture;

    [Header("Jenis Objek")]
    public WorldObjectType objectType = WorldObjectType.Trash;
    public TrashType trashType = TrashType.PlasticBottle;
    public WasteCategory category = WasteCategory.Inorganic;
    [Range(0, GameManager.ProductCount - 1)] public int productIndex;

    [Header("Pengaturan")]
    [Min(1)] public int capacity = 5;
    [Min(0.1f)] public float processTimePerItem = 1.5f;
    public int[] productPrices = { 15000, 20000, 25000, 10000 };

    [Header("Posisi Produk Hasil Daur Ulang")]
    [Min(0.1f)] public float productDropSideOffset = 1.5f;
    [Min(0.1f)] public float productDropSpacing = 0.6f;
    [Min(0f)] public float productDropHeight = 0.8f;

    static readonly int[] machineStock = new int[GameManager.TrashTypeCount];
    static readonly TrashType[] inorganic =
    {
        TrashType.PlasticBottle, TrashType.PlasticCup, TrashType.Cardboard,
        TrashType.Paper, TrashType.MetalCan
    };
    static readonly TrashType[] organic = { TrashType.FoodWaste, TrashType.DryLeaves };

    TrashType selectedTrash;
    int selectedProduct, amount = 1;
    bool selected;
    string feedback = "";
    bool processing;

    void Reset() { SetPrompt(); }
    void OnValidate() { SetPrompt(); }

    void SetPrompt()
    {
        switch (objectType)
        {
            case WorldObjectType.Trash: prompt = "[E] Ambil sampah"; break;
            case WorldObjectType.FoodShop: prompt = "[E] Buka toko makanan"; break;
            case WorldObjectType.SortingBin: prompt = "[E] Buka tempat pilah"; break;
            case WorldObjectType.RecyclingMachine: prompt = "[E] Gunakan mesin daur ulang"; break;
            case WorldObjectType.SellPoint: prompt = "[E] Jual produk"; break;
            case WorldObjectType.ProductPickup: prompt = "[E] Ambil produk"; break;
        }
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;
        if (gm == null) return;

        if (objectType == WorldObjectType.Trash)
        {
            if (gm.TryAddCarried(trashType))
            {
                gm.Notify("Mengambil " + GameManager.GetTrashName(trashType) +
                          " (" + gm.TotalInventoryCount + "/" + gm.inventoryCapacity + ")");
                Destroy(gameObject);
            }
            else gm.Notify("Tas barang penuh! Simpan barang di bagasi.");
            return;
        }

        if (objectType == WorldObjectType.ProductPickup)
        {
            if (productIndex < 0 || productIndex >= GameManager.ProductCount) return;
            if (gm.FreeInventorySpace <= 0)
            {
                gm.Notify("Tas barang penuh! Simpan barang di bagasi.");
                return;
            }
            gm.products[productIndex]++;
            gm.Notify("Mengambil " + GameManager.ProductNames[productIndex]);
            Destroy(gameObject);
            return;
        }

        if (objectType == WorldObjectType.FoodShop)
        {
            active = this;
            MenuOpen = true;
            selected = false;
            amount = 1;
            feedback = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (objectType == WorldObjectType.RecyclingMachine && processing)
        {
            gm.Notify("Mesin sedang memproses sampah!");
            return;
        }

        active = this;
        MenuOpen = true;
        selected = false;
        amount = 1;
        feedback = "";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (!MenuOpen || active != this || GameManager.I == null) return;
        switch (objectType)
        {
            case WorldObjectType.FoodShop: DrawFoodShop(); break;
            case WorldObjectType.SortingBin: DrawSorting(); break;
            case WorldObjectType.RecyclingMachine: DrawMachine(); break;
            case WorldObjectType.SellPoint: DrawSell(); break;
        }
    }

    bool sendToMachine;


    void DrawFoodShop()
    {
        GameManager gm = GameManager.I;
        Window("TOKO MAKANAN", () =>
        {
            GUILayout.Label("Uang: Rp" + gm.money.ToString("N0"));
            GUILayout.Label("Inventaris makanan: " + gm.FoodInventoryCount + "/" + gm.foodCapacity);
            GUILayout.Space(8);

            for (int i = 0; i < GameManager.FoodCount; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label(GameManager.FoodNames[i] + " | Rp" + GameManager.FoodPrices[i].ToString("N0"));
                GUILayout.Label("Memulihkan " + GameManager.FoodHungerRestore[i].ToString("0") + " Hunger");
                GUILayout.EndVertical();

                GUI.enabled = gm.FoodInventoryCount < gm.foodCapacity && gm.money >= GameManager.FoodPrices[i];
                if (GUILayout.Button("Beli", GUILayout.Width(85), GUILayout.Height(38)))
                    gm.TryBuyFood(i);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(feedback);
        });
    }

    void DrawSorting()
    {
        GameManager gm = GameManager.I;
        Window("TEMPAT PILAH " + category.ToString().ToUpper(), () =>
        {
            int stored = gm.CountCategory(gm.sorted, category);
            GUILayout.Label("Isi tempat pilah: " + stored + "/5");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(sendToMachine ? "Simpan sampah" : "Simpan sampah ✓"))
            { sendToMachine = false; selected = false; amount = 1; }
            if (GUILayout.Button(sendToMachine ? "Pindahkan ke mesin ✓" : "Pindahkan ke mesin"))
            { sendToMachine = true; selected = false; amount = 1; }
            GUILayout.EndHorizontal();

            foreach (TrashType type in Types())
            {
                int stock = sendToMachine ? gm.sorted[(int)type] : gm.CountCarried(type);
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameManager.GetTrashName(type) + " (" + stock + ")");
                GUI.enabled = stock > 0;
                if (GUILayout.Button(selected && selectedTrash == type ? "Dipilih" : "Pilih", GUILayout.Width(85)))
                { selectedTrash = type; selected = true; amount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            if (selected)
            {
                int available = sendToMachine
                    ? gm.sorted[(int)selectedTrash]
                    : gm.CountCarried(selectedTrash);
                int room = sendToMachine
                    ? MachineRemaining(category)
                    : Mathf.Max(0, 5 - stored);
                int max = Mathf.Min(available, room);
                AmountPicker(max);
                if (max > 0 && GUILayout.Button(sendToMachine ? "Pindahkan ke mesin" : "Masukkan ke tempat pilah"))
                {
                    int transfer = selectedTransferAmount;
                    int idx = (int)selectedTrash;
                    if (sendToMachine)
                    {
                        machineStock[idx] += transfer;
                        gm.sorted[idx] -= transfer;
                        feedback = "Sampah dipindahkan ke mesin.";
                    }
                    else
                    {
                        int moved = 0;
                        for (int i = gm.carried.Count - 1; i >= 0 && moved < transfer; i--)
                            if (gm.carried[i] == selectedTrash) { gm.carried.RemoveAt(i); moved++; }
                        gm.sorted[idx] += moved;
                        feedback = "Sampah berhasil dimasukkan.";
                    }
                    selected = false;
                    amount = 1;
                }
            }
            GUILayout.Label(feedback);
        });
    }

    int selectedTransferAmount = 1;
    void AmountPicker(int max)
    {
        if (max <= 0) { GUILayout.Label("Kapasitas penuh."); return; }
        amount = Mathf.Clamp(amount, 1, max);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-", GUILayout.Width(45))) amount = Mathf.Max(1, amount - 1);
        GUILayout.Label("Jumlah: " + amount);
        if (GUILayout.Button("+", GUILayout.Width(45))) amount = Mathf.Min(max, amount + 1);
        GUILayout.EndHorizontal();
        selectedTransferAmount = amount;
    }

    void DrawMachine()
    {
        GameManager gm = GameManager.I;
        Window("MESIN DAUR ULANG - " + category.ToString().ToUpper(), () =>
        {
            GUILayout.Label("Kapasitas mesin: " + MachineTotal() + "/5");
            foreach (TrashType type in Types())
            {
                int stock = machineStock[(int)type];
                int pi = gm.GetProductIndex(type);
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameManager.GetTrashName(type) + " (" + stock + ") → " +
                    (pi >= 0 ? GameManager.ProductNames[pi] : "-"));
                GUI.enabled = stock > 0;
                if (GUILayout.Button(selected && selectedTrash == type ? "Dipilih" : "Pilih", GUILayout.Width(85)))
                { selectedTrash = type; selected = true; amount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (selected)
            {
                int max = Mathf.Min(machineStock[(int)selectedTrash], 5);
                AmountPicker(max);
                GUILayout.Label("Waktu proses: " + (amount * processTimePerItem).ToString("0.0") + " detik");
                if (GUILayout.Button("Mulai proses") && max >= amount) StartCoroutine(Process());
            }
            GUILayout.Label(feedback);
        });
    }

    IEnumerator Process()
    {
        GameManager gm = GameManager.I;
        int count = selectedTransferAmount;
        int pi = gm.GetProductIndex(selectedTrash);

        if (pi < 0 || count <= 0)
        {
            feedback = "Produk tidak valid.";
            yield break;
        }

        machineStock[(int)selectedTrash] -= count;
        processing = true;
        Close();

        yield return new WaitForSeconds(count * Mathf.Max(0.1f, processTimePerItem));

        SpawnProductPickups(gm, pi, count);
        PakIwanQuest.RegisterRecycledProducts(pi, count);
        gm.Notify("Daur ulang selesai. " + GameManager.ProductNames[pi] + " x" + count +
                  " jatuh di samping mesin. Tekan E untuk mengambilnya.");

        processing = false;
    }

    void SpawnProductPickups(GameManager gm, int index, int count)
    {
        GameObject prefab = null;
        if (gm.productPrefabs != null && index >= 0 && index < gm.productPrefabs.Length)
            prefab = gm.productPrefabs[index];

        for (int i = 0; i < count; i++)
        {
            float spacing = (i - (count - 1) * 0.5f) * productDropSpacing;
            Vector3 spawnPosition = transform.position
                + transform.right * productDropSideOffset
                + transform.forward * spacing
                + Vector3.up * productDropHeight;

            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab, spawnPosition, prefab.transform.rotation);
            }
            else
            {
                obj = GameObject.CreatePrimitive(
                    index == 1 ? PrimitiveType.Cube : PrimitiveType.Cylinder
                );
                obj.transform.position = spawnPosition;
            }

            obj.name = "ProductPickup_" + GameManager.ProductNames[index];

            WorldObject pickup = obj.GetComponent<WorldObject>();
            if (pickup == null)
                pickup = obj.AddComponent<WorldObject>();

            pickup.objectType = WorldObjectType.ProductPickup;
            pickup.productIndex = index;
            pickup.SetPrompt();

            if (obj.GetComponentsInChildren<Collider>().Length == 0)
                obj.AddComponent<BoxCollider>();

            MeshCollider[] meshColliders = obj.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider mesh in meshColliders)
                mesh.convex = true;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
                rb = obj.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(
                transform.right * 1.2f + Vector3.up * 1.5f,
                ForceMode.Impulse
            );
        }
    }

    int MachineTotal()
    {
        int total = 0;
        foreach (TrashType t in Types()) total += machineStock[(int)t];
        return total;
    }

    void DrawSell()
    {
        GameManager gm = GameManager.I;
        Window("TOKO DAUR ULANG", () =>
        {
            for (int i = 0; i < GameManager.ProductCount; i++)
            {
                int idx = i;
                int price = idx < productPrices.Length ? productPrices[idx] : 0;
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameManager.ProductNames[idx] + " | Stok: " + gm.products[idx] +
                    " | Rp" + price.ToString("N0"));
                GUI.enabled = gm.products[idx] > 0;
                if (GUILayout.Button(selected && selectedProduct == idx ? "Dipilih" : "Pilih", GUILayout.Width(85)))
                { selectedProduct = idx; selected = true; amount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (selected && selectedProduct >= 0 && selectedProduct < gm.products.Length)
            {
                int max = gm.products[selectedProduct];
                AmountPicker(max);
                int price = selectedProduct < productPrices.Length ? productPrices[selectedProduct] : 0;
                GUILayout.Label("Total: Rp" + (price * amount).ToString("N0"));
                if (GUILayout.Button("Jual produk") && max >= amount && price > 0)
                {
                    int sold = selectedTransferAmount;
                    gm.products[selectedProduct] -= sold;
                    gm.money += price * sold;
                    gm.totalSold += sold;
                    gm.Notify("Produk terjual! +Rp" + (price * sold).ToString("N0"));
                    selected = false; amount = 1;
                }
            }
            GUILayout.Label(feedback);
        });
    }

    void EnsureWindowStyles()
    {
        if (menuFrameStyle != null) return;

        menuFrameTexture = CreateMenuTexture(new Color(0.13f, 0.34f, 0.65f, 1f));
        menuPanelTexture = CreateMenuTexture(new Color(0.98f, 0.93f, 0.83f, 0.99f));
        menuHeaderTexture = CreateMenuTexture(new Color(0.15f, 0.46f, 0.84f, 1f));
        menuButtonTexture = CreateMenuTexture(new Color(0.12f, 0.43f, 0.78f, 1f));
        menuButtonHoverTexture = CreateMenuTexture(new Color(0.20f, 0.55f, 0.90f, 1f));
        menuRowTexture = CreateMenuTexture(new Color(1f, 0.97f, 0.89f, 1f));

        menuFrameStyle = new GUIStyle(GUI.skin.box);
        menuFrameStyle.normal.background = menuFrameTexture;

        menuPanelStyle = new GUIStyle(GUI.skin.box);
        menuPanelStyle.normal.background = menuPanelTexture;

        menuHeaderStyle = new GUIStyle(GUI.skin.label);
        menuHeaderStyle.fontSize = 17;
        menuHeaderStyle.fontStyle = FontStyle.Bold;
        menuHeaderStyle.alignment = TextAnchor.MiddleCenter;
        menuHeaderStyle.normal.textColor = Color.white;
        menuHeaderStyle.normal.background = menuHeaderTexture;

        menuLabelStyle = new GUIStyle(GUI.skin.label);
        menuLabelStyle.fontSize = 13;
        menuLabelStyle.wordWrap = true;
        menuLabelStyle.normal.textColor = new Color(0.25f, 0.18f, 0.12f);

        menuButtonStyle = new GUIStyle(GUI.skin.button);
        menuButtonStyle.fontSize = 12;
        menuButtonStyle.fontStyle = FontStyle.Bold;
        menuButtonStyle.alignment = TextAnchor.MiddleCenter;
        menuButtonStyle.fixedHeight = 30;
        menuButtonStyle.normal.textColor = Color.white;
        menuButtonStyle.hover.textColor = Color.white;
        menuButtonStyle.active.textColor = Color.white;
        menuButtonStyle.normal.background = menuButtonTexture;
        menuButtonStyle.hover.background = menuButtonHoverTexture;
        menuButtonStyle.active.background = menuButtonTexture;

        menuBoxStyle = new GUIStyle(GUI.skin.box);
        menuBoxStyle.normal.background = menuRowTexture;
        menuBoxStyle.normal.textColor = new Color(0.25f, 0.18f, 0.12f);
        menuBoxStyle.padding = new RectOffset(8, 8, 5, 5);
    }

    Texture2D CreateMenuTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void Window(string title, System.Action content)
    {
        EnsureWindowStyles();

        float w = Mathf.Min(560f, Screen.width - 30f);
        float h = Mathf.Min(540f, Screen.height - 30f);
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        // Bingkai biru dan panel isi krem.
        GUI.Box(new Rect(x, y + 10f, w, h), GUIContent.none, menuFrameStyle);
        GUI.Box(new Rect(x + 5f, y + 15f, w - 10f, h - 10f),
            GUIContent.none, menuPanelStyle);
        GUI.Box(new Rect(x + 18f, y, w - 36f, 44f),
            GUIContent.none, menuHeaderStyle);
        GUI.Label(new Rect(x + 18f, y + 1f, w - 36f, 42f),
            title, menuHeaderStyle);

        // Simpan style bawaan dan pulihkan setelah panel selesai digambar.
        GUIStyle oldLabel = GUI.skin.label;
        GUIStyle oldButton = GUI.skin.button;
        GUIStyle oldBox = GUI.skin.box;
        GUIStyle oldWindow = GUI.skin.window;

        try
        {
            GUI.skin.label = menuLabelStyle;
            GUI.skin.button = menuButtonStyle;
            GUI.skin.box = menuBoxStyle;
            GUI.skin.window = menuPanelStyle;

            GUILayout.BeginArea(new Rect(x + 24f, y + 58f, w - 48f, h - 82f));
            GUILayout.BeginVertical();

            content();

            GUILayout.FlexibleSpace();
            GUILayout.Space(8f);
            if (GUILayout.Button("TUTUP", GUILayout.Height(36f)))
                Close();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        finally
        {
            GUI.skin.label = oldLabel;
            GUI.skin.button = oldButton;
            GUI.skin.box = oldBox;
            GUI.skin.window = oldWindow;
        }
    }

    TrashType[] Types() => category == WasteCategory.Organic ? organic : inorganic;

    public static int MachineStock(TrashType type) => machineStock[(int)type];
    public static int MachineRemaining(WasteCategory cat)
    {
        int total = 0;
        foreach (TrashType t in GameManager.AllTrashTypes)
            if (GameManager.GetCategory(t) == cat) total += machineStock[(int)t];
        return Mathf.Max(0, 5-total);
    }

    void Update()
    {
        if (MenuOpen && active == this && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void Close()
    {
        MenuOpen = false; active = null; selected = false;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }
}
