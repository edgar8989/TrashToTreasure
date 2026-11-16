using UnityEngine;

// Induk untuk semua objek yang dapat diinteraksi.
public abstract class Interactable : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public string prompt = "[E] Interaksi";

    public abstract void Interact();
}
