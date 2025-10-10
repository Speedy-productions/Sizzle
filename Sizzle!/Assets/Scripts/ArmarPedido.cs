using System.Collections.Generic;
using UnityEngine;

public class ArmarPedido : MonoBehaviour
{
    [Header("Receta actual")]
    public RecipeSO currentRecipe;

    public void SetCurrentRecipe(RecipeSO recipe)
    {
        currentRecipe = recipe;
    }

    public void OnIngredientPlaced(string ingredientName, List<GameObject> currentPlaced)
    {
        if (currentRecipe == null) return;

        if (CheckIfRecipeComplete(currentPlaced))
        {
            CombineIntoFinalDish(currentPlaced);
        }
    }

    private bool CheckIfRecipeComplete(List<GameObject> placed)
    {
        List<string> placedNames = new List<string>();
        foreach (var obj in placed)
        {
            string name = GetIngredientName(obj);
            if (name != null && !placedNames.Contains(name))
                placedNames.Add(name);
        }

        foreach (var required in currentRecipe.requiredIngredients)
        {
            if (!placedNames.Contains(required))
                return false;
        }

        return true;
    }

    private string GetIngredientName(GameObject obj)
    {
        if (obj.TryGetComponent(out Ingredient ing)) return ing.ingredientName;
        if (obj.TryGetComponent(out SliceIngredient slice)) return slice.ingredientName;
        if (obj.TryGetComponent(out MeatCookingState meat)) return meat.meatName;
        return null;
    }

    private void CombineIntoFinalDish(List<GameObject> placedIngredients)
    {
        if (currentRecipe == null || currentRecipe.finalProductPrefab == null)
        {
            Debug.LogWarning("No hay receta o prefab final asignado.");
            return;
        }

        Vector3 spawnPos = placedIngredients[0].transform.position;

        // Activar kinematic en carnes antes de destruir
        foreach (var ing in placedIngredients)
        {
            if (ing.TryGetComponent(out MeatCookingState meat))
            {
                Rigidbody rb = ing.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }

        foreach (var ing in placedIngredients)
        {
            Destroy(ing);
        }
        placedIngredients.Clear();

        Instantiate(currentRecipe.finalProductPrefab, spawnPos, Quaternion.identity);

        Debug.Log($"¡Platillo completado!: {currentRecipe.recipeName}");
    }
}
