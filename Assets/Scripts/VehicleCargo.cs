using UnityEngine;

public class VehicleCargo : Interactable
{
    [Header("Kapasitas Bagasi")]
    [Min(1)] public int capacity = 50;

    [Header("Stok Sampah di Bagasi")]
    public int[] cargo = new int[GameManager.TrashTypeCount];

    public static bool MenuOpen = false;
    private static VehicleCargo activeCargo;

    private enum TransferMode
    {
        PutIn,
        TakeOut
    }

    private TransferMode mode = TransferMode.PutIn;
    private TrashType selectedType;
    private int selectedAmount = 1;
    private bool hasSelection;
    private string feedback = "";

    private readonly TrashType[] displayTypes =
    {
        TrashType.PlasticBottle,
        TrashType.PlasticCup,
        TrashType.Cardboard,
        TrashType.Paper,
        TrashType.MetalCan,
        TrashType.FoodWaste,
        TrashType.DryLeaves
    };

    void Awake()
    {
        if (cargo == null || cargo.Length != GameManager.TrashTypeCount)
        {
            int[] resized = new int[GameManager.TrashTypeCount];
            if (cargo != null)
            {
                for (int i = 0; i < Mathf.Min(cargo.Length, resized.Length); i++)
                    resized[i] = cargo[i];
            }
            cargo = resized;
        }

        prompt = "[E] Buka bagasi mobil";
    }

    public override void Interact()
    {
        if (VehicleController.IsPlayerDriving)
        {
            if (GameManager.I != null)
                GameManager.I.Notify("Keluar dari mobil terlebih dahulu untuk membuka bagasi.");
            return;
        }

        if (MenuOpen || SortingBin.MenuOpen || Machine.MenuOpen || SellPoint.MenuOpen)
            return;

        activeCargo = this;
        MenuOpen = true;
        mode = TransferMode.PutIn;
        hasSelection = false;
        selectedAmount = 1;
        feedback = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    int TotalCargo()
    {
        int total = 0;
        if (cargo == null) return 0;
        foreach (int amount in cargo) total += Mathf.Max(0, amount);
        return total;
    }

    int GetAvailable(TrashType type)
    {
        GameManager gm = GameManager.I;
        if (gm == null) return 0;

        int index = (int)type;
        if (index < 0 || index >= cargo.Length) return 0;

        return mode == TransferMode.PutIn
            ? gm.CountCarried(type)
            : cargo[index];
    }

    void OnGUI()
    {
        if (!MenuOpen || activeCargo != this)
            return;

        GameManager gm = GameManager.I;
        if (gm == null) return;

        GUI.skin.box.fontSize = 18;
        GUI.skin.label.fontSize = 14;
        GUI.skin.button.fontSize = 14;

        float width = 560f;
        float height = 600f;
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height - height) / 2f;

        GUI.Box(new Rect(x, y, width, height), "BAGASI MOBIL");

        GUI.Label(new Rect(x + 25, y + 40, 510, 25),
            "Kapasitas: " + TotalCargo() + " / " + capacity);

        GUI.Label(new Rect(x + 25, y + 65, 510, 25),
            "Tas pemain: " + gm.CarriedCount + " / " + gm.inventoryCapacity);

        if (GUI.Button(new Rect(x + 25, y + 100, 245, 40),
            mode == TransferMode.PutIn ? "MASUKKAN KE MOBIL ✓" : "MASUKKAN KE MOBIL"))
        {
            mode = TransferMode.PutIn;
            hasSelection = false;
            selectedAmount = 1;
            feedback = "";
        }

        if (GUI.Button(new Rect(x + 290, y + 100, 245, 40),
            mode == TransferMode.TakeOut ? "AMBIL KE TAS ✓" : "AMBIL KE TAS"))
        {
            mode = TransferMode.TakeOut;
            hasSelection = false;
            selectedAmount = 1;
            feedback = "";
        }

        DrawItemList(gm, x, y);

        if (hasSelection)
            DrawAmountPanel(gm, x, y);

        if (!string.IsNullOrEmpty(feedback))
            GUI.Label(new Rect(x + 25, y + 555, 510, 25), feedback);

        if (GUI.Button(new Rect(x + 180, y + 565, 200, 30), "TUTUP BAGASI"))
            CloseMenu();
    }

