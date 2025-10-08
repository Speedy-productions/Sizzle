using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }
    public FoodGroup[] foodGroups;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public Order GenerateHamburgerOrder()
    {
        int totalIngredients = 5;
        string[] ingredientNames = new string[totalIngredients];

        // Nombres del pan
        ingredientNames[0] = "Tapa";
        ingredientNames[totalIngredients - 1] = "Base";

        // Nombres de los ingredientes intermedios
        for (int i = 1; i < totalIngredients - 1; i++)
        {
            int ranChoice = Random.Range(0, 4);
            if (ranChoice == 1)
            {
                ingredientNames[i] = "Lechuga";
            }
            else if (ranChoice == 2)
            {
                ingredientNames[i] = "Tomate";
            }
            else
            {
                ingredientNames[i] = "Carne";
            }
        }
        return new Order(ingredientNames);
    }
}
