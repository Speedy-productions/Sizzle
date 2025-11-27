using UnityEngine;

public class Order
{
    public string[] ingredients;

    public Order(string[] ingredientNames)
    {
        ingredients = ingredientNames;
    }
}

public class OrderPapas : Order
{
    public OrderPapas() : base(new string[] { "Papas" })
    {
    }
}
