using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class MesaArmado : MonoBehaviourPun
{
    [Header("Configuración")]
    public Transform assemblePoint;  // Punto donde se coloca cada ingrediente
    public Transform stackRoot;  // Contenedor fijo para los ingredientes apilados
    public float separationY = 0.01f;  // Separación vertical entre ingredientes

    [Header("Referencias")]
    public ArmarPedido armarPedido;  // Sistema que valida y combina los ingredientes
    public RecipeSO currentRecipe;  // Prefab final de la hamburguesa (si aplica)

    // Estado interno
    private List<GameObject> placedIngredients = new List<GameObject>();  // Ingredientes colocados en la mesa
    private Vector3 baseAssembleLocalPos;  // Posición base de ensamblaje
    private float currentTopY = 0f;  // Altura acumulada de los ingredientes

    void Awake()
    {
        if (armarPedido == null)
        {
            armarPedido = FindObjectOfType<ArmarPedido>();
            if (armarPedido == null)
                Debug.LogWarning("[MesaArmado] No se encontró referencia a ArmarPedido en la escena.");
        }

        if (assemblePoint == null)
        {
            Debug.LogError("[MesaArmado] Falta asignar assemblePoint.");
            enabled = false;
            return;
        }

        if (stackRoot == null) stackRoot = transform;  // Si no hay contenedor, usamos el propio objeto
        baseAssembleLocalPos = assemblePoint.localPosition;
        ResetStackHeight();
    }

    // Método para colocar un ingrediente en la mesa
    // ---------------------- MÉTODO PRINCIPAL ----------------------
    public void TryPlaceIngredientFromHand(Ingredient ing)
    {
        if (ing == null || !ing.IsReady()) return;
        GameObject obj = ing.gameObject;

        if (placedIngredients.Contains(obj)) return;

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

    // ---------------------- RPC DE SINCRONIZACIÓN ----------------------
    [PunRPC]
    void RPC_PlaceIngredient(int viewID, float syncTopY)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv == null)
        {
            Debug.LogWarning($"[MesaArmado] No se encontró objeto con ViewID {viewID}");
            return;
        }

        GameObject obj = pv.gameObject;
        PrepareForTable(obj);

        assemblePoint.localPosition = baseAssembleLocalPos + Vector3.up * syncTopY;
        Vector3 topWorld = assemblePoint.position;

        Vector3 Sw = WorldScaleUtils.GetOrInitWorldScaleMemory(obj.transform);
        WorldScaleUtils.ReparentKeepWorldScale(obj.transform, stackRoot, Sw);

        obj.transform.rotation = Quaternion.identity;
        obj.transform.position = topWorld;

        if (!placedIngredients.Contains(obj))
            placedIngredients.Add(obj);

        currentTopY = syncTopY + GetWorldHeight(obj) + separationY;
        UpdateAssemblePointY();

        if (armarPedido != null)
        {
            armarPedido.OnIngredientPlaced(
                obj.GetComponent<Ingredient>()?.ingredientName ?? obj.name,
                placedIngredients,
                currentRecipe,
                this
            );
        }
    }

    public void CreateCustomBurger()
    {
        if (placedIngredients.Count == 0)
        {
            Debug.LogError("[MesaArmado] No hay ingredientes en la mesa.");
            return;
        }

        // Recolectamos los nombres de ingredientes
        List<string> ingredientNames = new List<string>();
        foreach (var ingredient in placedIngredients)
        {
            if (ingredient.TryGetComponent(out Ingredient ing))
                ingredientNames.Add(ing.ingredientName);
        }

        // Generamos el pedido local y sincronizamos con todos
        Order customOrder = new Order(ingredientNames.ToArray());
        OrderManager.Instance.SetCurrentOrder(customOrder);

        // RPC global para crear el producto visual
        photonView.RPC(nameof(RPC_CreateBurger), RpcTarget.AllBuffered, ingredientNames.ToArray());

        // Limpieza local
        ClearMesa();
    }

    [PunRPC]
    void RPC_CreateBurger(string[] ingredientNames)
    {
        if (currentRecipe == null || currentRecipe.finalProductPrefab == null)
        {
            Debug.LogWarning("[MesaArmado] currentRecipe o su prefab final no están asignados.");
            return;
        }

        Vector3 spawnPos = assemblePoint.position;
        Quaternion spawnRot = Quaternion.identity;

        // Usa PhotonNetwork para crearla en red
        GameObject burger = PhotonNetwork.Instantiate(
            currentRecipe.finalProductPrefab.name,
            spawnPos,
            spawnRot
        );

        // Configura sus ingredientes
        if (burger.TryGetComponent(out Hamburguesa hamburguesaScript))
            hamburguesaScript.SetIngredientes(ingredientNames.ToList());

        Debug.Log("[MesaArmado] ¡Hamburguesa personalizada creada en red!");
    }


    // Método para limpiar la mesa
    public void ClearMesa()
    {
        foreach (var obj in placedIngredients)
        {
            if (obj)
                Destroy(obj);
        }
        placedIngredients.Clear();
        ResetStackHeight();
    }

    // Método para verificar la altura de un objeto (usado para calcular la posición)
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

    // Ajuste de la altura de la mesa
    void ResetStackHeight()
    {
        currentTopY = 0f;
        UpdateAssemblePointY();
    }

    void UpdateAssemblePointY()
    {
        assemblePoint.localPosition = baseAssembleLocalPos + Vector3.up * currentTopY;
    }

    // Preparar el objeto (ingrediente) para la mesa
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
        // (normalmente no habrá hijos bajo assemblePoint, pero dejo la opción)
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

        // ✅ Reactivar físicas
        if (obj.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ✅ Quitar parent (liberarlo de la mesa)
        obj.transform.SetParent(null);

        // ✅ Asegurar que sus colliders vuelvan a estar activos
        foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        // ✅ Levantarlo un poquito para evitar que se quede pegado
        obj.transform.position += Vector3.up * 0.02f;

        Debug.Log($"[MesaArmado] Ingrediente '{obj.name}' retirado de la mesa (no destruido).");

        // ✅ Recalcular la altura de la pila después de quitarlo
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



}
