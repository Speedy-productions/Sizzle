using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MesaArmado : MonoBehaviourPun
{
    [Header("Configuración")]
    public Transform assemblePoint;
    public Transform stackRoot;
    public float separationY = 0.01f;

    [Header("Referencias")]
    public ArmarPedido armarPedido;
    public RecipeSO currentRecipe;

    readonly List<GameObject> placedIngredients = new();
    Vector3 baseAssembleLocalPos;
    float currentTopY = 0f;

    void Awake()
    {
        if (assemblePoint == null)
        {
            Debug.LogError($"[MesaArmado] Falta assignar assemblePoint en {name}");
            enabled = false;
            return;
        }

        if (stackRoot == null) stackRoot = transform;
        baseAssembleLocalPos = assemblePoint.localPosition;
        ResetStackHeight();
    }

    // --------------------- SINCRONIZACIÓN DE INGREDIENTE ------------------------
    public void TryPlaceIngredientFromHand(Ingredient ing)
    {
        if (!ing) return;
        if (!ing.IsReady()) return;

        GameObject obj = ing.gameObject;

        if (placedIngredients.Contains(obj)) return;
        if (!IsNextInOrder(ResolveIngredientName(obj), placedIngredients.Count)) return;

        if (ing.TryGetComponent(out PhotonView pv))
        {
            // 🔸 Todos pueden enviar el RPC (ya no depende solo de pv.IsMine)
            float nextTopY = currentTopY;
            photonView.RPC(nameof(RPC_PlaceIngredient), RpcTarget.AllBuffered, pv.ViewID, nextTopY);
        }
        else
        {
            Debug.LogWarning($"[MesaArmado] {obj.name} no tiene PhotonView asignado.");
        }
    }

    // RPC remoto: sincroniza la colocación con posición exacta
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
                ResolveIngredientName(obj),
                placedIngredients,
                currentRecipe,
                this
            );
        }
    }

    /// <summary>Quita un ingrediente de la mesa (se usa al agarrarlo con la mano).</summary>
    public void RemoveIngredient(GameObject obj)
    {
        if (obj == null) return;
        if (!placedIngredients.Remove(obj)) return;

        // Desbloquear si era carne
        if (obj.TryGetComponent(out MeatCookingState meat))
            meat.LockOnTable(false);

        // Recalcular toda la pila (alturas y posiciones)
        RebuildStack();
    }

    /// <summary>Borra todos los ingredientes de la mesa.</summary>
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

    /// <summary>Ingredientes actualmente en la mesa (en orden de apilado).</summary>
    public List<GameObject> GetPlacedIngredients() => placedIngredients;

    // =============== Utilidades internas ===============

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
                // si actualmente es dinámico, zeroeamos antes de pasarlo a kinematic
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


    string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace("Sliced", "").Replace("Slice", "").Replace("Cortado", "");
        return name.Trim();
    }

    float GetWorldHeight(GameObject obj)
    {
        // 1) Renderers
        var rends = obj.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return Mathf.Max(0.001f, b.size.y);
        }

        // 2) Colliders
        var cols = obj.GetComponentsInChildren<Collider>(true);
        if (cols.Length > 0)
        {
            var b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return Mathf.Max(0.001f, b.size.y);
        }

        // 3) Fallback: escala local Y
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

    void RebuildStack()
    {
        // Recalcular desde cero SIN usar assemblePoint como padre
        currentTopY = 0f;

        // Base del tope
        assemblePoint.localPosition = baseAssembleLocalPos;

        for (int i = 0; i < placedIngredients.Count; i++)
        {
            var obj = placedIngredients[i];
            if (!obj) continue;

            float h = GetWorldHeight(obj);

            // Top actual en mundo
            Vector3 topWorld = assemblePoint.position;

            // === Mantener escala mundial y parentear al contenedor fijo
            Vector3 Sw = WorldScaleUtils.GetOrInitWorldScaleMemory(obj.transform);
            WorldScaleUtils.ReparentKeepWorldScale(obj.transform, stackRoot, Sw);

            obj.transform.rotation = Quaternion.identity;
            obj.transform.position = topWorld;

            // Subir tope
            currentTopY += h + separationY;
            UpdateAssemblePointY();
        }
    }


    string ResolveIngredientName(GameObject obj)
    {
        string n = null;
        if (obj.TryGetComponent(out Ingredient ing)) n = ing.ingredientName;
        else if (obj.TryGetComponent(out SliceIngredient slice)) n = slice.ingredientName;
        else if (obj.TryGetComponent(out MeatCookingState meat)) n = meat.meatName;
        else n = obj.name;

        return NormalizeName(n);
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

    bool IsNextInOrder(string candidate, int placedCountSoFar)
    {
        string cand = NormalizeName(candidate);

        // Usar pedido activo si existe
        if (OrderManager.Instance != null && OrderManager.Instance.CurrentOrder != null)
        {
            var order = OrderManager.Instance.CurrentOrder.ingredients;
            if (order == null || order.Length == 0) return true;
            if (placedCountSoFar >= order.Length) return false;
            return string.Equals(cand, NormalizeName(order[placedCountSoFar]));
        }

        // Fallback a RecipeSO
        if (currentRecipe == null || currentRecipe.requiredIngredients == null) return true;
        if (placedCountSoFar >= currentRecipe.requiredIngredients.Count) return false;
        return string.Equals(cand, NormalizeName(currentRecipe.requiredIngredients[placedCountSoFar]));
    }

    // ¿Este objeto está en la pila de esta mesa?
public bool Contains(GameObject obj) => obj && placedIngredients.Contains(obj);

// ¿Es el de hasta arriba?
public bool IsTopIngredient(GameObject obj)
{
    if (!obj || placedIngredients.Count == 0) return false;
    return placedIngredients[placedIngredients.Count - 1] == obj;
}

// Obtener el top (o null si no hay)
public GameObject GetTopIngredient()
{
    if (placedIngredients.Count == 0) return null;
    return placedIngredients[placedIngredients.Count - 1];
}

}
