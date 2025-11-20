using UnityEngine;
using Photon.Pun;

public class CookMeatInPan : MonoBehaviourPun
{
    [Header("Configuración")]
    [SerializeField] Transform panCenter;
    [SerializeField] public float cookingTime = 10f;
    [SerializeField] public float burningTime = 5f;

    [Header("UI")]
    [SerializeField] GameObject cookingCanvas;
    [SerializeField] CookingProgressUI cookingUI;

    MeatCookingState currentMeat;
    bool isCooking = false;
    float currentCookingTime = 0f;
    bool isFlipped = false;

    void Start() => ResetCookingSystem();

    void Update()
    {
        if (!currentMeat) return;

        PhotonView meatPV = currentMeat.GetComponent<PhotonView>();
        if (!meatPV) return;

        bool iAmOwner = meatPV.IsMine;

        // --- EL OWNER cocina ---
        if (iAmOwner)
        {
            ProcessCookingTick();
        }

        // --- TODOS deben ver la UI si hay carne cocinando ---
        if (isCooking && cookingUI)
        {
            cookingUI.SetVisible(true);

            float total = cookingTime + burningTime;
            float t = Mathf.Clamp01(currentCookingTime / total);
            float frac = cookingTime / total;

            cookingUI.SetProgress(t, frac);
        }

        // Si la carne se quita del sartén
        if (currentMeat && currentMeat.transform.parent != panCenter)
        {
            StopCookingAndHideUI();
        }
    }

    // ----------------------------------------------------------
    // PROCESO DE COCCIÓN SOLO PARA EL OWNER DE LA CARNE
    // ----------------------------------------------------------

    void ProcessCookingTick()
    {
        if (!isCooking) return;

        currentCookingTime += Time.deltaTime;

        float total = cookingTime + burningTime;

        // --- Estado final (quemado) ---
        if (currentCookingTime >= total)
        {
            if (!isFlipped && !currentMeat.isSide1Burned)
                currentMeat.BurnSide1();
            else if (isFlipped && !currentMeat.isSide2Burned)
                currentMeat.BurnSide2();

            isCooking = false;
            SetMeatToBurnedOrCookedState();

            photonView.RPC(nameof(RPC_UpdateVisualState), RpcTarget.OthersBuffered,
                currentMeat.GetComponent<PhotonView>().ViewID,
                (int)currentMeat.currentState,
                isFlipped);

            return;
        }

        // --- Estado de cocción (listo sin quemar) ---
        if (currentCookingTime >= cookingTime)
        {
            if (!isFlipped && !currentMeat.isSide1Cooked && !currentMeat.isSide1Burned)
                currentMeat.CookSide1();
            else if (isFlipped && !currentMeat.isSide2Cooked && !currentMeat.isSide2Burned)
                currentMeat.CookSide2();

            photonView.RPC(nameof(RPC_UpdateVisualState), RpcTarget.OthersBuffered,
                currentMeat.GetComponent<PhotonView>().ViewID,
                (int)currentMeat.currentState,
                isFlipped);
        }
    }

    // ----------------------------------------------------------
    // INICIAR COCCIÓN
    // ----------------------------------------------------------

    public bool TryStartCooking(MeatCookingState meat)
    {
        if (!meat || isCooking) return false;

        bool side1Finished = meat.isSide1Cooked || meat.isSide1Burned;
        bool side2Finished = meat.isSide2Cooked || meat.isSide2Burned;

        if (side1Finished && side2Finished)
        {
            ShowAimHint(false, false);
            return false;
        }

        currentMeat = meat;

        PhotonView meatPV = meat.GetComponent<PhotonView>();
        PhotonView panPV = GetComponent<PhotonView>();

        // --- LO MÁS IMPORTANTE ---
        // Asegurar que quien cocina es el OWNER de la carne
        if (meatPV != null && !meatPV.IsMine)
            meatPV.RequestOwnership();

        // Reposicionar carne en TODOS
        panPV.RPC(nameof(RPC_SetMeatOnPan), RpcTarget.AllBuffered, meatPV.ViewID);

        // Volteo automático si ya cocinó el primer lado
        if (side1Finished && !side2Finished)
        {
            meat.transform.rotation = panCenter.rotation;
            meat.transform.Rotate(180f, 0f, 0f, Space.Self);
            isFlipped = true;
        }
        else
        {
            isFlipped = false;
        }

        meat.SetOnPan(true);
        meat.SetCollidersAsTrigger(true);
        meat.SetKinematic(true);

        // Empezar cocción
        isCooking = true;
        currentCookingTime = 0f;

        // UI solo local (no por RPC)
        if (cookingUI)
        {
            cookingUI.SetVisible(true);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }

        ShowAimHint(false, false);
        meat.currentState = MeatCookingState.CookingState.Cooking;

        return true;
    }

