using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BandejaArmado : MonoBehaviourPun
{
    [Header("Spots de la bandeja (armado)")]
    public Transform hamburguesaSpot;
    public Transform papasSpot;

    [Header("Prefabs finales")]
    public GameObject bandejaFinalPrefab;             // burger + (opcional) papas
    public GameObject bandejaFinalSoloBurgerPrefab;   // solo burger

    [Header("Finalizar (seguridad)")]
    [Tooltip("Distancia máxima del jugador a la bandeja para poder finalizar (Q).")]
    public float finalizeDistance = 2.2f;

    [Header("Capa para impedir re-agarrar ítems ya colocados")]
    [Tooltip("Usa una capa no pickable, por ejemplo Ignore Raycast (2) o una capa propia.")]
    public int unpickableLayer = 2; // 2 = Ignore Raycast por defecto

    // --- ESTADO LOCAL ---
    private Hamburguesa hamburguesaActual;
    private FriesCookingState papasActual;

    // --- ESTADO DE RED (visible para todos) ---
    private bool hasBurgerNet = false;
    private bool hasFriesNet  = false;
    private string[] cachedIngredientes = null; // ingredientes de la hamburguesa colocada

    private bool _finalizada = false;

    // ========================= UTIL =========================

    void FreezeForTray(Transform root, bool makeUnpickable)
    {
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            col.isTrigger = true;

        if (makeUnpickable)
        {
            SetLayerRecursively(root.gameObject, unpickableLayer);
        }
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0) return;
        var stack = new Stack<Transform>();
        stack.Push(go.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }

    void ReparentKeepWorld(Transform child, Transform newParent)
    {
        Vector3 wPos = child.position;
        Quaternion wRot = child.rotation;
        Vector3 wScale = child.lossyScale;

        child.SetParent(newParent, worldPositionStays: false);
        child.position = wPos;
        child.rotation = wRot;

        Vector3 pScale = newParent.lossyScale;
        child.localScale = new Vector3(
            pScale.x != 0f ? wScale.x / pScale.x : wScale.x,
            pScale.y != 0f ? wScale.y / pScale.y : wScale.y,
            pScale.z != 0f ? wScale.z / pScale.z : wScale.z
        );
    }

    // ===================== COLOCAR ÍTEMS =====================

    public void ColocarHamburguesa(Hamburguesa h)
{
    if (hasBurgerNet || h == null) return;

    PhotonView pv = h.GetComponent<PhotonView>();
    if (pv != null && !pv.IsMine) pv.RequestOwnership();

    hamburguesaActual = h; // local
    var list = h.GetIngredientes();
    string[] ingredientes = (list != null) ? list.ToArray() : System.Array.Empty<string>();
    cachedIngredientes = ingredientes; // cache local

    // 1) Adjuntar visualmente en todos
    photonView.RPC(nameof(RPC_AttachToSpot), RpcTarget.AllBuffered, pv ? pv.ViewID : -1, true);

    // 2) Difundir burger + ingredientes como string para evitar null-params en RPC
    string joined = (ingredientes != null && ingredientes.Length > 0) ? string.Join("|", ingredientes) : string.Empty;
    photonView.RPC(nameof(RPC_SetBurgerData), RpcTarget.AllBuffered, joined);
}


    public void ColocarPapas(FriesCookingState p)
    {
        if (hasFriesNet || p == null) return;

        PhotonView pv = p.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) pv.RequestOwnership();

        papasActual = p; // local

        // Adjuntar visualmente en todos
        photonView.RPC(nameof(RPC_AttachToSpot), RpcTarget.AllBuffered, pv ? pv.ViewID : -1, false);

        // Marcar estado de red
        photonView.RPC(nameof(RPC_SetHasFries), RpcTarget.AllBuffered, true);
    }

    [PunRPC]
