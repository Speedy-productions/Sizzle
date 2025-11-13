using UnityEngine;
using TMPro;

public class DineroUI : MonoBehaviour
{
    public TMP_Text textoDinero;
    public int dineroActual = 0;


    void Start()
    {
        if (!UIBoot.Ready) return;
        ActualizarTexto();
    }

    public void AgregarDinero(int cantidad)
    {

        dineroActual += cantidad;
        ActualizarTexto();
    }

    public void QuitarDinero(int cantidad)
    {
        dineroActual = Mathf.Max(0, dineroActual - cantidad); // evita números negativos
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        textoDinero.text = "$" + dineroActual.ToString("N0"); // con separadores de miles
    }
}
