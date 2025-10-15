using UnityEngine;

public class FriesCookingState : MonoBehaviour
{
    public enum CookingState { Raw, Cooking, Cooked, Burned }
    public CookingState currentState = CookingState.Raw;
    public Quaternion initialWorldRot { get; private set; }
    public Vector3 initialWorldScale { get; private set; }

    [Header("Materiales/Render")]
    [SerializeField] Material rawMaterial;         // Opcional (si quieres)
    [SerializeField] Material cookedMaterial;      // Dorado
    [SerializeField] Material burnedMaterial;      // Quemado

    [Header("Prefabs y Padres")]
    [SerializeField] private GameObject friesParentPrefab;  // Prefab del padre que contiene todas las papas

    private Renderer[] friesRenderers;  // Array de renderers de todas las papas (hijos)

    [Header("Flags")]
    public bool isInFryer = false;
    public bool isHeldByPlayer = false;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Siempre tomar los renderers de ESTA instancia (hijos)
        friesRenderers = GetComponentsInChildren<Renderer>(true);
        if (friesRenderers == null || friesRenderers.Length == 0)
        {
            Debug.Log("[FriesCookingState] No se encontraron renderers en hijos de la instancia.");
        }

        initialWorldRot   = transform.rotation;   // rotación “correcta” del prefab en mundo
    initialWorldScale = transform.lossyScale;
    }

    void Update()
    {
        // En mano = padre con tag PlayerHand
        Transform p = transform.parent;
        isHeldByPlayer = p != null && p.CompareTag("PlayerHand");

        // Si está en la freidora o en la mano, se ajustan las físicas del objeto
        SetKinematic(isInFryer || isHeldByPlayer);
    }

    public void SetInFryer(bool v)
    {
        isInFryer = v;
        SetKinematic(v || isHeldByPlayer);
        SetCollidersAsTrigger(v); // En la freidora, los colliders deben ser trigger
    }

    void SetKinematic(bool k)
    {
        if (!rb) return;
        bool wasKinematic = rb.isKinematic;

        if (k)
        {
            if (!wasKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public void SetCollidersAsTrigger(bool t)
    {
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.isTrigger = t;
    }

    // Método para marcar todas las papas como cocidas
    public void MarkCooked()
    {
        if (currentState == CookingState.Raw || currentState == CookingState.Cooking)
        {
            foreach (var renderer in friesRenderers)
            {
                if (renderer && cookedMaterial) renderer.material = cookedMaterial; // Cambiar a material cocido
            }
            currentState = CookingState.Cooked;
        }
    }

    // Método para marcar todas las papas como quemadas
    public void MarkBurned()
    {
        if (currentState != CookingState.Burned)
        {
            foreach (var renderer in friesRenderers)
            {
                if (renderer && burnedMaterial) renderer.material = burnedMaterial; // Cambiar a material quemado
            }
            currentState = CookingState.Burned;
        }
    }

    // Método para marcar todas las papas como crudas (restablecer estado)
    public void ResetFries()
    {
        if (currentState != CookingState.Raw)
        {
            foreach (var renderer in friesRenderers)
            {
                if (renderer && rawMaterial) renderer.material = rawMaterial; // Cambiar a material crudo
            }
            currentState = CookingState.Raw;
        }
    }
}
