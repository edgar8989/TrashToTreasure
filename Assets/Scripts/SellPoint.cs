
using UnityEngine;

public class SellPoint : Interactable
{
    [Header("Harga Produk")]
    public int[] prices =
    {
        15000, // Pot Tanaman
        20000, // Buku Catatan
        25000, // Wadah Logam
        10000  // Kompos
    };

    private static readonly int[] defaultPrices =
    {
        15000,
        20000,
        25000,
        10000
    };

    public static bool MenuOpen = false;

    private static SellPoint activeShop;

    private int selectedProduct = 0;
    private int selectedAmount = 1;

    private string feedback = "";

    // Memastikan harga tersedia saat objek diaktifkan.
    void Awake()
    {
        EnsurePrices();
    }

    // Memperbaiki konfigurasi harga di Unity Inspector.
    void OnValidate()
    {
        EnsurePrices();
    }

    void EnsurePrices()
    {
        if (prices == null)
        {
            prices = (int[])defaultPrices.Clone();
            return;
        }

        if (prices.Length != defaultPrices.Length)
        {
            int[] newPrices = (int[])defaultPrices.Clone();

            for (int i = 0; i < Mathf.Min(
                prices.Length,
                defaultPrices.Length); i++)
            {
                newPrices[i] = prices[i];
            }

            prices = newPrices;
        }

        // Memperbaiki harga yang belum diatur atau bernilai nol.
        for (int i = 0; i < prices.Length; i++)
        {
            if (prices[i] <= 0)
                prices[i] = defaultPrices[i];
        }
    }

    void Reset()
    {
        prompt = "[E] Jual produk";
        EnsurePrices();
    }

    public override void Interact()
    {
        if (MenuOpen ||
            SortingBin.MenuOpen ||
            Machine.MenuOpen)
            return;

        activeShop = this;
        MenuOpen = true;

        selectedProduct = 0;
        selectedAmount = 1;
        feedback = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (!MenuOpen || activeShop != this)
            return;

        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        EnsurePrices();

        GUI.skin.box.fontSize = 18;
        GUI.skin.label.fontSize = 14;
        GUI.skin.button.fontSize = 14;

        float width = 500;
        float height = 520;

        float x = (Screen.width - width) / 2;
        float y = (Screen.height - height) / 2;

        GUI.Box(
            new Rect(x, y, width, height),
            "TOKO DAUR ULANG"
        );

        DrawProductList(gm, x, y);
        DrawSelectedProduct(gm, x, y);
        DrawActionButtons(gm, x, y);

        if (!string.IsNullOrEmpty(feedback))
        {
            GUI.Label(
                new Rect(x + 25, y + 485, 450, 25),
                feedback
            );
        }
    }

    void DrawProductList(GameManager gm, float x, float y)
    {
        GUI.Label(
            new Rect(x + 25, y + 40, 450, 25),
            "Pilih produk yang ingin dijual:"
        );

        int productCount = Mathf.Min(
            gm.products.Length,
            GameManager.ProductNames.Length
        );

        for (int i = 0; i < productCount; i++)
        {
            int stock = gm.products[i];
            int price = prices[i];

            int rowY = (int)y + 75 + i * 55;

            GUI.Label(
                new Rect(x + 30, rowY, 285, 25),
                GameManager.ProductNames[i] +
                " | Stok: " + stock
            );

            GUI.Label(
                new Rect(x + 30, rowY + 25, 285, 22),
                "Rp" + price.ToString("N0") + " / buah"
            );

            GUI.enabled = stock > 0;

            if (GUI.Button(
                new Rect(x + 340, rowY + 5, 125, 35),
                selectedProduct == i ? "DIPILIH" : "PILIH"))
            {
                selectedProduct = i;
                selectedAmount = 1;
                feedback = "";
            }

            GUI.enabled = true;
        }
    }

    void DrawSelectedProduct(GameManager gm, float x, float y)
    {
        if (selectedProduct < 0 ||
            selectedProduct >= gm.products.Length ||
            selectedProduct >= prices.Length ||
            selectedProduct >= GameManager.ProductNames.Length)
            return;

        int available = gm.products[selectedProduct];

        if (available > 0)
        {
            selectedAmount = Mathf.Clamp(
                selectedAmount,
                1,
                available
            );
        }
        else
        {
            selectedAmount = 1;
        }

        int selectedPrice = prices[selectedProduct];

        int totalPrice = selectedPrice * selectedAmount;

        GUI.Label(
            new Rect(x + 25, y + 305, 450, 25),
            "Produk dipilih: " +
            GameManager.ProductNames[selectedProduct]
        );

        GUI.Label(
            new Rect(x + 25, y + 335, 450, 25),
            "Stok tersedia: " + available
        );

        if (GUI.Button(
            new Rect(x + 65, y + 370, 65, 35), "-"))
        {
            selectedAmount = Mathf.Max(
                1,
                selectedAmount - 1
            );
        }

        GUI.Label(
            new Rect(x + 165, y + 370, 100, 35),
            selectedAmount.ToString(),
            new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter
            }
        );

        GUI.enabled = selectedAmount < available;

        if (GUI.Button(
            new Rect(x + 300, y + 370, 65, 35), "+"))
        {
            selectedAmount = Mathf.Min(
                available,
                selectedAmount + 1
            );
        }

        GUI.enabled = true;

        GUI.Label(
            new Rect(x + 25, y + 415, 450, 25),
            "Total penjualan: Rp" +
            totalPrice.ToString("N0")
        );
    }

    void DrawActionButtons(GameManager gm, float x, float y)
    {
        if (selectedProduct < 0 ||
            selectedProduct >= gm.products.Length ||
            selectedProduct >= prices.Length)
            return;

        int available = gm.products[selectedProduct];

        GUI.enabled =
            available > 0 &&
            selectedAmount > 0 &&
            selectedAmount <= available &&
            prices[selectedProduct] > 0;

        if (GUI.Button(
            new Rect(x + 25, y + 450, 210, 35),
            "JUAL PRODUK"))
        {
            SellSelectedProduct();
        }

        GUI.enabled = true;

        if (GUI.Button(
            new Rect(x + 265, y + 450, 210, 35),
            "TUTUP"))
        {
            CloseMenu();
        }
    }

    void SellSelectedProduct()
    {
        GameManager gm = GameManager.I;

        if (gm == null)
            return;

        EnsurePrices();

        if (selectedProduct < 0 ||
            selectedProduct >= gm.products.Length ||
            selectedProduct >= prices.Length ||
            selectedProduct >= GameManager.ProductNames.Length)
        {
            feedback = "Produk tidak valid!";
            return;
        }

        int available = gm.products[selectedProduct];

        if (selectedAmount <= 0 ||
            available < selectedAmount)
        {
            feedback = "Stok produk tidak mencukupi!";
            return;
        }

        int price = prices[selectedProduct];

        if (price <= 0)
        {
            feedback = "Harga produk belum diatur!";
            return;
        }

        int earned = price * selectedAmount;

        // Kurangi stok produk.
        gm.products[selectedProduct] -= selectedAmount;

        // Tambahkan uang pemain.
        gm.money += earned;

        // Tambahkan total produk terjual.
        gm.totalSold += selectedAmount;

        gm.Notify(
            "Berhasil menjual " +
            GameManager.ProductNames[selectedProduct] +
            " x" + selectedAmount +
            "! +Rp" + earned.ToString("N0")
        );

        CloseMenu();
    }

    void Update()
    {
        if (MenuOpen &&
            activeShop == this &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
    }

    void CloseMenu()
    {
        MenuOpen = false;
        activeShop = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
