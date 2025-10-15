using UnityEngine;

public class PruebaDinero : MonoBehaviour
{
    private DineroUI dineroUI;

    void Start()
    {
        dineroUI = FindFirstObjectByType<DineroUI>();
    }

    void Update()
    {
        // Suma 10 con la tecla E
        if (Input.GetKeyDown(KeyCode.M))
            dineroUI.AgregarDinero(10);

        // Resta 5 con la tecla Q
        if (Input.GetKeyDown(KeyCode.N))
            dineroUI.QuitarDinero(5);
    }
}
