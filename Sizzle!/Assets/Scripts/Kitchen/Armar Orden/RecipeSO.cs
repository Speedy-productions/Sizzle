using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipes/RecipeSO")]
public class RecipeSO : ScriptableObject
{
    public string recipeName;
    public List<string> requiredIngredients; // Nombres de los ingredientes
    public GameObject finalProductPrefab;    // Prefab de la hamburguesa ya armada
}
