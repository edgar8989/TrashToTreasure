
using UnityEngine;

public class SortingBin : Interactable
{
    [Header("Kategori Tempat Pemilahan")]
    public WasteCategory binCategory = WasteCategory.Inorganic;

    [Header("Kapasitas (0 = Tidak Terbatas)")]
    public int capacity = 0;

    public static bool MenuOpen = false;
    private static SortingBin activeBin;

    private enum BinAction
    {
        Deposit,
        TransferToMachine
    }

    private BinAction currentAction = BinAction.Deposit;

    private TrashType selectedType;
    private int selectedAmount = 1;
    private bool typeSelected = false;

    private string feedback = "";

    void Reset()
    {
        prompt = "[E] Buka tempat pemilahan";
    }

    public override void Interact()
    {
        if (MenuOpen || Machine.MenuOpen || SellPoint.MenuOpen)
            return;

        activeBin = this;
        MenuOpen = true;

        currentAction = BinAction.Deposit;
        typeSelected = false;
        selectedAmount = 1;
        feedback = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    TrashType[] GetTypesForBin()
    {
        if (binCategory == WasteCategory.Organic)
        {
            return new TrashType[]
            {
                TrashType.FoodWaste,
                TrashType.DryLeaves
            };
        }

        return new TrashType[]
        {
            TrashType.PlasticBottle,
            TrashType.PlasticCup,
            TrashType.Cardboard,
            TrashType.Paper,
            TrashType.MetalCan
        };
    }

    int GetAvailable(TrashType type)
    {
        GameManager gm = GameManager.I;

        if (gm == null)
            return 0;

        if (currentAction == BinAction.Deposit)
            return gm.CountCarried(type);

        int index = (int)type;

        if (index < 0 || index >= gm.sorted.Length)
            return 0;

        return gm.sorted[index];
    }

    int GetTotalBinStock(GameManager gm)
    {
        return gm.CountCategory(gm.sorted, binCategory);
    }

    void OnGUI()
    {
        if (!MenuOpen || activeBin != this)
            return;

        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        GUI.skin.box.fontSize = 18;
        GUI.skin.label.fontSize = 14;
        GUI.skin.button.fontSize = 14;

        float width = 560;
        float height = 570;

        float x = (Screen.width - width) / 2;
        float y = (Screen.height - height) / 2;

        GUI.Box(
            new Rect(x, y, width, height),
            "TEMPAT PILAH " + binCategory.ToString().ToUpper()
        );

        GUI.Label(
            new Rect(x + 25, y + 40, 510, 25),
            "Stok tersimpan: " + GetTotalBinStock(gm)
        );

        GUI.Label(
            new Rect(x + 25, y + 65, 510, 25),
            "Tas pemain: " + gm.carried.Count + " sampah"
        );

        DrawActionButtons(x, y);

        if (!typeSelected)
            DrawTypeList(x, y);
        else
            DrawAmountControls(gm, x, y);

        if (!string.IsNullOrEmpty(feedback))
        {
            GUI.Label(
                new Rect(x + 25, y + 530, 510, 25),
                feedback
            );
        }
    }

    void DrawActionButtons(float x, float y)
    {
        if (GUI.Button(
            new Rect(x + 25, y + 100, 245, 40),
            "MASUKKAN SAMPAH"))
        {
            currentAction = BinAction.Deposit;
            typeSelected = false;
            selectedAmount = 1;
            feedback = "";
        }

        if (GUI.Button(
            new Rect(x + 290, y + 100, 245, 40),
            "PINDAHKAN KE MESIN PILAH"))
        {
            currentAction = BinAction.TransferToMachine;
            typeSelected = false;
            selectedAmount = 1;
            feedback = "";
        }
    }

    void DrawTypeList(float x, float y)
    {
        GUI.Label(
            new Rect(x + 25, y + 150, 510, 25),
            currentAction == BinAction.Deposit
                ? "PILIH SAMPAH DARI TAS"
                : "PILIH STOK UNTUK DIPINDAHKAN KE MESIN"
        );

        TrashType[] types = GetTypesForBin();

        for (int i = 0; i < types.Length; i++)
        {
            TrashType type = types[i];

            int stock = GetAvailable(type);
            int rowY = (int)y + 185 + i * 48;

            GUI.Label(
                new Rect(x + 30, rowY, 260, 25),
                GameManager.GetTrashName(type)
            );

            GUI.Label(
                new Rect(x + 290, rowY, 85, 25),
                "Stok: " + stock
            );

            GUI.enabled = stock > 0;

            if (GUI.Button(
                new Rect(x + 400, rowY - 3, 125, 32),
                "PILIH"))
            {
                selectedType = type;
                selectedAmount = 1;
                typeSelected = true;
                feedback = "";
            }

            GUI.enabled = true;
        }

        if (GUI.Button(
            new Rect(x + 180, y + 490, 200, 35),
            "TUTUP"))
        {
            CloseMenu();
        }
    }

    void DrawAmountControls(
        GameManager gm,
        float x,
        float y)
    {
        int available = GetAvailable(selectedType);

        if (available <= 0)
        {
            typeSelected = false;
            feedback = "Stok sampah tidak tersedia!";
            return;
        }

        selectedAmount = Mathf.Clamp(
            selectedAmount,
            1,
            available
        );

        GUI.Label(
            new Rect(x + 25, y + 165, 510, 25),
            "Jenis: " + GameManager.GetTrashName(selectedType)
        );

        GUI.Label(
            new Rect(x + 25, y + 195, 510, 25),
            "Tersedia: " + available
        );

        GUI.Label(
            new Rect(x + 25, y + 230, 510, 25),
            "Jumlah yang ingin " +
            (currentAction == BinAction.Deposit
                ? "dimasukkan ke tempat pilah:"
                : "dipindahkan ke mesin pilah:")
        );

        if (GUI.Button(
            new Rect(x + 65, y + 275, 65, 40),
            "-"))
        {
            selectedAmount = Mathf.Max(
                1,
                selectedAmount - 1
            );
        }

        GUI.Label(
            new Rect(x + 165, y + 275, 100, 40),
            selectedAmount.ToString(),
            new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter
            }
        );

        if (GUI.Button(
            new Rect(x + 300, y + 275, 65, 40),
            "+"))
        {
            selectedAmount = Mathf.Min(
                available,
                selectedAmount + 1
            );
        }

        if (currentAction == BinAction.Deposit && capacity > 0)
        {
            int remaining = capacity - GetTotalBinStock(gm);

            GUI.Label(
                new Rect(x + 25, y + 330, 510, 25),
                "Kapasitas tersisa: " + Mathf.Max(0, remaining)
            );
        }

        string actionLabel = currentAction == BinAction.Deposit
            ? "MASUKKAN KE TEMPAT PILAH"
            : "PINDAHKAN KE MESIN PILAH";

        if (GUI.Button(
            new Rect(x + 25, y + 390, 245, 45),
            actionLabel))
        {
            ExecuteTransfer();
        }

        if (GUI.Button(
            new Rect(x + 290, y + 390, 245, 45),
            "KEMBALI"))
        {
            typeSelected = false;
            selectedAmount = 1;
            feedback = "";
        }

        if (GUI.Button(
            new Rect(x + 180, y + 455, 200, 35),
            "TUTUP"))
        {
            CloseMenu();
        }
    }

