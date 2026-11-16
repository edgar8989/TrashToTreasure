using UnityEngine;

public class TrashItem : Interactable
{
    [Header("Jenis Sampah")]
    public TrashType type = TrashType.PlasticBottle;

    void Reset()
    {
        prompt = "[E] Ambil sampah";
    }

    public override void Interact()
    {
        GameManager gm = GameManager.I;

        if (gm == null)
        {
            Debug.LogWarning("GameManager belum tersedia di Scene.");
            return;
        }

        if (!GameManager.IsValidTrashType(type))
        {
            gm.Notify("Jenis sampah pada objek belum valid!");
            Debug.LogWarning("TrashItem memiliki TrashType yang tidak valid: " + type);
            return;
        }

        if (!gm.TryAddCarried(type))
        {
            gm.Notify("Tas penuh! Kapasitas tas: " +
                gm.CarriedCount + " / " + gm.inventoryCapacity);
            return;
        }

        gm.Notify("Mengambil " + GameManager.GetTrashName(type) +
            " (" + gm.CarriedCount + " / " + gm.inventoryCapacity + ")");

        Destroy(gameObject);
    }
}
