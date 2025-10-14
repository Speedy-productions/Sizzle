using UnityEngine;

public class MeatCookingState : MonoBehaviour
{
    public enum CookingState { Raw, Cooking, Cooked, Burned }
    public CookingState currentState = CookingState.Raw;
    public string meatName;

    [Header("Estados")]
    public bool isOnPan = false;          // está en el sartén
    public bool isHeldByPlayer = false;   // está en la mano (padre con tag PlayerHand)

    [Header("Lados (meshes)")]
    [SerializeField] private GameObject burger1; // lado A visual
    [SerializeField] private GameObject burger2; // lado B visual

    [Header("Materiales")]
    [SerializeField] private Material cookedMaterialSide1;
    [SerializeField] private Material cookedMaterialSide2;
    [SerializeField] private Material burnedMaterial;

    [Header("Progreso")]
    public bool isSide1Cooked = false;
    public bool isSide2Cooked = false;
    public bool isSide1Burned = false;
    public bool isSide2Burned = false;


    [HideInInspector] public bool lockedOnTable = false;

    Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void Update()
    {
        // Detecta si está en la mano (el padre tiene el tag PlayerHand)
        Transform parent = transform.parent;
        isHeldByPlayer = parent != null && parent.CompareTag("PlayerHand");

        SetKinematic(isHeldByPlayer || isOnPan);

        // Estado global según lados
        UpdateCookingState();
    }

    // Marcar/Desmarcar como "en sartén"
    public void SetOnPan(bool value)
    {
        isOnPan = value;
        SetKinematic(value || isHeldByPlayer);
    }

    // Activa/desactiva físicas del rigidbody
    public void SetKinematic(bool k)
    {
        if (lockedOnTable && !k) return; // bloqueado en mesa, kinematic

        if (!rb) return;

        if (k)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = k;
        rb.useGravity = !k;


    }

    public void LockOnTable(bool locked)
    {
        lockedOnTable = locked;
        SetKinematic(locked);
        SetCollidersAsTrigger(false);
    }

    // Todos los colliders del objeto como trigger 
    public void SetCollidersAsTrigger(bool asTrigger)
    {
        var cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].isTrigger = asTrigger;
    }

    // Marca lado 1 como cocido (pinta burger2)
    public void CookSide1()
    {
        if (!isSide1Cooked && !isSide1Burned && burger2 && cookedMaterialSide1)
        {
            var r = burger2.GetComponent<Renderer>();
            if (r) r.material = cookedMaterialSide1;
            isSide1Cooked = true;
        }
    }

    // Marca lado 2 como cocido (pinta burger1)
    public void CookSide2()
    {
        if (!isSide2Cooked && !isSide2Burned && burger1 && cookedMaterialSide2)
        {
            var r = burger1.GetComponent<Renderer>();
            if (r) r.material = cookedMaterialSide2;
            isSide2Cooked = true;
        }
    }

    // Quema lado 1 (pinta burger2)
    public void BurnSide1()
    {
        if (!isSide1Burned && burger2 && burnedMaterial)
        {
            var r = burger2.GetComponent<Renderer>();
            if (r) r.material = burnedMaterial;
            isSide1Burned = true;
        }
    }

    // Quema lado 2 (pinta burger1)
    public void BurnSide2()
    {
        if (!isSide2Burned && burger1 && burnedMaterial)
        {
            var r = burger1.GetComponent<Renderer>();
            if (r) r.material = burnedMaterial;
            isSide2Burned = true;
        }
    }

    // No marca Burned hasta que ambos lados terminaron (cocidos o quemados)
    void UpdateCookingState()
    {
        bool side1Finished = isSide1Cooked || isSide1Burned;
        bool side2Finished = isSide2Cooked || isSide2Burned;

        if (side1Finished && side2Finished)
            currentState = (isSide1Burned || isSide2Burned) ? CookingState.Burned : CookingState.Cooked;
        else if (side1Finished || side2Finished)
            currentState = CookingState.Cooking;
        else
            currentState = CookingState.Raw;
    }
}
