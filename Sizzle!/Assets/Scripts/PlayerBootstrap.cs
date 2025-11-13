using Photon.Pun;
using UnityEngine;

public class PlayerBootstrap : MonoBehaviour
{
    PhotonView view;

    void Awake()
    {
        view = GetComponent<PhotonView>();
    }

    void Start()
    {
        bool isMine = view && view.IsMine;

        // Activa tus componentes de control solo si es el jugador local
        var cam = GetComponentInChildren<PlayerCam>(true);
        if (cam) cam.gameObject.SetActive(isMine);

        var movement = GetComponent<PlayerMovement>();
        if (movement) movement.enabled = isMine;

        // Si tienes HUD local específico, actívalo aquí cuando isMine sea true
    }
}
