using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientacion;

    float rotacionX;
    float rotacionY;

    private PhotonView view;

    private void Start()
    {
        view = GetComponentInParent<PhotonView>();
        if (!view.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    private void Update()
    {
        if (!view.IsMine) return;

        // Si tienda o pausa: liberar cursor y no rotar cámara
        if (AbrirTiendaTrigger.TiendaAbierta || PauseMenu.GameIsPaused)
        {
            DesbloquearCursor();
            return;
        }

        // Input del mouse
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;

        rotacionY += mouseX;

        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -75f, 90f); // Limitaciones en los ejes Y y Z

        transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0);
        orientacion.rotation = Quaternion.Euler(0, rotacionY, 0);

        BloquearCursor();
    }

    public void BloquearCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DesbloquearCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
