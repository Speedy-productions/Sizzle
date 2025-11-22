using System;
using UnityEngine;

public class AbrirTiendaTrigger : MonoBehaviour
{
    public Cambios_Menu uiManager;
    private bool jugadorDentro = false;

    void Update()
    {
        if (jugadorDentro && Input.GetKeyDown(KeyCode.E))
        {
            uiManager.AbrirTienda();
            Console.WriteLine("Tienda abierta");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = false;
            uiManager.AbrirTienda(); // o cerrar todo si prefieres
        }
    }
}
