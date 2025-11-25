using UnityEngine;

public class IInteractive : MonoBehaviour
{
    [Header("UI Canvases")]
    public GameObject primaryCanvas;   // Shows E action
    public GameObject secondaryCanvas; // Shows Q action

    public void ShowUI()
    {
        if (primaryCanvas != null)
            primaryCanvas.SetActive(true);

        if (secondaryCanvas != null)
            secondaryCanvas.SetActive(true);
    }

    public void HideUI()
    {
        if (primaryCanvas != null)
            primaryCanvas.SetActive(false);

        if (secondaryCanvas != null)
            secondaryCanvas.SetActive(false);
    }
}