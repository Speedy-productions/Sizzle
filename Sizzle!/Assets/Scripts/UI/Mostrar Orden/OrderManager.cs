using UnityEngine;
using Photon.Pun;

public class OrderManager : MonoBehaviourPun
{
    public static OrderManager Instance { get; private set; }

    public FoodGroup[] foodGroups;

    // Pedido activo que seguirá la mesa/armado
    public Order CurrentOrder { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CurrentOrder = GenerateHamburgerOrder();
            photonView.RPC(nameof(RPC_SetCurrentOrder), RpcTarget.OthersBuffered, CurrentOrder.ingredients);
        }
    }

    [PunRPC]
    void RPC_SetCurrentOrder(string[] ingredients)
    {
        CurrentOrder = new Order(ingredients);
    }

    public void SetCurrentOrder(Order order)
    {
        CurrentOrder = order;
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SetCurrentOrder), RpcTarget.OthersBuffered, order.ingredients);
        }
        Debug.Log($"[OrderManager] CurrentOrder seteado: {(order != null ? string.Join(",", order.ingredients) : "NULL")}");
    }

    public Order GenerateHamburgerOrder()
    {
        int totalIngredients = 5;
        string[] ingredientNames = new string[totalIngredients];
        ingredientNames[0] = "Base";
        ingredientNames[totalIngredients - 1] = "Tapa";

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
