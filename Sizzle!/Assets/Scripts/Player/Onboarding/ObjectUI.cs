using UnityEngine;

public class ObjectUI : MonoBehaviour
{
    public GameObject handCanvas;
    public Interact interactScript;

    void Update()
    {
        if (interactScript.ObjetosEnMano() > 0)
        {
            handCanvas.SetActive(true);
        }
        else
        {
            handCanvas.SetActive(false);
        }
    }
}
