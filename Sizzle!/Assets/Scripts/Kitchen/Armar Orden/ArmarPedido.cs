using System.Collections.Generic;
using UnityEngine;

public class ArmarPedido : MonoBehaviour
{
    [Header("Receta (para prefab final)")]
    public RecipeSO currentRecipe; // solo para saber qué prefab instanciar

    [Header("Opciones")]
    public bool requireExactOrder = true;   // Debe coincidir exactamente con el pedido activo
    public bool requireExactCount = true;   // No más ni menos ingredientes que el pedido

    public void SetCurrentRecipe(RecipeSO recipe) => currentRecipe = recipe;

    /// <summary>Se llama cada vez que se coloca un ingrediente en la mesa.</summary>
    public void OnIngredientPlaced(
    string ingredientName,
    List<GameObject> currentPlaced,
    RecipeSO recipeFromMesa,
    MesaArmado mesa // <- nueva referencia
)
    {
        if (recipeFromMesa != null)
            currentRecipe = recipeFromMesa;

        // Debe existir un pedido activo
        if (OrderManager.Instance == null || OrderManager.Instance.CurrentOrder == null)
            return;

        if (CheckIfOrderComplete(currentPlaced, OrderManager.Instance.CurrentOrder))
        {
            CombineIntoFinalDish(currentPlaced, mesa); // <- usamos la mesa pasada
        }
    }


    bool CheckIfOrderComplete(List<GameObject> placed, Order activeOrder)
    {
        if (activeOrder == null || activeOrder.ingredients == null) return false;

        var expected = activeOrder.ingredients;
        Debug.Log($"[ArmarPedido] placed={placed.Count} vs expected={expected.Length}");

        if (requireExactCount && placed.Count != expected.Length) return false;
        if (placed.Count < expected.Length) return false;

        for (int i = 0; i < expected.Length; i++)
        {
            var obj = placed[i];
            if (!obj) { Debug.Log("[ArmarPedido] obj null en index " + i); return false; }

            string have = ResolveName(obj);
            string want = NormalizeName(expected[i]);
            if (!string.Equals(have, want))
            {
                Debug.Log($"[ArmarPedido] MISMATCH idx {i}: have='{have}' want='{want}'");
                return false;
            }

            if (obj.TryGetComponent(out Ingredient ing) && !ing.IsReady())
            {
                Debug.Log($"[ArmarPedido] NOT READY idx {i}: '{have}'");
                return false;
            }

            if (obj.transform.parent == null)
            {
                Debug.Log($"[ArmarPedido] NOT ON TABLE idx {i}: '{have}'");
                return false;
            }
        }

        return true;
    }


    string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace("Sliced", "").Replace("Slice", "").Replace("Cortado", "");
        return name.Trim();
    }

    string ResolveName(GameObject obj)
    {
        if (obj.TryGetComponent(out Ingredient ing)) return NormalizeName(ing.ingredientName);
        if (obj.TryGetComponent(out SliceIngredient slice)) return NormalizeName(slice.ingredientName);
        if (obj.TryGetComponent(out MeatCookingState meat)) return NormalizeName(meat.meatName);
        return NormalizeName(obj.name);
    }

    void CombineIntoFinalDish(List<GameObject> placedIngredients, MesaArmado mesa)
    {
        if (currentRecipe == null || currentRecipe.finalProductPrefab == null)
        {
            Debug.LogWarning("[ArmarPedido] No hay RecipeSO o finalProductPrefab asignado.");
            return;
        }
        if (placedIngredients == null || placedIngredients.Count == 0) return;

        Vector3 spawnPos = placedIngredients[0].transform.position;

        // Asegurar carnes kinematic
        foreach (var ing in placedIngredients)
        {
            if (!ing) continue;
            if (ing.TryGetComponent(out MeatCookingState meat))
            {
                var rb = ing.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }

        // Destruir ingredientes y limpiar tracking
        foreach (var ing in placedIngredients)
            if (ing) Object.Destroy(ing);

        placedIngredients.Clear();

        // Reset visual/altura de la mesa
        if (mesa) mesa.ResetAfterComplete(false);

                // Instanciar producto final
        Object.Instantiate(currentRecipe.finalProductPrefab, spawnPos, Quaternion.identity);

        // ? Sonido al completar el platillo
        BurgerCompleteSound sfx = Object.FindFirstObjectByType<BurgerCompleteSound>();
        if (sfx != null) sfx.Play();

        Debug.Log($"[ArmarPedido] ¡Platillo completado!: {currentRecipe.recipeName}");
    }


}
