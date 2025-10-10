using UnityEngine;

/// <summary>
/// Script ligero para ingredientes listos para el armado.
/// - Para carne, habilitar requireCookedMeat en el Inspector si se debe verificar cocción.
/// </summary>
public class Ingredient : MonoBehaviour
{
    [Header("Datos")]
    public string ingredientName = "Ingredient"; // ejemplo: "LechugaSliced", "TomateSliced", "Base", "Tapa", "Burger"

    [Header("Opciones especiales")]
    [Tooltip("Si la carne necesita estar cocida para ser válida en el armado, marcar true.")]
    public bool requireCookedMeat = false;

    /// <summary>
    /// Indica si el ingrediente está listo para usarse (si es carne revisa MeatCookingState).
    /// </summary>
    public bool IsReady()
    {
        if (!requireCookedMeat) return true;

        var meat = GetComponent<MeatCookingState>();
        if (meat == null) return false;

        return meat.currentState == MeatCookingState.CookingState.Cooked;
    }
}
