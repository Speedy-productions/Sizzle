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

        // ---------------- SINGLE PLAYER (NO PHOTON) ----------------
        if (meatPV == null)
        {
            ProcessCookingTick();
            UpdateUIForEveryone();
            return;
        }

        // ---------------- MULTIJUGADOR ----------------
        bool iAmOwner = meatPV.IsMine;

        // Dueño cocina y envía progreso
        if (iAmOwner)
        {
            ProcessCookingTick();

            float total = cookingTime + burningTime;
            float t = Mathf.Clamp01(currentCookingTime / total);
            float frac = cookingTime / total;

            photonView.RPC(nameof(RPC_UpdateUIProgress), RpcTarget.Others, t, frac);
        }

        UpdateUIForEveryone();

        // Si la carne fue agarrada ? dejar de cocinar
        if (currentMeat && currentMeat.transform.parent != panCenter)
        {
            StopCookingAndHideUI();
        }
    }

    // ================================================================
    // PROCESO DE COCCIÓN
    // ================================================================
    void ProcessCookingTick()
    {
        if (!isCooking) return;

        currentCookingTime += Time.deltaTime;

        float total = cookingTime + burningTime;

        if (currentCookingTime >= total)
        {
            if (!isFlipped && !currentMeat.isSide1Burned)
                currentMeat.BurnSide1();
            else if (isFlipped && !currentMeat.isSide2Burned)
                currentMeat.BurnSide2();

            isCooking = false;
            SetMeatToBurnedOrCookedState();

            var meatPV = currentMeat.GetComponent<PhotonView>();
            if (photonView && meatPV)
            {
                photonView.RPC(nameof(RPC_UpdateVisualState), RpcTarget.OthersBuffered,
                    meatPV.ViewID, (int)currentMeat.currentState, isFlipped);
            }
            return;
        }

        if (currentCookingTime >= cookingTime)
        {
            if (!isFlipped && !currentMeat.isSide1Cooked)
                currentMeat.CookSide1();
            else if (isFlipped && !currentMeat.isSide2Cooked)
                currentMeat.CookSide2();

            var meatPV = currentMeat.GetComponent<PhotonView>();
            if (photonView && meatPV)
            {
                photonView.RPC(nameof(RPC_UpdateVisualState), RpcTarget.OthersBuffered,
                    meatPV.ViewID, (int)currentMeat.currentState, isFlipped);
            }
        }
    }

    // ================================================================
    // UI GLOBAL
    // ================================================================
    void UpdateUIForEveryone()
    {
        if (!isCooking || !cookingUI) return;

        cookingUI.SetVisible(true);

        float total = cookingTime + burningTime;
        float t = Mathf.Clamp01(currentCookingTime / total);
        float frac = cookingTime / total;

        cookingUI.SetProgress(t, frac);
    }

    // ================================================================
    // INICIAR COCCIÓN
    // ================================================================
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

        if (meatPV && !meatPV.IsMine)
            meatPV.RequestOwnership();

        if (panPV && meatPV)
            panPV.RPC(nameof(RPC_SetMeatOnPan), RpcTarget.AllBuffered, meatPV.ViewID);
        else
        {
            meat.transform.SetParent(panCenter);
            meat.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);
        }

        if (side1Finished && !side2Finished)
        {
            meat.transform.rotation = panCenter.rotation;
            meat.transform.Rotate(180f, 0f, 0f, Space.Self);
            isFlipped = true;
        }
        else isFlipped = false;

        meat.SetOnPan(true);
        meat.SetCollidersAsTrigger(true);
        meat.SetKinematic(true);

        isCooking = true;
        currentCookingTime = 0f;

        float frac = cookingTime / (cookingTime + burningTime);

        photonView.RPC(nameof(RPC_SetCookingUI), RpcTarget.AllBuffered, true, frac);

        ShowAimHint(false, false);
        meat.currentState = MeatCookingState.CookingState.Cooking;

        return true;
    }

    // ================================================================
    // VOLTEAR
    // ================================================================
    public bool TryFlipFromInteraccion()
    {
        if (!currentMeat) return false;

        PhotonView meatPV = currentMeat.GetComponent<PhotonView>();
        if (!meatPV.IsMine)
            meatPV.RequestOwnership();

        if (!isFlipped &&
            (currentMeat.isSide1Cooked || currentMeat.isSide1Burned))
        {
            currentMeat.FlipMeat();
            isFlipped = true;

            currentCookingTime = 0f;
            isCooking = true;

            float frac = cookingTime / (cookingTime + burningTime);
            photonView.RPC(nameof(RPC_SetCookingUI), RpcTarget.AllBuffered, true, frac);

            photonView.RPC(nameof(RPC_SyncMeatOnPan), RpcTarget.OthersBuffered, meatPV.ViewID, isFlipped);

            return true;
        }

        return false;
    }

    // ================================================================
    // RPC VISUAL
    // ================================================================
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

    // ================================================================
    // RPC RE-PARENT
    // ================================================================
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
    void RPC_SyncMeatOnPan(int meatViewID, bool flipped)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        MeatCookingState meat = meatPV.GetComponent<MeatCookingState>();
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
    void RPC_SetCookingUI(bool state, float frac)
    {
        if (!cookingUI) return;

        cookingUI.SetVisible(state);
        if (state) cookingUI.ResetUI(frac);
    }

    [PunRPC]
    void RPC_UpdateUIProgress(float t, float frac)
    {
        if (cookingUI)
            cookingUI.SetProgress(t, frac);
    }

    // ================================================================
    // DETENER COCCIÓN — ***AQUÍ ESTÁ EL FIX***
    // ================================================================
    void StopCookingAndHideUI()
    {
        isCooking = false;
        currentCookingTime = 0f;

        if (photonView)
            photonView.RPC(nameof(RPC_SetCookingUI), RpcTarget.AllBuffered, false, 0f);
        else if (cookingUI)
            cookingUI.SetVisible(false);

        if (currentMeat)
        {
            PhotonView meatPV = currentMeat.GetComponent<PhotonView>();

            // ?? **FIX REAL AQUÍ**
            // SOLO enviamos el RPC si LA CARNE SIGUE EN EL SARTÉN.
            if (photonView && meatPV && currentMeat.transform.parent == panCenter)
            {
                photonView.RPC(nameof(RPC_ClearMeatFromPan), RpcTarget.OthersBuffered, meatPV.ViewID);
            }

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

        MeatCookingState meat = meatPV.GetComponent<MeatCookingState>();
        if (!meat) return;

        meat.SetOnPan(false);
        meat.SetKinematic(false);
        meat.transform.SetParent(null);
    }

    // ================================================================
    // HINT
    // ================================================================
    public void ShowAimHint(bool aimingPan, bool hasMeatInHand)
    {
        bool show = aimingPan && hasMeatInHand && !isCooking && !currentMeat;

        if (cookingCanvas)
            cookingCanvas.SetActive(show);
    }

    // ================================================================
    // HELPERS
    // ================================================================
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

        if (cookingUI)
        {
            cookingUI.SetVisible(false);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }
        if (cookingCanvas)
            cookingCanvas.SetActive(false);
    }
}