void RPC_SetBurgerData(string joined)
{
    hasBurgerNet = true;
    if (string.IsNullOrEmpty(joined))
        cachedIngredientes = System.Array.Empty<string>();
    else
        cachedIngredientes = joined.Split('|');
}


    [PunRPC]
    void RPC_SetHasFries(bool has)
    {
        hasFriesNet = has;
    }

    [PunRPC]
    void RPC_AttachToSpot(int itemViewID, bool isBurger)
    {
        Transform spot = isBurger ? hamburguesaSpot : papasSpot;
        if (!spot) return;

        // Si tenemos referencia local (dueño que llamó), ya se setearon arriba.
        // Aquí resolvemos para TODOS con el PV recibido.
        if (itemViewID >= 0)
        {
            PhotonView itemPV = PhotonView.Find(itemViewID);
            if (!itemPV) return;

            Transform it = itemPV.transform;
            ReparentKeepWorld(it, spot);
            it.localPosition = Vector3.zero;
            it.localRotation = Quaternion.identity;
            FreezeForTray(it, makeUnpickable: true);

            if (isBurger)
                hamburguesaActual = it.GetComponent<Hamburguesa>();
            else
            {
                var fries = it.GetComponent<FriesCookingState>();
                if (fries) { fries.isInFryer = false; fries.enabled = false; }
                papasActual = fries;
            }
            return;
        }

        // Fallback singleplayer
        Transform t = (isBurger ? hamburguesaActual?.transform : papasActual?.transform);
        if (!t) return;
        ReparentKeepWorld(t, spot);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        FreezeForTray(t, makeUnpickable: true);
        if (!isBurger)
        {
            var fries = t.GetComponent<FriesCookingState>();
            if (fries) { fries.isInFryer = false; fries.enabled = false; }
        }
    }

    // ===================== FINALIZAR DESDE JUGADOR =====================

    // Llamado por Interact SOLO si el jugador está apuntando a esta bandeja (UX)
   public void TryFinalizeFromPlayer(Vector3 requesterPosition, int requesterActorNumber)
{
    if (_finalizada) return;

    // Validación de red: debe haber algo en la bandeja
    if (!hasBurgerNet && !hasFriesNet) return;

    // Validación cliente (distancia)
    if (Vector3.Distance(requesterPosition, transform.position) > finalizeDistance)
        return;

    _finalizada = true; // bloquear doble click local

    // 🔴 IMPORTANTE: usar string join para enviar por RPC
    string joinedIngredientes = (cachedIngredientes != null && cachedIngredientes.Length > 0)
        ? string.Join("|", cachedIngredientes)
        : string.Empty;

    bool incluyePapas = hasFriesNet;

    string prefabName = (hasBurgerNet && !hasFriesNet && bandejaFinalSoloBurgerPrefab)
        ? bandejaFinalSoloBurgerPrefab.name
        : bandejaFinalPrefab.name;

    int burgerID = (hamburguesaActual && hamburguesaActual.TryGetComponent(out PhotonView pvH)) ? pvH.ViewID : -1;
    int friesID  = (papasActual && papasActual.TryGetComponent(out PhotonView pvP)) ? pvP.ViewID : -1;

    Vector3 pos = transform.position;
    Quaternion rot = transform.rotation;

    photonView.RPC(nameof(RPC_Finalize_Master), RpcTarget.MasterClient,
        prefabName, pos, rot,
        joinedIngredientes, incluyePapas,    // <-- MANDAMOS STRING, NO ARRAY
        burgerID, friesID, photonView.ViewID,
        requesterActorNumber, requesterPosition);
}


    // ===================== MASTER: INSTANCIA Y LIMPIA =====================

    [PunRPC]
void RPC_Finalize_Master(string prefabName, Vector3 pos, Quaternion rot,
                         string joinedIngredientes, bool incluyePapas,  // <-- STRING AQUÍ
                         int burgerViewID, int friesViewID, int trayViewID,
                         int requesterActorNumber, Vector3 requesterPosition)
{
    if (!PhotonNetwork.IsMasterClient) return;

    var trayPV = PhotonView.Find(trayViewID);
    var tray   = trayPV ? trayPV.GetComponent<BandejaArmado>() : null;
    if (tray == null) return;

    // Revalidar distancia
    if (Vector3.Distance(requesterPosition, tray.transform.position) > tray.finalizeDistance + 0.25f)
    {
        tray._finalizada = false;
        return;
    }

    // InstantiationData también como STRING join-eado
    object[] data = new object[] { joinedIngredientes ?? string.Empty, incluyePapas };

    GameObject go = PhotonNetwork.Instantiate(prefabName, pos, rot, 0, data);

    // Ownership al solicitante
    if (go.TryGetComponent(out PhotonView newPV))
        newPV.TransferOwnership(requesterActorNumber);

    // Limpiar originales
    MasterDestroyView(burgerViewID);
    MasterDestroyView(friesViewID);
    MasterDestroyView(trayViewID);
}


    // Si el owner sigue conectado, le pedimos que destruya su objeto.
    // Si el owner ya no está, el Master lo destruye directamente.
    void MasterDestroyView(int viewID)
    {
        if (viewID <= 0) return;
        var pv = PhotonView.Find(viewID);
        if (!pv) return;

        Player owner = pv.Owner; // null si el owner salió

        // Owner ausente o Master (local): destruir directo
        if (owner == null || owner.IsInactive || owner == PhotonNetwork.LocalPlayer)
        {
            PhotonNetwork.Destroy(pv);
            return;
        }

        // Pedir al dueño que lo destruya
        photonView.RPC(nameof(RPC_OwnerDestroy), owner, viewID);
    }

    [PunRPC]
    void RPC_OwnerDestroy(int viewID)
    {
        var pv = PhotonView.Find(viewID);
        if (!pv) return;

        if (pv.AmOwner)
            PhotonNetwork.Destroy(pv);
    }
}
