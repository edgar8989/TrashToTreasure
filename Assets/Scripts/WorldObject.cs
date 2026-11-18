using System.Collections;
using UnityEngine;

public class WorldObject : Interactable
{
    public static bool MenuOpen { get; private set; }
    static WorldObject active;

    [Header("Jenis Objek")]
    public WorldObjectType objectType = WorldObjectType.Trash;
    public TrashType trashType = TrashType.PlasticBottle;
    public WasteCategory category = WasteCategory.Inorganic;
    [Range(0, GameManager.ProductCount - 1)] public int productIndex;

    [Header("Pengaturan")]
    [Min(1)] public int capacity = 5;
    [Min(0.1f)] public float processTimePerItem = 1.5f;
    public int breadPrice = 5000;
    public int[] productPrices = { 15000, 20000, 25000, 10000 };

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
            case WorldObjectType.FoodShop: prompt = "[E] Beli roti"; break;
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
            if (gm.bread >= gm.foodCapacity) { gm.Notify("Slot makanan penuh!"); return; }
            if (gm.money < breadPrice) { gm.Notify("Saldo tidak cukup!"); return; }
            gm.money -= breadPrice;
            gm.bread++;
            gm.Notify("Membeli roti. Stok: " + gm.bread + "/" + gm.foodCapacity);
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
            case WorldObjectType.SortingBin: DrawSorting(); break;
            case WorldObjectType.RecyclingMachine: DrawMachine(); break;
            case WorldObjectType.SellPoint: DrawSell(); break;
        }
    }

    bool sendToMachine;

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
        if (pi < 0 || !gm.TryReserveProductSlots(count))
        { feedback = "Tas tidak cukup. Pindahkan barang ke bagasi."; yield break; }

        machineStock[(int)selectedTrash] -= count;
        processing = true;
        Close();
        yield return new WaitForSeconds(count * Mathf.Max(0.1f, processTimePerItem));
        gm.CompleteReservedProductProduction(pi, count);
        gm.Notify("Daur ulang selesai. " + GameManager.ProductNames[pi] + " x" + count + " masuk tas.");
        processing = false;
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

    void Window(string title, System.Action content)
    {
        float w = Mathf.Min(500f, Screen.width - 30f);
        float h = Mathf.Min(550f, Screen.height - 30f);
        GUILayout.BeginArea(new Rect((Screen.width-w)/2, (Screen.height-h)/2, w, h), GUI.skin.window);
        GUILayout.BeginVertical();
        GUILayout.Label(title);
        content();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Tutup", GUILayout.Height(32))) Close();
        GUILayout.EndVertical();
        GUILayout.EndArea();
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
