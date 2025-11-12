using System.Collections.Generic;
using UnityEngine;

public class MesaArmado : MonoBehaviour
{
    [Header("Configuración")]
    public Transform assemblePoint;  // Punto donde se coloca cada ingrediente
    public Transform stackRoot;  // Contenedor fijo para los ingredientes apilados
    public float separationY = 0.01f;  // Separación vertical entre ingredientes

    [Header("Referencias")]
    public ArmarPedido armarPedido;  // Sistema que valida y combina los ingredientes
    public RecipeSO currentRecipe;  // Prefab final de la hamburguesa (si aplica)

    // Estado interno
    private List<GameObject> placedIngredients = new List<GameObject>();  // Ingredientes colocados en la mesa
    private Vector3 baseAssembleLocalPos;  // Posición base de ensamblaje
    private float currentTopY = 0f;  // Altura acumulada de los ingredientes

    void Awake()
    {
        if (assemblePoint == null)
        {
            Debug.LogError("[MesaArmado] Falta asignar assemblePoint.");
            enabled = false;
            return;
        }

        if (stackRoot == null) stackRoot = transform;  // Si no hay contenedor, usamos el propio objeto
        baseAssembleLocalPos = assemblePoint.localPosition;
        ResetStackHeight();
    }

    // Método para colocar un ingrediente en la mesa
    public void TryPlaceIngredientFromHand(Ingredient ing)
    {
        if (ing == null || ing.IsReady() == false) return;

        GameObject obj = ing.gameObject;

        if (placedIngredients.Contains(obj)) return;

        // Agregar el ingrediente a la mesa
        PrepareForTable(obj);

        // Ajustamos la altura de la mesa y posicionamos el ingrediente
        float h = GetWorldHeight(obj);
        assemblePoint.localPosition = baseAssembleLocalPos + Vector3.up * currentTopY;
        Vector3 topWorld = assemblePoint.position;

        // Reparentamos el ingrediente al contenedor fijo
        Vector3 Sw = WorldScaleUtils.GetOrInitWorldScaleMemory(obj.transform);
        WorldScaleUtils.ReparentKeepWorldScale(obj.transform, stackRoot, Sw);

        obj.transform.rotation = Quaternion.identity;
        obj.transform.position = topWorld;

        placedIngredients.Add(obj);
        currentTopY += h + separationY;
        UpdateAssemblePointY();
    }

    public void CreateCustomBurger()
    {
        if (placedIngredients.Count == 0)
        {
            Debug.LogError("[MesaArmado] No hay ingredientes en la mesa.");
            return;
        }

        List<string> ingredientNames = new List<string>();
        foreach (var ingredient in placedIngredients)
        {
            if (ingredient.TryGetComponent(out Ingredient ing))
            {
                ingredientNames.Add(ing.ingredientName);
            }
        }

        Order customOrder = new Order(ingredientNames.ToArray());
        OrderManager.Instance.SetCurrentOrder(customOrder);

        if (currentRecipe != null && currentRecipe.finalProductPrefab != null)
        {
            Vector3 spawnPos = placedIngredients[0].transform.position;
            GameObject burger = Instantiate(currentRecipe.finalProductPrefab, spawnPos, Quaternion.identity);

            Hamburguesa hamburguesaScript = burger.GetComponent<Hamburguesa>();
            if (hamburguesaScript != null)
            {
                hamburguesaScript.SetIngredientes(ingredientNames);
            }

            Debug.Log("[MesaArmado] ¡Hamburguesa personalizada creada!");
        }

        ClearMesa();
    }


    // Método para limpiar la mesa
    public void ClearMesa()
    {
        foreach (var obj in placedIngredients)
        {
            if (obj)
                Destroy(obj);
        }
        placedIngredients.Clear();
        ResetStackHeight();
    }

    // Método para verificar la altura de un objeto (usado para calcular la posición)
    float GetWorldHeight(GameObject obj)
    {
        var rends = obj.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return Mathf.Max(0.001f, b.size.y);
        }
        return Mathf.Max(0.001f, obj.transform.lossyScale.y);
    }

    // Ajuste de la altura de la mesa
    void ResetStackHeight()
    {
        currentTopY = 0f;
        UpdateAssemblePointY();
    }

    void UpdateAssemblePointY()
    {
        assemblePoint.localPosition = baseAssembleLocalPos + Vector3.up * currentTopY;
    }

    // Preparar el objeto (ingrediente) para la mesa
    void PrepareForTable(GameObject obj)
    {
        if (obj.TryGetComponent(out MeatCookingState meat))
        {
            meat.SetOnPan(false);
            meat.LockOnTable(true);
        }
        else
        {
            if (obj.TryGetComponent(out Rigidbody rb))
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
                c.isTrigger = false;
        }
    }
public void ResetAfterComplete(bool destroyChildrenUnderAssemblePoint = false)
    {
        // (normalmente no habrá hijos bajo assemblePoint, pero dejo la opción)
        if (destroyChildrenUnderAssemblePoint && assemblePoint)
        {
            for (int i = assemblePoint.childCount - 1; i >= 0; i--)
            {
                var ch = assemblePoint.GetChild(i);
                if (ch) Destroy(ch.gameObject);
            }
        }

        placedIngredients.Clear();
        currentTopY = 0f;
        assemblePoint.localPosition = baseAssembleLocalPos;
    }

    public bool Contains(GameObject obj)
    {
        return placedIngredients.Contains(obj);
    }

    public bool IsTopIngredient(GameObject obj)
    {
        if (placedIngredients.Count == 0) return false;
        return placedIngredients[placedIngredients.Count - 1] == obj;
    }

    public void RemoveIngredient(GameObject obj)
{
    if (obj == null) return;

    if (placedIngredients.Contains(obj))
    {
        placedIngredients.Remove(obj);

        // ✅ Reactivar físicas
        if (obj.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ✅ Quitar parent (liberarlo de la mesa)
        obj.transform.SetParent(null);

        // ✅ Asegurar que sus colliders vuelvan a estar activos
        foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        // ✅ Levantarlo un poquito para evitar que se quede pegado
        obj.transform.position += Vector3.up * 0.02f;

        Debug.Log($"[MesaArmado] Ingrediente '{obj.name}' retirado de la mesa (no destruido).");

        // ✅ Recalcular la altura de la pila después de quitarlo
        RecalculateStackHeight();
    }
}
private void RecalculateStackHeight()
{
    currentTopY = 0f;

    foreach (var ing in placedIngredients)
    {
        if (!ing) continue;
        float h = GetWorldHeight(ing);
        currentTopY += h + separationY;
    }

    UpdateAssemblePointY();
}



}
