using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [TextArea] public string prompt = "[E] Interaksi";
    public abstract void Interact();
}
