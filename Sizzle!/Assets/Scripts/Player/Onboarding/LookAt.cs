using UnityEngine;

public class LookAt : MonoBehaviour
{
    public float rayDistance = 4f;
    public GameObject canva;
    public LayerMask pickableLayer;

    private IInteractive current;

    void Update()
    {
        if (DetectIsPickable())
        {
            if (current != null)
            {
                current.HideUI();
                current = null;
            }
            return;
        }

        DetectInteractables();
    }

    void DetectInteractables()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
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

    bool DetectIsPickable()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, pickableLayer))
        {
            canva.SetActive(true);
            return true;
        }

        canva.SetActive(false);
        return false;
    }
}
