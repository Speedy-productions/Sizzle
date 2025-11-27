using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class MesaArmado : MonoBehaviourPun
{
    [Header("Configuración")]
    public Transform assemblePoint;
    public Transform stackRoot;
    public float separationY = 0.01f;

    [Header("Referencias")]
    public RecipeSO currentRecipe;

    [Header("Prefabs hamburguesa")]
    public GameObject finalBurgerPrefabOffline;

    private List<GameObject> placedIngredients = new List<GameObject>();
    private Vector3 baseAssembleLocalPos;
    private float currentTopY = 0f;
    private bool burgerAlreadyCreated = false;

    void Awake()
    {
        if (assemblePoint == null)
        {
            Debug.LogError("[MesaArmado] Falta asignar assemblePoint.");
            enabled = false;
            return;
        }

        if (stackRoot == null) stackRoot = transform;
        baseAssembleLocalPos = assemblePoint.localPosition;
        ResetStackHeight();
    }

    public void TryPlaceIngredientFromHand(Ingredient ing)
{
    if (ing == null) return;

    GameObject obj = ing.gameObject;

    // ❌ No permitir hamburguesas dentro de hamburguesas
    if (obj.CompareTag("Burger") || obj.GetComponent<Hamburguesa>())
        return;

    string ingName = ing.ingredientName;

    // ✅ DETECCIÓN DE CARNE
    bool isMeat = ing.TryGetComponent(out MeatCookingState meat);

    // ✅ VALIDACIÓN COMPLETA DE INGREDIENTES PERMITIDOS
    bool isValid =
        ingName == "Tapa" ||                // ✅ Pan superior
        ingName == "Base" ||                // ✅ Pan inferior
        ingName.Contains("Sliced") ||       // ✅ Tomate / Lechuga cortados
        isMeat;                             // ✅ Carne

    if (!isValid)
        return;

    // ✅ BLOQUEAR CARNE CRUDA
    if (isMeat && !ing.IsReady())
        return;

    // ✅ EVITAR DUPLICADOS
    if (placedIngredients.Contains(obj))
        placedIngredients.Remove(obj);

    // ✅ SINCRONIZACIÓN NORMAL
    if (ing.TryGetComponent(out PhotonView pv))
    {
        float syncTopY = currentTopY;
        photonView.RPC(nameof(RPC_PlaceIngredient), RpcTarget.AllBuffered, pv.ViewID, syncTopY);
    }
    else
    {
        Debug.LogWarning($"[MesaArmado] {obj.name} no tiene PhotonView asignado.");
    }
}



    [PunRPC]
    void RPC_PlaceIngredient(int viewID, float syncTopY)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv == null) return;

        GameObject obj = pv.gameObject;
        PrepareForTable(obj);

        assemblePoint.localPosition = baseAssembleLocalPos + Vector3.up * syncTopY;
        Vector3 topWorld = assemblePoint.position;

        Vector3 worldScale = WorldScaleUtils.GetOrInitWorldScaleMemory(obj.transform);
        WorldScaleUtils.ReparentKeepWorldScale(obj.transform, stackRoot, worldScale);

        obj.transform.rotation = Quaternion.identity;
        obj.transform.position = topWorld;

        foreach (var c in obj.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = true;
            c.isTrigger = false;
        }

        if (!placedIngredients.Contains(obj))
            placedIngredients.Add(obj);

        currentTopY = syncTopY + GetWorldHeight(obj) + separationY;
        UpdateAssemblePointY();

        obj.transform.position = new Vector3(
            obj.transform.position.x,
            obj.transform.position.y - (GetWorldHeight(obj) * 0.5f),
            obj.transform.position.z
        );
    }

    public void FinalizeBurger()
{
    if (placedIngredients.Count == 0)
    {
        Debug.LogWarning("[MesaArmado] No hay ingredientes para crear una hamburguesa.");
        return;
    }

    List<string> ingredientNames = new List<string>();
    foreach (var ingredient in placedIngredients)
    {
        if (ingredient.TryGetComponent(out Ingredient ing))
            ingredientNames.Add(ing.ingredientName);
    }

    // Solo este jugador crea la hamburguesa y limpia su mesa
    CreateAndClearBurgerLocal(currentRecipe.finalProductPrefab.name, ingredientNames.ToArray());

    // Luego avisa a los demás para que limpien sus mesas
    photonView.RPC(nameof(RPC_ClearMesa), RpcTarget.OthersBuffered);
}


    [PunRPC]
