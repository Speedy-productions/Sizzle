using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Hamburguesa : MonoBehaviour
{
    [Header("Ingredientes de la hamburguesa")]
    public List<string> ingredientes;  // Lista de nombres de los ingredientes

    // Método para inicializar la hamburguesa con los ingredientes
    public void SetIngredientes(List<string> nuevosIngredientes)
    {
        ingredientes = nuevosIngredientes;
        Debug.Log("Hamburguesa creada con los ingredientes: " + string.Join(", ", ingredientes));
    }

    // Método para obtener los ingredientes de la hamburguesa
    public List<string> GetIngredientes()
    {
        return ingredientes;
    }
}
