using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public FoodGroup[] foodGroups;

    // === NUEVO: pedido activo que seguirá la mesa/armado ===
    public Order CurrentOrder { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetCurrentOrder(Order order)
    {
        CurrentOrder = order;
        // Podrías disparar un evento aquí si lo necesitas
        // OnOrderChanged?.Invoke(order);
        Debug.Log($"[OrderManager] CurrentOrder seteado: {(order != null ? string.Join(",", order.ingredients) : "NULL")}");
    }

    public Order GenerateHamburgerOrder()
    {
        int totalIngredients = 5;
        string[] ingredientNames = new string[totalIngredients];

        // Panes -> ahora primero BASE y al final TAPA
        ingredientNames[0] = "Base";
        ingredientNames[totalIngredients - 1] = "Tapa";

        // Intermedios
        for (int i = 1; i < totalIngredients - 1; i++)
        {
            int ranChoice = Random.Range(0, 4);
            if (ranChoice == 1) ingredientNames[i] = "Lechuga";
            else if (ranChoice == 2) ingredientNames[i] = "Tomate";
            else ingredientNames[i] = "Carne";
        }

        return new Order(ingredientNames);
    }

}
