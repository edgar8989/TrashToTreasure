
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float range = 3f;

    void Update()
    {
        if (GameManager.I == null)
            return;

        if (SortingBin.MenuOpen ||
            Machine.MenuOpen ||
            SellPoint.MenuOpen ||
            VehicleCargo.MenuOpen)
        {
            GameManager.I.prompt = "";
            return;
        }

        GameManager.I.prompt = "";

        Ray ray = new Ray(transform.position, transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(hits, (a, b) =>
            a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Interactable interactable =
                hit.collider.GetComponentInParent<Interactable>();

            if (interactable == null)
                continue;

            GameManager.I.prompt = interactable.prompt;

            if (Input.GetKeyDown(KeyCode.E))
                interactable.Interact();

            break;
        }
    }
}
