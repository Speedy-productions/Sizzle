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

    // ?? No permitir colocar si ya hay uno (pero NO afecta cortar)
    if (currentIngredient != null)
    {
        // PERO si el que intenta poner es EL MISMO ingrediente que ya está,
        // no retornamos — permitimos cortar.
        if (currentIngredient == ingrediente)
            return;

        Debug.LogWarning("[CuttingBoard] Ya hay un ingrediente en la tabla.");
        return;
    }

    PhotonView pv = ingrediente.GetComponent<PhotonView>();

    if (PhotonNetwork.IsConnected && pv != null && pv.ViewID > 0)
    {
        photonView.RPC(nameof(RPC_PlaceIngredient), RpcTarget.AllBuffered, pv.ViewID);
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

    // ?? Si ya hay ingrediente Y NO ES EL MISMO, NO permitir reemplazo.
    if (currentIngredient != null && currentIngredient != ingrediente)
        return;

    // Si es el mismo, continuar (esto permite que ambos jugadores lo corten).

    if (currentIngredient == null)
        PlaceIngredientLocal(ingrediente);
}

void PlaceIngredientLocal(SliceIngredient ingrediente)
{
    // Si ya está en tabla y es el mismo, no recolocar, solo dejarlo
    if (currentIngredient == ingrediente)
        return;

    currentIngredient = ingrediente;

    var rb = ingrediente.GetComponent<Rigidbody>();
    if (rb)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    ingrediente.transform.SetParent(transform);
    ingrediente.transform.position = posicionIngrediente.position;
    ingrediente.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

    ingrediente.SetOnBoard(true);

    Debug.Log($"Ingrediente colocado en tabla: {ingrediente.name}");
}


[PunRPC]
void RPC_RemoveIngredient(int ingredientViewID)
{
    PhotonView pv = PhotonView.Find(ingredientViewID);
    if (pv == null) return;

    SliceIngredient ing = pv.GetComponent<SliceIngredient>();
    if (ing == null) return;

    // Desmarcar
    ing.SetOnBoard(false);

    // Quitar parent
    ing.transform.SetParent(null);

    // Reactivar físicas
    Rigidbody rb = ing.GetComponent<Rigidbody>();
    if (rb)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Colliders normales
    foreach (var c in ing.GetComponentsInChildren<Collider>())
        c.isTrigger = false;

    Debug.Log("[CuttingBoard] Ingredient removed via RPC");

    if (currentIngredient == ing)
        currentIngredient = null;
}


    public void RemoveIngredient()
{
    if (currentIngredient == null) return;

    PhotonView pv = currentIngredient.GetComponent<PhotonView>();

    if (PhotonNetwork.IsConnected && pv != null)
    {
        photonView.RPC(nameof(RPC_RemoveIngredient), RpcTarget.AllBuffered, pv.ViewID);
    }
    else
    {
        // Sin Photon ? singleplayer
        LocalRemove(currentIngredient);
    }
}

void LocalRemove(SliceIngredient ing)
{
    ing.SetOnBoard(false);
    ing.transform.SetParent(null);

    Rigidbody rb = ing.GetComponent<Rigidbody>();
    if (rb)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    foreach (var c in ing.GetComponentsInChildren<Collider>())
        c.isTrigger = false;

    currentIngredient = null;
}


    public bool HasIngredient() => currentIngredient != null;
    public SliceIngredient GetIngredient() => currentIngredient;

    // (Opcional) visual hint
    public void ShowAimHint(bool show, bool _) { /* implementar si tienes UI para la tabla */ }
}
