
using System.Collections;
using UnityEngine;

public class Machine : Interactable
{
    [Header("Kategori Mesin")]
    public WasteCategory machineCategory = WasteCategory.Inorganic;

    [Header("Pengaturan Produksi")]
    [Min(0.1f)]
    public float processTimePerItem = 1.5f;

    public static bool MenuOpen = false;

    private static Machine activeMachine;

    // Stok bahan baku yang sudah dipindahkan dari tempat pilah.
    private static readonly int[] machineStock =
        new int[GameManager.TrashTypeCount];

    public static int GetMachineStock(TrashType type)
    {
        int index = (int)type;
        if (index < 0 || index >= machineStock.Length)
            return 0;

        return machineStock[index];
    }

    public static bool AddToMachine(TrashType type, int amount)
    {
        int index = (int)type;
        if (index < 0 || index >= machineStock.Length || amount <= 0)
            return false;

        machineStock[index] += amount;
        return true;
    }

    private TrashType selectedType;
    private int selectedAmount = 1;

    private bool hasSelectedType = false;
    private bool processing = false;

    private string feedback = "";

    private readonly TrashType[] inorganicTypes =
    {
        TrashType.PlasticBottle,
        TrashType.PlasticCup,
        TrashType.Cardboard,
        TrashType.Paper,
        TrashType.MetalCan
    };

    private readonly TrashType[] organicTypes =
    {
        TrashType.FoodWaste,
        TrashType.DryLeaves
    };

    void Reset()
    {
        prompt = "[E] Gunakan mesin";
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        if (processing)
        {
            gm.Notify("Mesin sedang memproses sampah!");
            return;
        }

        if (MenuOpen ||
            SortingBin.MenuOpen ||
            SellPoint.MenuOpen)
            return;

        activeMachine = this;

        MenuOpen = true;
        hasSelectedType = false;
        selectedAmount = 1;
        feedback = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    TrashType[] GetAvailableTypes()
    {
        return machineCategory == WasteCategory.Inorganic
            ? inorganicTypes
            : organicTypes;
    }

    int CountCarried(TrashType type)
    {
        // Sumber stok mesin, bukan inventory tas pemain.
        return GetMachineStock(type);
    }

    int GetTotalCarried()
    {
        int total = 0;

        foreach (TrashType type in GetAvailableTypes())
        {
            total += GetMachineStock(type);
        }

        return total;
    }

    void OnGUI()
    {
        if (!MenuOpen || activeMachine != this)
            return;

        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        GUI.skin.box.fontSize = 18;
        GUI.skin.label.fontSize = 14;
        GUI.skin.button.fontSize = 14;

        float width = 560;
        float height = 640;

        float x = (Screen.width - width) / 2;
        float y = (Screen.height - height) / 2;

        GUI.Box(
            new Rect(x, y, width, height),
            "MESIN PENGOLAHAN " +
            machineCategory.ToString().ToUpper()
        );

        DrawInventoryInformation(gm, x, y);

        DrawTrashSelection(gm, x, y);

        if (hasSelectedType)
        {
            DrawProductionSettings(gm, x, y);
        }

        DrawCloseButton(x, y);
    }

    void DrawInventoryInformation(
        GameManager gm,
        float x,
        float y)
    {
        GUI.Label(
            new Rect(x + 25, y + 40, 510, 25),
            "Pilih sampah yang sudah dipindahkan dari tempat pilah"
        );

        GUI.Label(
            new Rect(x + 25, y + 65, 510, 25),
            "Kategori: " + machineCategory
        );

        GUI.Label(
            new Rect(x + 25, y + 90, 510, 25),
            "Total bahan di mesin: " + GetTotalCarried()
        );
    }

    void DrawTrashSelection(
        GameManager gm,
        float x,
        float y)
    {
        GUI.Label(
            new Rect(x + 25, y + 125, 510, 25),
            "PILIH JENIS SAMPAH"
        );

        TrashType[] types = GetAvailableTypes();

        for (int i = 0; i < types.Length; i++)
        {
            TrashType type = types[i];

            int stock = CountCarried(type);

            int productIndex = gm.GetProductIndex(type);

            string productName =
                productIndex >= 0 &&
                productIndex < GameManager.ProductNames.Length
                ? GameManager.ProductNames[productIndex]
                : "Tidak diketahui";

            int rowY = (int)y + 160 + i * 48;

            GUI.Label(
                new Rect(x + 30, rowY, 220, 25),
                GameManager.GetTrashName(type)
            );

            GUI.Label(
                new Rect(x + 250, rowY, 130, 25),
                "Stok: " + stock
            );

            GUI.enabled = stock > 0;

            if (GUI.Button(
                new Rect(x + 390, rowY - 3, 135, 32),
                hasSelectedType && selectedType == type
                    ? "DIPILIH"
                    : "PILIH"))
            {
                selectedType = type;
                selectedAmount = 1;
                hasSelectedType = true;
                feedback = "";
            }

            GUI.enabled = true;

            GUI.Label(
                new Rect(x + 30, rowY + 23, 350, 20),
                "Hasil: " + productName
            );
        }
    }

    void DrawProductionSettings(
        GameManager gm,
        float x,
        float y)
    {
        int available = CountCarried(selectedType);

        if (available <= 0)
        {
            hasSelectedType = false;
            feedback = "Stok sampah tidak tersedia!";
            return;
        }

        selectedAmount = Mathf.Clamp(
            selectedAmount,
            1,
            available
        );

        int productIndex = gm.GetProductIndex(selectedType);

        if (productIndex < 0 ||
            productIndex >= GameManager.ProductNames.Length)
            return;

        string productName =
            GameManager.ProductNames[productIndex];

        int startY = 420;

        GUI.Label(
            new Rect(x + 25, y + startY, 510, 25),
            "SAMPAH DIPILIH: " +
            GameManager.GetTrashName(selectedType)
        );

        GUI.Label(
            new Rect(x + 25, y + startY + 25, 510, 25),
            "Produk yang dihasilkan: " + productName
        );

        GUI.Label(
            new Rect(x + 25, y + startY + 55, 510, 25),
            "Jumlah yang diproses:"
        );

        if (GUI.Button(
            new Rect(x + 65, y + startY + 85, 65, 35),
            "-"))
        {
            selectedAmount = Mathf.Max(
                1,
                selectedAmount - 1
            );
        }

        GUI.Label(
            new Rect(x + 165, y + startY + 85, 100, 35),
            selectedAmount.ToString(),
            new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter
            }
        );

        if (GUI.Button(
            new Rect(x + 300, y + startY + 85, 65, 35),
            "+"))
        {
            selectedAmount = Mathf.Min(
                available,
                selectedAmount + 1
            );
        }

        float totalTime =
            selectedAmount *
            Mathf.Max(0.1f, processTimePerItem);

        GUI.Label(
            new Rect(x + 25, y + startY + 125, 510, 25),
            "Waktu proses: " +
            totalTime.ToString("0.0") +
            " detik"
        );

        GUI.enabled =
            available >= selectedAmount &&
            selectedAmount > 0;

        if (GUI.Button(
            new Rect(x + 25, y + startY + 155, 245, 40),
            "MULAI PROSES"))
        {
            StartProcessing();
        }

        GUI.enabled = true;

        if (GUI.Button(
            new Rect(x + 290, y + startY + 155, 245, 40),
            "BATAL PILIH"))
        {
            hasSelectedType = false;
            selectedAmount = 1;
            feedback = "";
        }

        if (!string.IsNullOrEmpty(feedback))
        {
            GUI.Label(
                new Rect(x + 25, y + startY + 200, 510, 25),
                feedback
            );
        }
    }

