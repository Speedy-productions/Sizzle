using Photon.Pun;
using UnityEngine;

public class CuttingBoard : MonoBehaviourPun
{
    public Transform posicionIngrediente;

    private SliceIngredient currentIngredient;

    public void TryPlaceIngredient(SliceIngredient ingrediente)
    {
        if (ingrediente == null) return;

        PhotonView ingredientePV = ingrediente.GetComponent<PhotonView>();
        if (ingredientePV != null && photonView != null)
        {
            photonView.RPC(nameof(RPC_PlaceIngredient), RpcTarget.AllBuffered, ingredientePV.ViewID);
        }
        else
        {
            PlaceIngredientLocal(ingrediente);
        }
    }

    [PunRPC]
    void RPC_PlaceIngredient(int ingredienteViewID)
    {
        PhotonView ingredientePV = PhotonView.Find(ingredienteViewID);
        if (ingredientePV == null) return;

        SliceIngredient ingrediente = ingredientePV.GetComponent<SliceIngredient>();
        if (ingrediente == null) return;

        PlaceIngredientLocal(ingrediente);
    }

    void PlaceIngredientLocal(SliceIngredient ingrediente)
    {
        currentIngredient = ingrediente;

        var rb = ingrediente.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        ingrediente.transform.SetParent(null);
        ingrediente.transform.position = posicionIngrediente.position;
        ingrediente.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        ingrediente.SetOnBoard(true);

        Debug.Log($"Ingrediente colocado ({ingrediente.name})");
    }

    public void RemoveIngredient()
    {
        if (currentIngredient == null) return;

        currentIngredient.SetOnBoard(false);
        currentIngredient.transform.SetParent(null);
        currentIngredient = null;
    }

    public bool HasIngredient() => currentIngredient != null;
    public SliceIngredient GetIngredient() => currentIngredient;

    public void ShowAimHint(bool show, bool _) { }
}
