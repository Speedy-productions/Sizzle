using UnityEngine;
using Photon.Pun;

public class PlayerSetup : MonoBehaviourPun
{
    public GameObject playerCamera;
    public GameObject playerCanvas;

    void Start()
    {
        if (photonView.IsMine)
        {
            playerCamera.SetActive(true);
            playerCanvas.SetActive(true);     // ← Se activa solo AHORA
        }
        else
        {
            playerCamera.SetActive(false);
            playerCanvas.SetActive(false);
        }
    }
}
