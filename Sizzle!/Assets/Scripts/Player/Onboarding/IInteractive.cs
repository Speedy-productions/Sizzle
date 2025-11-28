using UnityEngine;

public class IInteractive : MonoBehaviour
{
    [Header("UI Canvases")]
    public GameObject primaryCanvas;   // Shows E action
    public GameObject secondaryCanvas; // Shows Q action

    [Header("Canvas to Open on E")]
    public GameObject canvasToOpen;    // Canvas que se abre al presionar E

    [Header("Settings")]
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode secondaryKey = KeyCode.Q;

    bool isPlayerLooking = false;

    void Update()
    {
        // Si el jugador está mirando y presiona E
        if (isPlayerLooking && Input.GetKeyDown(interactionKey))
        {
            OpenCanvas();
        }
        else if (isPlayerLooking && Input.GetKeyDown(secondaryKey))
        {
            CloseCanvas();
        }
    }

    public void ShowUI()
    {
        if (primaryCanvas != null)
            primaryCanvas.SetActive(true);

        if (secondaryCanvas != null)
            secondaryCanvas.SetActive(true);

        isPlayerLooking = true;
    }

    public void HideUI()
    {
        if (primaryCanvas != null)
            primaryCanvas.SetActive(false);

        if (secondaryCanvas != null)
            secondaryCanvas.SetActive(false);

        isPlayerLooking = false;
    }

    void OpenCanvas()
    {
        if (canvasToOpen != null)
        {
            canvasToOpen.SetActive(true);
            Debug.Log($"Canvas abierto: {canvasToOpen.name}");
        }
    }

    // Método público para cerrar el canvas desde otro script si es necesario
    public void CloseCanvas()
    {
        if (canvasToOpen != null)
        {
            canvasToOpen.SetActive(false);
        }
    }
}