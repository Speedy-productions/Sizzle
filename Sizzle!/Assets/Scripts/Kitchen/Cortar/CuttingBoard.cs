using Photon.Pun;
using UnityEngine;

public class CuttingBoard : MonoBehaviourPun
{
    [Tooltip("Empty transform donde se colocará el ingrediente (hijo de la tabla).")]
    public Transform posicionIngrediente;

    private SliceIngredient currentIngredient;

    public void TryPlaceIngredient(SliceIngredient ingrediente)
    {
        if (ingrediente == null) return;

        PhotonView ingredientePV = ingrediente.GetComponent<PhotonView>();
        if (ingredientePV != null && photonView != null)
        {
            // Enviamos a todos los jugadores la acción de colocar el ingrediente
            photonView.RPC(nameof(RPC_PlaceIngredient), RpcTarget.AllBuffered, ingredientePV.ViewID);
        }
        else
        {
            // fallback local
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

        Debug.Log($"Ingrediente colocado en la tabla ({ingrediente.name}).");
    }



    // Llamar cuando el ingrediente se quite o se corte
    public void RemoveIngredient()
    {
        if (currentIngredient == null) return;

        // desmarcar y quitar parent
        currentIngredient.SetOnBoard(false);
        // no forzamos reposicionar; lo hará quien lo agarre
        currentIngredient.transform.SetParent(null);
        currentIngredient = null;
    }

    public bool HasIngredient() => currentIngredient != null;
    public SliceIngredient GetIngredient() => currentIngredient;

    // (Opcional) visual hint
    public void ShowAimHint(bool show, bool _) { /* implementar si tienes UI para la tabla */ }
}
