using UnityEngine;

public class RefrigeratorInteraction : MonoBehaviour
{
    public GameObject storageCanvas;
    public float activationDistance = 2f;
    private bool isPlayerNear = false;

    void Start()
    {
        storageCanvas.SetActive(false); // Inicio apagado
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            storageCanvas.SetActive(!storageCanvas.activeSelf);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            storageCanvas.SetActive(false);
        }
    }
}
