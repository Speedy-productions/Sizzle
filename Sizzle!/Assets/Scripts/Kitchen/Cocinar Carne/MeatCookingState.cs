using UnityEngine;
using Photon.Pun;

public class MeatCookingState : MonoBehaviourPunCallbacks
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
        if (!photonView.IsMine) return;
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
        if (lockedOnTable && !k) return; // no permitir soltar si está bloqueado
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

    [PunRPC]
    void RPC_SetMaterial (int side, string state)
    {
        Renderer r = null;
        if (side == 1 && burger2) r = burger2.GetComponent<Renderer>();
        if (side == 2 && burger1) r = burger1.GetComponent<Renderer>();

        if (r == null) return;

        if (state == "Cooked")
        {
            r.material = side == 1 ? cookedMaterialSide1 : cookedMaterialSide2;
        } 
        else if (state == "Burned")
        {
            r.material = burnedMaterial;
        }
    }

    [PunRPC]
    void RPC_Flip ()
    {
        transform.Rotate(180f, 0f, 0f);
    }

    [PunRPC]
    void RPC_SetParent(int viewID)
    {
        if (viewID == -1)
        {
            transform.SetParent(null);
        }
        else
        {
            PhotonView targetView = PhotonView.Find(viewID);
            if (targetView != null)
            {
                transform.SetParent(targetView.transform);
            }
        }
    }

    // Marca lado 1 como cocido (pinta burger2)
    public void CookSide1()
    {
        if (!isSide1Cooked && !isSide1Burned && burger2 && cookedMaterialSide1)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 1, "Cooked");
            isSide1Cooked = true;
        }
    }

    // Marca lado 2 como cocido (pinta burger1)
    public void CookSide2()
    {
        if (!isSide2Cooked && !isSide2Burned && burger1 && cookedMaterialSide2)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 2, "Cooked");
            isSide2Cooked = true;
        }
    }

    // Quema lado 1 (pinta burger2)
    public void BurnSide1()
    {
        if (!isSide1Burned && burger2 && burnedMaterial)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 1, "Burned");
            isSide1Burned = true;
        }
    }

    // Quema lado 2 (pinta burger1)
    public void BurnSide2()
    {
        if (!isSide2Burned && burger1 && burnedMaterial)
        {
            photonView.RPC(nameof(RPC_SetMaterial), RpcTarget.All, 2, "Burned");
            isSide2Burned = true;
        }
    }

    public void FlipMeat()
    {
        photonView.RPC(nameof(RPC_Flip), RpcTarget.All);
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

    public void ApplyVisualDirectly(bool flipped)
    {
        // Ajusta la rotación física para mostrar el lado correcto
        Vector3 euler = transform.localEulerAngles;
        bool currentlyFlipped = Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > 90f;
        if (flipped && !currentlyFlipped)
            transform.Rotate(180f, 0f, 0f, Space.Self);
        else if (!flipped && currentlyFlipped)
            transform.Rotate(180f, 0f, 0f, Space.Self);

        // Aplica materiales según los flags locales (no envía RPC)
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
