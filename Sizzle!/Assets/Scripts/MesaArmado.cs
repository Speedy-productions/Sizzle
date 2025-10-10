using System.Collections.Generic;
using UnityEngine;

public class MesaArmado : MonoBehaviour
{
    [Header("Configuración")]
    public Transform assemblePoint;        // Punto donde se apilan los ingredientes
    public float placementOffsetY = 0.05f; // Espacio vertical entre ingredientes

    [Header("Referencias")]
    public ArmarPedido armarPedido;        // Referencia al script de armado
    public RecipeSO currentRecipe;         // Receta actual

    private List<GameObject> placedIngredients = new List<GameObject>();

    /// <summary>
    /// Coloca un ingrediente que el jugador tiene en la mano en la mesa
    /// </summary>
    public void TryPlaceIngredientFromHand(Ingredient ing)
    {
        if (!ing) return;

        GameObject obj = ing.gameObject;

        // Evitar duplicados
        if (placedIngredients.Contains(obj)) return;

        // Activar Kinematic y desactivar gravedad
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        obj.transform.SetParent(assemblePoint);

        // Posicionar según el orden de la receta
        Vector3 newPos = assemblePoint.position;
        float offset = placementOffsetY;

        if (currentRecipe != null)
        {
            int index = currentRecipe.requiredIngredients.IndexOf(ing.ingredientName);
            if (index >= 0)
                newPos += Vector3.up * (index * offset);
            else
                newPos += Vector3.up * (placedIngredients.Count * offset);
        }
        else
        {
            newPos += Vector3.up * (placedIngredients.Count * offset);
        }

        obj.transform.position = newPos;
        obj.transform.rotation = Quaternion.identity;

        // Agregar a la lista
        placedIngredients.Add(obj);

        // Avisar al sistema de armado
        if (armarPedido != null)
            armarPedido.OnIngredientPlaced(ing.ingredientName, placedIngredients);

        Debug.Log($"Ingrediente colocado en mesa: {ing.ingredientName}");
    }

    /// <summary>
    /// Borra todos los ingredientes de la mesa
    /// </summary>
    public void ClearMesa()
    {
        foreach (var obj in placedIngredients)
        {
            Destroy(obj);
        }
        placedIngredients.Clear();
    }

    /// <summary>
    /// Obtiene la lista de ingredientes colocados en la mesa
    /// </summary>
    public List<GameObject> GetPlacedIngredients() => placedIngredients;
}
