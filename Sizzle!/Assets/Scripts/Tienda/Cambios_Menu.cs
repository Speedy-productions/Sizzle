using UnityEngine;

public class Cambios_Menu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelTienda;
    public GameObject panelSuministros;
    public GameObject panelMejoras;

    private GameObject panelActual;

    void Start()
    {
        // Al iniciar, todo oculto
        CerrarTodos();
        DesactivarCursor();
    }

    // Eliminado Escape aquí para evitar duplicar (lo maneja AbrirTiendaTrigger)

    public void AbrirTienda()
    {
        CerrarTodos();
        panelTienda.SetActive(true);
        panelActual = panelTienda;
        ActivarCursor();
    }

    public void AbrirSuministros()
    {
        CerrarTodos();
        panelSuministros.SetActive(true);
        panelActual = panelSuministros;
        ActivarCursor();
    }

    public void AbrirMejoras()
    {
        CerrarTodos();
        panelMejoras.SetActive(true);
        panelActual = panelMejoras;
        ActivarCursor();
    }

    public void Volver()
    {
        if (panelActual == panelSuministros || panelActual == panelMejoras)
            AbrirTienda();
    }

    public void CerrarTienda()
    {
        CerrarTodos();
        panelActual = null;
        DesactivarCursor();
    }

    private void CerrarTodos()
    {
        panelTienda.SetActive(false);
        panelSuministros.SetActive(false);
        panelMejoras.SetActive(false);
    }

    private void ActivarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DesactivarCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