    // ----------------------------------------------------------
    // VOLTEAR
    // ----------------------------------------------------------

    public bool TryFlipFromInteraccion()
    {
        if (!currentMeat) return false;

        PhotonView meatPV = currentMeat.GetComponent<PhotonView>();
        if (meatPV != null && !meatPV.IsMine)
            meatPV.RequestOwnership();

        if (!isFlipped && (currentMeat.isSide1Cooked || currentMeat.isSide1Burned))
        {
            currentMeat.FlipMeat();
            isFlipped = true;

            currentCookingTime = 0f;
            isCooking = true;

            if (cookingUI)
                cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));

            // Sync visual
            photonView.RPC(nameof(RPC_SyncMeatOnPan), RpcTarget.OthersBuffered, meatPV.ViewID, isFlipped);

            return true;
        }

        return false;
    }

    // ----------------------------------------------------------
    // RPCs
    // ----------------------------------------------------------

    [PunRPC]
    void RPC_SyncMeatOnPan(int meatViewID, bool flipped)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        var meat = meatPV.GetComponent<MeatCookingState>();
        if (!meat) return;

        meat.transform.SetParent(panCenter, false);
        meat.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);

        if (flipped)
            meat.transform.Rotate(180f, 0f, 0f, Space.Self);

        meat.SetOnPan(true);
        meat.SetCollidersAsTrigger(true);
        meat.SetKinematic(true);
    }

    [PunRPC]
    void RPC_SetMeatOnPan(int meatViewID)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        meatPV.transform.SetParent(panCenter);
        meatPV.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);

        var rb = meatPV.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    [PunRPC]
    void RPC_UpdateVisualState(int meatViewID, int newState, bool flipped)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        var meat = meatPV.GetComponent<MeatCookingState>();
        if (!meat) return;

        meat.currentState = (MeatCookingState.CookingState)newState;
        meat.ApplyVisualDirectly(flipped);
    }

    // ----------------------------------------------------------
    // PARAR COCCIÓN
    // ----------------------------------------------------------

    void StopCookingAndHideUI()
    {
        isCooking = false;
        currentCookingTime = 0f;

        if (cookingCanvas) cookingCanvas.SetActive(false);
        if (cookingUI) cookingUI.SetVisible(false);

        if (currentMeat)
        {
            PhotonView meatPV = currentMeat.GetComponent<PhotonView>();
            if (meatPV)
                photonView.RPC(nameof(RPC_ClearMeatFromPan), RpcTarget.OthersBuffered, meatPV.ViewID);

            currentMeat.SetOnPan(false);
        }

        currentMeat = null;
        isFlipped = false;
    }

    [PunRPC]
    void RPC_ClearMeatFromPan(int meatViewID)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        var meat = meatPV.GetComponent<MeatCookingState>();
        if (!meat) return;

        meat.SetOnPan(false);
        meat.SetKinematic(false);
        meat.transform.SetParent(null);
    }

    // ----------------------------------------------------------
    // HINT E ICONOS
    // ----------------------------------------------------------

    public void ShowAimHint(bool aimingPan, bool hasMeatInHand)
    {
        bool show = false;

        if (aimingPan && hasMeatInHand && !isCooking && !currentMeat)
        {
            show = true;
        }

        if (cookingCanvas)
            cookingCanvas.SetActive(show);
    }

    void SetMeatToBurnedOrCookedState()
    {
        bool s1 = currentMeat.isSide1Cooked || currentMeat.isSide1Burned;
        bool s2 = currentMeat.isSide2Cooked || currentMeat.isSide2Burned;

        if (s1 && s2)
            currentMeat.currentState =
                (currentMeat.isSide1Burned || currentMeat.isSide2Burned)
                ? MeatCookingState.CookingState.Burned
                : MeatCookingState.CookingState.Cooked;
        else if (s1 || s2)
            currentMeat.currentState = MeatCookingState.CookingState.Cooking;
        else
            currentMeat.currentState = MeatCookingState.CookingState.Raw;
    }

    void ResetCookingSystem()
    {
        isCooking = false;
        isFlipped = false;
        currentCookingTime = 0f;
        currentMeat = null;

        if (cookingCanvas) cookingCanvas.SetActive(false);
        if (cookingUI)
        {
            cookingUI.SetVisible(false);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }
    }
}
