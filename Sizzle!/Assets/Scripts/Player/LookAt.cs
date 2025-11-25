using UnityEngine;

public class LookAt : MonoBehaviour
{
    public float rayDistance = 4f;
    public LayerMask interactableLayer;

    private IInteractive current;

    void Update()
    {
        DetectInteractables();
    }

    void DetectInteractables()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, interactableLayer))
        {
            Debug.Log($"[LOOK AT] Update -> DetectInteractables()");
            IInteractive interactable = hit.collider.GetComponent<IInteractive>();

            if (interactable != null)
            {
                if (current != interactable)
                {
                    if (current != null)
                        current.HideUI();

                    current = interactable;
                    current.ShowUI();
                }
                return;
            }
        }

        if (current != null)
        {
            current.HideUI();
            current = null;
        }
    }
}
