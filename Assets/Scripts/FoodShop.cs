using UnityEngine;

// Minimarket: membeli roti.
public class FoodShop : Interactable
{
    public int breadPrice = 5000;

    void Reset()
    {
        prompt = "[E] Beli Roti (Rp5.000)";
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;

        if (gm.money < breadPrice)
        {
            gm.Notify("Saldo tidak cukup!");
            return;
        }

        gm.money -= breadPrice;
        gm.bread++;
        gm.Notify("Membeli roti. Tekan F untuk makan.");
    }
}
