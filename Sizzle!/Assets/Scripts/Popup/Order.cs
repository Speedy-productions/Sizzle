using UnityEngine;

[System.Serializable]
public class Order
{
    public string[] ingredients;

    public Order(string[] ingredients)
    {
        this.ingredients = ingredients;
    }
}