using UnityEngine;
using Photon.Pun;

public class MeatCookingState : MonoBehaviourPunCallbacks
{
    public enum CookingState { Raw, Cooking, Cooked, Burned }
    public CookingState currentState = CookingState.Raw;
    public string meatName;

    [Header("Estados")]
    public bool isOnPan = false;
    public bool isHeldByPlayer = false;

    [Header("Lados (meshes)")]
    [SerializeField] private GameObject burger1; // lado A
    [SerializeField] private GameObject burger2; // lado B

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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        Transform parent = transform.parent;
        isHeldByPlayer = parent != null && parent.CompareTag("PlayerHand");

        SetKinematic(isHeldByPlayer || isOnPan);
        UpdateCookingState();
    }

    // ---------------------------------------------------------
    // FÍSICAS Y FLAGS
    // ---------------------------------------------------------

    public void SetOnPan(bool value)
    {
        isOnPan = value;
        SetKinematic(value || isHeldByPlayer);
    }

    public void SetKinematic(bool k)
    {
        if (lockedOnTable && !k) return;
        if (!rb) return;

        // 🔥 FIX IMPORTANTE:
        // Si alguien (por RPC) intenta poner la carne dinámica (k = false)
        // PERO actualmente está parentada a una mano (PlayerHand),
        // NO lo permitimos para evitar que se "caiga" en el cliente remoto.
        if (!k)
        {
            Transform parent = transform.parent;
            if (parent != null && parent.CompareTag("PlayerHand"))
            {
                // Aseguramos flags coherentes
                isHeldByPlayer = true;
                isOnPan = false;
                return; // ignorar este SetKinematic(false) mientras esté en la mano
            }
        }

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

    public void LockOnTable(bool locked)
    {
        lockedOnTable = locked;
        SetKinematic(locked);
        SetCollidersAsTrigger(false);
    }

    public void SetCollidersAsTrigger(bool asTrigger)
    {
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.isTrigger = asTrigger;
    }

    // ---------------------------------------------------------
    // RPC: Materiales
    // ---------------------------------------------------------

    [PunRPC]
    void RPC_SetMaterial(int side, string state)
    {
        Renderer r = null;

        if (side == 1 && burger2) r = burger2.GetComponent<Renderer>();
        if (side == 2 && burger1) r = burger1.GetComponent<Renderer>();

        if (!r) return;

        if (state == "Cooked")
        {
            r.material = (side == 1) ? cookedMaterialSide1 : cookedMaterialSide2;
        }
        else if (state == "Burned")
        {
            r.material = burnedMaterial;
        }
    }

    // ---------------------------------------------------------
    // RPC: Flip real (rotación física)
    // ---------------------------------------------------------
    [PunRPC]
    void RPC_Flip()
    {
        transform.Rotate(180f, 0f, 0f, Space.Self);
    }

    public void FlipMeat()
    {
        photonView.RPC(nameof(RPC_Flip), RpcTarget.All);
    }

    // ---------------------------------------------------------
    // RPC: Reparent (cuando agarras o pones en la mano/mesa)
    // ---------------------------------------------------------

    [PunRPC]
    void RPC_SetParent(int viewID)
    {
        if (viewID == -1)
        {
            transform.SetParent(null);
            return;
        }

        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView)
            transform.SetParent(targetView.transform);
    }

    // ---------------------------------------------------------
    // Lógica de cocción: estados y materiales
    // ---------------------------------------------------------

    public void CookSide1()
    {
        if (!isSide1Cooked && !isSide1Burned)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 1, "Cooked");
            isSide1Cooked = true;
        }
    }

    public void CookSide2()
    {
        if (!isSide2Cooked && !isSide2Burned)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 2, "Cooked");
            isSide2Cooked = true;
        }
    }

    public void BurnSide1()
    {
        if (!isSide1Burned)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 1, "Burned");
            isSide1Burned = true;
        }
    }

    public void BurnSide2()
    {
        if (!isSide2Burned)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 2, "Burned");
            isSide2Burned = true;
        }
    }

    // Side state → global cooking state
    void UpdateCookingState()
    {
        bool s1 = isSide1Cooked || isSide1Burned;
        bool s2 = isSide2Cooked || isSide2Burned;

        if (s1 && s2)
            currentState = (isSide1Burned || isSide2Burned) ? CookingState.Burned : CookingState.Cooked;
        else if (s1 || s2)
            currentState = CookingState.Cooking;
        else
            currentState = CookingState.Raw;
    }

    // ---------------------------------------------------------
    // APLICAR VISUAL (SOLO MATERIALES — SIN ROTAR)
    // ---------------------------------------------------------

    public void ApplyVisualDirectly(bool flipped)
    {
        // ⛔ YA NO TOCAMOS LA ROTACIÓN AQUÍ
        // (la rotación se maneja SOLO en CookMeatInPan)

        // Aplica materiales según los flags locales (no RPC)
        if (isSide1Burned)
        {
            if (burger2) burger2.GetComponent<Renderer>().material = burnedMaterial;
        }
        else if (isSide1Cooked)
        {
            if (burger2 && cookedMaterialSide1) burger2.GetComponent<Renderer>().material = cookedMaterialSide1;
        }

        if (isSide2Burned)
        {
            if (burger1) burger1.GetComponent<Renderer>().material = burnedMaterial;
        }
        else if (isSide2Cooked)
        {
            if (burger1 && cookedMaterialSide2) burger1.GetComponent<Renderer>().material = cookedMaterialSide2;
        }
    }
}
