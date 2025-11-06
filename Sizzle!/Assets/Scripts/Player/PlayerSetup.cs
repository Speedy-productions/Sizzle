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

        bool isMultiplayer = view != null && PhotonNetwork.IsConnected;
        bool isLocalPlayer = isMultiplayer ? view.IsMine : true; // En modo aventura, asumimos que siempre es local

        if (isLocalPlayer)
        {
            // --- Activar cámara y HUD ---
            if (playerCamera != null) playerCamera.SetActive(true);
            if (hud != null) hud.SetActive(true);

            // --- Asegurar la cámara principal ---
            Camera cam = playerCamera.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                cam.tag = "MainCamera";
                cam.enabled = true;

                var listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            // --- Activar scripts de control ---
            EnablePlayerControl(true);
        }
        else
        {
            // --- Jugador remoto (solo multijugador) ---
            if (playerCamera != null) playerCamera.SetActive(false);
            if (hud != null) hud.SetActive(false);
            EnablePlayerControl(false);
        }
    }

    void EnablePlayerControl(bool enable)
    {
        // Activar o desactivar scripts de control
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enable;

        var camControl = GetComponentInChildren<PlayerCam>(true);
        if (camControl != null) camControl.enabled = enable;

        var moveCam = GetComponentInChildren<MoveCamera>(true);
        if (moveCam != null) moveCam.enabled = enable;
    }
}
