using UnityEngine;
using Photon.Pun;

public class OrderManager : MonoBehaviourPun
{
    public static OrderManager Instance { get; private set; }

    public FoodGroup[] foodGroups;

    // Pedido activo que seguirá la mesa/armado (HAMBURGUESA)
    public Order CurrentOrder { get; private set; }

    // Pedido exclusivo de PAPAS
    public OrderPapas CurrentFriesOrder { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Generamos la orden de hamburguesa
            CurrentOrder = GenerateHamburgerOrder();

            // ? 50% de probabilidad de papas
            bool wantsFries = Random.value < 0.5f;

            CurrentFriesOrder = wantsFries ? new OrderPapas() : null;

            // ? Sincronizamos hamburguesa + si hay papas o no
            photonView.RPC(
                nameof(RPC_SetCurrentOrderFull),
                RpcTarget.OthersBuffered,
                CurrentOrder.ingredients,
                wantsFries
            );
        }
    }


    [PunRPC]
void RPC_SetCurrentOrderFull(string[] ingredients, bool wantsFries)
{
    CurrentOrder = new Order(ingredients);
    CurrentFriesOrder = wantsFries ? new OrderPapas() : null;
}

    public void SetCurrentOrder(Order order)
{
    CurrentOrder = order;

    bool wantsFries = Random.value < 0.8f;
    CurrentFriesOrder = wantsFries ? new OrderPapas() : null;

    if (PhotonNetwork.IsMasterClient)
    {
        photonView.RPC(
            nameof(RPC_SetCurrentOrderFull),
            RpcTarget.OthersBuffered,
            order.ingredients,
            wantsFries
        );
    }

    string ingredientes = (order != null) ? string.Join(",", order.ingredients) : "NULL";
    Debug.Log("[OrderManager] CurrentOrder: " + ingredientes + " | Papas: " + wantsFries);
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
            if (ranChoice == 1)       ingredientNames[i] = "Lechuga";
            else if (ranChoice == 2)  ingredientNames[i] = "Tomate";
            else                      ingredientNames[i] = "Carne";
        }

        return new Order(ingredientNames);
    }
}
