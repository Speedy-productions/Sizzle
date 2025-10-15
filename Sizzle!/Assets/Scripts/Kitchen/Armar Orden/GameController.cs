using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Referencias")]
    public ArmarPedido armarPedido;      // Asigna en el Inspector
    public RecipeSO recetaActual;        // Asigna la receta (por ejemplo, la de hamburguesa)

    void Start()
    {
        if (armarPedido == null)
        {
            Debug.LogWarning("No se asignó ArmarPedido en GameController.");
            return;
        }

        if (recetaActual == null)
        {
            Debug.LogWarning("No se asignó una receta en GameController.");
            return;
        }

        armarPedido.SetCurrentRecipe(recetaActual);
        Debug.Log($"Receta actual configurada: {recetaActual.recipeName}");
    }
}
