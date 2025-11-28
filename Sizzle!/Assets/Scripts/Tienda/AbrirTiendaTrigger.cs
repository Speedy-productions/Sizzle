using System;
using UnityEngine;

public class AbrirTiendaTrigger : MonoBehaviour
{
    public Cambios_Menu uiManager;
    private bool jugadorDentro = false;
    public static bool TiendaAbierta = false;

    private PlayerMovement movimientoJugador;

    void Update()
    {
        if (jugadorDentro && !TiendaAbierta && Input.GetKeyDown(KeyCode.E))
        {
            uiManager.AbrirTienda();
            TiendaAbierta = true;

            if (movimientoJugador != null)
                movimientoJugador.FreezeMovement(true); // congelar

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (TiendaAbierta && Input.GetKeyDown(KeyCode.Q))
            CerrarTienda();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = true;
            if (movimientoJugador == null)
                movimientoJugador = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = false;
            if (TiendaAbierta)
                CerrarTienda();
        }
    }

    private void CerrarTienda()
    {
        if (uiManager != null)
            uiManager.CerrarTienda();

        TiendaAbierta = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (movimientoJugador != null)
            movimientoJugador.FreezeMovement(false); // descongelar
    }
}