    void DrawCloseButton(float x, float y)
    {
        if (GUI.Button(
            new Rect(x + 180, y + 525, 200, 35),
            "TUTUP MESIN"))
        {
            CloseMenu();
        }
    }

    void StartProcessing()
    {
        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        if (!hasSelectedType)
        {
            feedback = "Pilih jenis sampah terlebih dahulu!";
            return;
        }

        int available = GetMachineStock(selectedType);

        if (available < selectedAmount ||
            selectedAmount <= 0)
        {
            feedback = "Stok mesin pilah tidak mencukupi!";
            return;
        }

        if (GameManager.GetCategory(selectedType) != machineCategory)
        {
            feedback = "Sampah tidak sesuai dengan mesin!";
            return;
        }

        int productIndex = gm.GetProductIndex(selectedType);

        if (productIndex < 0 ||
            productIndex >= gm.products.Length)
        {
            feedback = "Produk hasil daur ulang tidak valid!";
            return;
        }

        TrashType processingType = selectedType;
        int processingAmount = selectedAmount;

        // Kurangi bahan baku dari stok mesin, bukan dari tas pemain.
        int machineIndex = (int)processingType;
        if (machineIndex < 0 || machineIndex >= machineStock.Length ||
            machineStock[machineIndex] < processingAmount)
        {
            feedback = "Stok mesin pilah tidak mencukupi!";
            return;
        }

        machineStock[machineIndex] -= processingAmount;

        float duration =
            processingAmount *
            Mathf.Max(0.1f, processTimePerItem);

        CloseMenu();

        processing = true;

        StartCoroutine(
            ProcessTrash(
                processingType,
                processingAmount,
                duration
            )
        );
    }

    IEnumerator ProcessTrash(
        TrashType type,
        int amount,
        float duration)
    {
        GameManager gm = GameManager.I;

        if (gm != null)
        {
            gm.Notify(
                "Mesin sedang mengolah " +
                GameManager.GetTrashName(type) +
                " x" + amount + "..."
            );
        }

        yield return new WaitForSeconds(duration);

        gm = GameManager.I;

        if (gm != null)
        {
            int productIndex = gm.GetProductIndex(type);

            if (productIndex >= 0 &&
                productIndex < gm.products.Length)
            {
                // Satu sampah menghasilkan satu produk.
                gm.products[productIndex] += amount;

                string productName =
                    GameManager.ProductNames[productIndex];

                gm.Notify(
                    "Produksi selesai! " +
                    productName +
                    " x" + amount +
                    " berhasil dibuat."
                );
            }
            else
            {
                // Kembalikan bahan ke stok mesin jika pemetaan produk gagal.
                AddToMachine(type, amount);

                gm.Notify(
                    "Produksi gagal. Sampah dikembalikan ke stok mesin pilah."
                );
            }
        }

        processing = false;
    }

    void Update()
    {
        if (MenuOpen &&
            activeMachine == this &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
    }

    void CloseMenu()
    {
        MenuOpen = false;
        activeMachine = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