    void DrawItemList(GameManager gm, float x, float y)
    {
        GUI.Label(new Rect(x + 25, y + 150, 510, 25),
            mode == TransferMode.PutIn
                ? "PILIH SAMPAH DARI TAS"
                : "PILIH SAMPAH DARI BAGASI");

        for (int i = 0; i < displayTypes.Length; i++)
        {
            TrashType type = displayTypes[i];
            int available = GetAvailable(type);
            int rowY = (int)y + 180 + i * 43;

            GUI.Label(new Rect(x + 30, rowY, 260, 25),
                GameManager.GetTrashName(type));

            GUI.Label(new Rect(x + 290, rowY, 90, 25),
                "Stok: " + available);

            GUI.enabled = available > 0;

            if (GUI.Button(new Rect(x + 405, rowY - 3, 120, 30), "PILIH"))
            {
                selectedType = type;
                selectedAmount = 1;
                hasSelection = true;
                feedback = "";
            }

            GUI.enabled = true;
        }
    }

    void DrawAmountPanel(GameManager gm, float x, float y)
    {
        int available = GetAvailable(selectedType);
        if (available <= 0)
        {
            hasSelection = false;
            return;
        }

        int remainingCapacity = capacity - TotalCargo();
        int maxAmount = mode == TransferMode.PutIn
            ? Mathf.Min(available, remainingCapacity)
            : Mathf.Min(available, gm.FreeInventorySpace);

        if (maxAmount <= 0)
        {
            feedback = mode == TransferMode.TakeOut
                ? "Tas pemain penuh! Kosongkan tas terlebih dahulu."
                : "Bagasi penuh!";
            hasSelection = false;
            return;
        }

        selectedAmount = Mathf.Clamp(selectedAmount, 1, maxAmount);

        GUI.Label(new Rect(x + 25, y + 495, 230, 25),
            GameManager.GetTrashName(selectedType));

        if (GUI.Button(new Rect(x + 265, y + 490, 45, 30), "-"))
            selectedAmount = Mathf.Max(1, selectedAmount - 1);

        GUI.Label(new Rect(x + 315, y + 495, 55, 25), selectedAmount.ToString());

        if (GUI.Button(new Rect(x + 375, y + 490, 45, 30), "+"))
            selectedAmount = Mathf.Min(maxAmount, selectedAmount + 1);

        GUI.enabled = maxAmount > 0;

        string action = mode == TransferMode.PutIn
            ? "SIMPAN KE BAGASI"
            : "AMBIL KE TAS";

        if (GUI.Button(new Rect(x + 25, y + 525, 245, 30), action))
            Transfer();

        GUI.enabled = true;

        if (GUI.Button(new Rect(x + 290, y + 525, 245, 30), "BATAL PILIH"))
        {
            hasSelection = false;
            selectedAmount = 1;
        }
    }

    void Transfer()
    {
        GameManager gm = GameManager.I;
        if (gm == null || !hasSelection) return;

        int index = (int)selectedType;
        int available = GetAvailable(selectedType);

        if (mode == TransferMode.PutIn)
        {
            int freeSpace = capacity - TotalCargo();
            if (selectedAmount <= 0 || selectedAmount > available || selectedAmount > freeSpace)
            {
                feedback = "Jumlah tidak valid atau kapasitas bagasi tidak cukup.";
                return;
            }

            int removed = 0;
            for (int i = gm.carried.Count - 1; i >= 0 && removed < selectedAmount; i--)
            {
                if (gm.carried[i] == selectedType)
                {
                    gm.carried.RemoveAt(i);
                    removed++;
                }
            }

            if (removed != selectedAmount)
            {
                for (int i = 0; i < removed; i++)
                    gm.carried.Add(selectedType);

                feedback = "Gagal memindahkan sampah dari tas.";
                return;
            }

            cargo[index] += selectedAmount;
            gm.Notify(selectedAmount + " " + GameManager.GetTrashName(selectedType) + " disimpan di bagasi.");
        }
        else
        {
            if (selectedAmount <= 0 || selectedAmount > cargo[index])
            {
                feedback = "Stok bagasi tidak mencukupi.";
                return;
            }

            if (selectedAmount > gm.FreeInventorySpace)
            {
                feedback = "Tas penuh! Ruang tersisa: " + gm.FreeInventorySpace +
                    " dari " + gm.inventoryCapacity;
                return;
            }

            if (!gm.TryAddCarried(selectedType, selectedAmount))
            {
                feedback = "Gagal memindahkan sampah ke tas.";
                return;
            }

            cargo[index] -= selectedAmount;
            gm.Notify(selectedAmount + " " + GameManager.GetTrashName(selectedType) +
                " dipindahkan ke tas.");
        }

        hasSelection = false;
        selectedAmount = 1;
        feedback = "Pemindahan berhasil.";
    }

    void Update()
    {
        if (MenuOpen && activeCargo == this && Input.GetKeyDown(KeyCode.Escape))
            CloseMenu();
    }

    void CloseMenu()
    {
        MenuOpen = false;
        activeCargo = null;
        hasSelection = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