    void ExecuteTransfer()
    {
        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        if (GameManager.GetCategory(selectedType) != binCategory)
        {
            feedback = "Kategori sampah tidak sesuai!";
            return;
        }

        int index = (int)selectedType;

        if (currentAction == BinAction.Deposit)
            DepositFromPlayer(gm, index);
        else
            TransferToMachine(gm, index);
    }

    void DepositFromPlayer(GameManager gm, int index)
    {
        int available = gm.CountCarried(selectedType);

        if (available < selectedAmount)
        {
            feedback = "Sampah di tas tidak mencukupi!";
            return;
        }

        if (capacity > 0)
        {
            int remainingCapacity =
                capacity - GetTotalBinStock(gm);

            if (selectedAmount > remainingCapacity)
            {
                feedback = "Kapasitas tempat pilah tidak cukup!";
                return;
            }
        }

        int moved = 0;

        for (int i = gm.carried.Count - 1;
             i >= 0 && moved < selectedAmount; i--)
        {
            if (gm.carried[i] == selectedType)
            {
                gm.carried.RemoveAt(i);
                moved++;
            }
        }

        gm.sorted[index] += moved;

        gm.Notify(
            moved + " " +
            GameManager.GetTrashName(selectedType) +
            " masuk ke tempat pilah " + binCategory
        );

        CloseMenu();
    }

    void TransferToMachine(GameManager gm, int index)
    {
        if (index < 0 || index >= gm.sorted.Length)
        {
            feedback = "Data stok tempat pilah tidak valid!";
            return;
        }

        if (gm.sorted[index] < selectedAmount)
        {
            feedback = "Stok tempat pilah tidak mencukupi!";
            return;
        }

        // Pastikan kategori mesin sesuai dengan kategori tempat pilah.
        if (GameManager.GetCategory(selectedType) != binCategory)
        {
            feedback = "Kategori sampah tidak sesuai!";
            return;
        }

        // Masukkan stok ke persediaan mesin terlebih dahulu.
        if (!Machine.AddToMachine(selectedType, selectedAmount))
        {
            feedback = "Sampah gagal dipindahkan ke mesin pilah!";
            return;
        }

        // Kurangi stok tempat pilah. Sampah tidak kembali ke tas pemain.
        gm.sorted[index] -= selectedAmount;

        gm.Notify(
            selectedAmount + " " +
            GameManager.GetTrashName(selectedType) +
            " berhasil dipindahkan ke mesin pilah " + binCategory + "!"
        );

        CloseMenu();
    }

    void Update()
    {
        if (MenuOpen &&
            activeBin == this &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
    }

    void CloseMenu()
    {
        MenuOpen = false;
        activeBin = null;
        typeSelected = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