void RPC_CreateAndClearBurger(string burgerPrefabName, string[] ingredientNames)
{
    if (burgerAlreadyCreated) return;
    burgerAlreadyCreated = true;

    // Solo este cliente crea la hamburguesa, luego PUN sincroniza el objeto a todos
    if (photonView.IsMine)
    {
        Vector3 spawnPos = assemblePoint.position;
        GameObject burger = PhotonNetwork.Instantiate(
            burgerPrefabName,
            spawnPos,
            Quaternion.identity
        );

        if (burger.TryGetComponent(out Hamburguesa hamb))
            hamb.SetIngredientes(ingredientNames.ToList());

        Debug.Log("[MesaArmado] Hamburguesa creada con ingredientes: " + string.Join(",", ingredientNames));
    }

    ClearMesaLocal();
}

void CreateAndClearBurgerLocal(string burgerPrefabName, string[] ingredientNames)
{
    if (burgerAlreadyCreated) return;
    burgerAlreadyCreated = true;

    Vector3 spawnPos = assemblePoint.position;
    GameObject burger = PhotonNetwork.Instantiate(
        burgerPrefabName,
        spawnPos,
        Quaternion.identity
    );

    if (burger.TryGetComponent(out Hamburguesa hamb))
        hamb.SetIngredientes(ingredientNames.ToList());

    Debug.Log("[MesaArmado] Hamburguesa creada con ingredientes: " + string.Join(",", ingredientNames));

    ClearMesaLocal();
}


    public void ClearMesa()
    {
        photonView.RPC(nameof(RPC_ClearMesa), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_ClearMesa()
    {
        ClearMesaLocal();
    }

    void ClearMesaLocal()
{
    foreach (var obj in placedIngredients)
    {
        if (obj == null) continue;

        if (PhotonNetwork.IsConnected && obj.TryGetComponent(out PhotonView pv))
        {
            // ✅ Destruye solo si el cliente actual es el dueño (pv.IsMine)
            if (pv.IsMine)
                PhotonNetwork.Destroy(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    placedIngredients.Clear();
    burgerAlreadyCreated = false;
    ResetStackHeight();
}


    float GetWorldHeight(GameObject obj)
    {
        var rends = obj.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return Mathf.Max(0.001f, b.size.y);
        }
        return Mathf.Max(0.001f, obj.transform.lossyScale.y);
    }

    void ResetStackHeight()
    {
        currentTopY = 0f;
        UpdateAssemblePointY();
    }

    void UpdateAssemblePointY()
    {
        assemblePoint.localPosition = baseAssembleLocalPos + Vector3.up * currentTopY;
    }

    void PrepareForTable(GameObject obj)
    {
        if (obj.TryGetComponent(out MeatCookingState meat))
        {
            meat.SetOnPan(false);
            meat.LockOnTable(true);
        }
        else
        {
            if (obj.TryGetComponent(out Rigidbody rb))
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
                c.isTrigger = false;
        }
    }

    public void ResetAfterComplete(bool destroyChildrenUnderAssemblePoint = false)
    {
        if (destroyChildrenUnderAssemblePoint && assemblePoint)
        {
            for (int i = assemblePoint.childCount - 1; i >= 0; i--)
            {
                var ch = assemblePoint.GetChild(i);
                if (ch) Destroy(ch.gameObject);
            }
        }

        placedIngredients.Clear();
        currentTopY = 0f;
        assemblePoint.localPosition = baseAssembleLocalPos;
        burgerAlreadyCreated = false;
    }

    public bool Contains(GameObject obj)
    {
        return placedIngredients.Contains(obj);
    }

    public bool IsTopIngredient(GameObject obj)
    {
        if (placedIngredients.Count == 0) return false;
        return placedIngredients[placedIngredients.Count - 1] == obj;
    }

    public void RemoveIngredient(GameObject obj)
    {
        if (obj == null) return;

        if (placedIngredients.Contains(obj))
        {
            placedIngredients.Remove(obj);

            if (obj.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            obj.transform.SetParent(null);

            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
                c.enabled = true;

            obj.transform.position += Vector3.up * 0.02f;

            Debug.Log($"[MesaArmado] Ingrediente '{obj.name}' retirado de la mesa (no destruido).");

            RecalculateStackHeight();
        }
    }

    private void RecalculateStackHeight()
    {
        currentTopY = 0f;

        foreach (var ing in placedIngredients)
        {
            if (!ing) continue;
            float h = GetWorldHeight(ing);
            currentTopY += h + separationY;
        }

        UpdateAssemblePointY();
    }

    public List<GameObject> GetPlacedIngredients()
    {
        return placedIngredients;
    }
}