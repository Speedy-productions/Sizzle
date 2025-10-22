using UnityEngine;
using Photon.Pun;

public class PlayerSetup : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject playerCamera; // Prefab de cámara (hijo de Player)
    public GameObject hud;          // HUD del jugador

    private PhotonView view;

    void Awake()
    {
        view = GetComponent<PhotonView>();

        if (view.IsMine)
        {
            // --- Activar cámara y HUD solo para jugador local ---
            if (playerCamera != null) playerCamera.SetActive(true);
            if (hud != null) hud.SetActive(true);

            // Asegurar que la cámara tenga el tag y AudioListener correctos
            Camera cam = playerCamera != null ? playerCamera.GetComponentInChildren<Camera>() : null;
            if (cam != null)
            {
                cam.tag = "MainCamera";
                cam.enabled = true;

                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            // --- Asignar la cámara al HeadLook ---
            HeadLook headLook = GetComponentInChildren<HeadLook>();
            if (headLook != null && cam != null)
            {
                headLook.cameraTransform = cam.transform;
            }

            // --- Asignar la cámara al Blade ---
            Blade blade = GetComponentInChildren<Blade>();
            if (blade != null && cam != null)
            {
                blade.SetCamera(cam);
            }
        }
        else
        {
            // Desactivar cámara y HUD de los jugadores remotos
            if (playerCamera != null) playerCamera.SetActive(false);
            if (hud != null) hud.SetActive(false);
        }
    }
}
