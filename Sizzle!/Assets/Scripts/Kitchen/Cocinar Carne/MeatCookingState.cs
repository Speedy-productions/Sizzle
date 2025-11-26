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
        // Solo el dueño actual de la carne mantiene coherencia de flags → estado
        if (!photonView.IsMine) return;

        Transform parent = transform.parent;
        isHeldByPlayer = parent != null && parent.CompareTag("PlayerHand");

        SetKinematic(isHeldByPlayer || isOnPan);

        // Estado global derivado de los flags
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

        // FIX: si está parentado a una mano (PlayerHand) no dejamos que
        // un RPC remoto lo haga dinámico (para que no se caiga en ese cliente)
        if (!k)
        {
            Transform parent = transform.parent;
            if (parent != null && parent.CompareTag("PlayerHand"))
            {
                isHeldByPlayer = true;
                isOnPan = false;
                return;
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
    // RPC: Materiales (compatibilidad)
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
    // RPC: Flip real (ya casi no lo usamos, pero lo dejamos)
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
    // LÓGICA DE COCCIÓN (solo dueño modifica flags y estado)
    // ---------------------------------------------------------

    public void CookSide1()
    {
        if (!photonView.IsMine) return;

        if (!isSide1Cooked && !isSide1Burned)
        {
            isSide1Cooked = true;

            UpdateCookingState();
            ApplyVisualDirectly(false);
            SyncCookingStateToOthersBuffered();
        }
    }

    public void CookSide2()
    {
        if (!photonView.IsMine) return;

        if (!isSide2Cooked && !isSide2Burned)
        {
            isSide2Cooked = true;

            UpdateCookingState();
            ApplyVisualDirectly(false);
            SyncCookingStateToOthersBuffered();
        }
    }

    public void BurnSide1()
    {
        if (!photonView.IsMine) return;

        if (!isSide1Burned)
        {
            isSide1Burned = true;

            UpdateCookingState();
            ApplyVisualDirectly(false);
            SyncCookingStateToOthersBuffered();
        }
    }

    public void BurnSide2()
    {
        if (!photonView.IsMine) return;

        if (!isSide2Burned)
        {
            isSide2Burned = true;

            UpdateCookingState();
            ApplyVisualDirectly(false);
            SyncCookingStateToOthersBuffered();
        }
    }

    // Estado global derivado de los flags
    void UpdateCookingState()
    {
        bool s1 = isSide1Cooked || isSide1Burned;
        bool s2 = isSide2Cooked || isSide2Burned;

        if (s1 && s2)
            currentState = (isSide1Burned || isSide2Burned)
                ? CookingState.Burned
                : CookingState.Cooked;
        else if (s1 || s2)
            currentState = CookingState.Cooking;
        else
            currentState = CookingState.Raw;
    }

    // ---------------------------------------------------------
    // SINCRONIZAR ESTADO COMPLETO A LOS DEMÁS
    // ---------------------------------------------------------

    public void SyncCookingStateToOthersBuffered()
    {
        if (!PhotonNetwork.IsConnected || !photonView) return;

        photonView.RPC(nameof(RPC_SyncCookingState), RpcTarget.OthersBuffered,
            isSide1Cooked, isSide2Cooked, isSide1Burned, isSide2Burned, (int)currentState);
    }

    [PunRPC]
    void RPC_SyncCookingState(
        bool s1Cooked,
        bool s2Cooked,
        bool s1Burned,
        bool s2Burned,
        int stateInt)
    {
        isSide1Cooked = s1Cooked;
        isSide2Cooked = s2Cooked;
        isSide1Burned = s1Burned;
        isSide2Burned = s2Burned;
        currentState = (CookingState)stateInt;

        ApplyVisualDirectly(false);
    }

    // ---------------------------------------------------------
    // APLICAR VISUAL (SOLO MATERIALES — SIN ROTAR)
    // ---------------------------------------------------------
    public void ApplyVisualDirectly(bool flipped)
    {
        // Lado 1 → burger2
        if (burger2)
        {
            var r2 = burger2.GetComponent<Renderer>();
            if (r2)
            {
                if (isSide1Burned && burnedMaterial)
                {
                    r2.material = burnedMaterial;
                }
                else if (isSide1Cooked && cookedMaterialSide1)
                {
                    r2.material = cookedMaterialSide1;
                }
            }
        }

        // Lado 2 → burger1
        if (burger1)
        {
            var r1 = burger1.GetComponent<Renderer>();
            if (r1)
            {
                if (isSide2Burned && burnedMaterial)
                {
                    r1.material = burnedMaterial;
                }
                else if (isSide2Cooked && cookedMaterialSide2)
                {
                    r1.material = cookedMaterialSide2;
                }
            }
        }
    }
}
