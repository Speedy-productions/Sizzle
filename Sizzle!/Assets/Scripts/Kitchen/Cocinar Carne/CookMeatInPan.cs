using UnityEngine;
using Photon.Pun;

public class CookMeatInPan : MonoBehaviourPun
{
    [Header("Configuración")]
    [SerializeField] Transform panCenter;
    [SerializeField] public float cookingTime = 10f;
    [SerializeField] public float burningTime = 5f;

    [Header("UI")]
    [SerializeField] CookingProgressUI cookingUI;

    MeatCookingState currentMeat;
    bool isCooking = false;
    float currentCookingTime = 0f;
    bool isFlipped = false;   // false = lado 1, true = lado 2

    void Start() => ResetCookingSystem();

    void Update()
    {
        if (!currentMeat) return;

        PhotonView meatPV = currentMeat.GetComponent<PhotonView>();
        bool networked = PhotonNetwork.IsConnected && meatPV != null;

        // ---------------- SINGLEPLAYER ----------------
        if (!networked)
        {
            ProcessCookingTick();
            UpdateUIForOwnerAndSendIfNeeded(false);
            
            // Si la carne se saca del sartén, detener
            if (currentMeat && currentMeat.transform.parent != panCenter)
                StopCookingAndHideUI(false);

            return;
        }

        // ---------------- MULTIJUGADOR ----------------
        bool iAmOwner = meatPV.IsMine;

        // Solo el DUEÑO avanza el tiempo, cocina y controla fin
        if (!iAmOwner) return;

        ProcessCookingTick();
        UpdateUIForOwnerAndSendIfNeeded(true);

        // Si el dueño detecta que la carne salió del sartén ? parar global
        if (currentMeat && currentMeat.transform.parent != panCenter)
            StopCookingAndHideUI(true);
    }

    // ================================================================
    // PROCESO DE COCCIÓN (solo dueño o singleplayer)
    // ================================================================
    void ProcessCookingTick()
    {
        if (!isCooking || currentMeat == null) return;

        currentCookingTime += Time.deltaTime;

        float total = cookingTime + burningTime;

        // 1) Fin total (se quema lado activo)
        if (currentCookingTime >= total)
        {
            if (!isFlipped && !currentMeat.isSide1Burned)
                currentMeat.BurnSide1();
            else if (isFlipped && !currentMeat.isSide2Burned)
                currentMeat.BurnSide2();

            isCooking = false;

            // Recalcular estado global
            SetMeatToBurnedOrCookedState();

            // Estado completo a todos
            currentMeat.SyncCookingStateToOthersBuffered();

            return;
        }

        // 2) Cuando llega al punto de "cocinado" del lado actual
        if (currentCookingTime >= cookingTime)
        {
            if (!isFlipped && !currentMeat.isSide1Cooked)
                currentMeat.CookSide1();
            else if (isFlipped && !currentMeat.isSide2Cooked)
                currentMeat.CookSide2();
            // CookSideX ya se encarga de sincronizar flags + materiales
        }
    }

    // ================================================================
    // UI: SOLO EL DUEÑO CALCULA TIEMPO, Y ENVÍA A LOS DEMÁS
    // ================================================================
    void UpdateUIForOwnerAndSendIfNeeded(bool isNetworkedOwner)
    {
        if (!isCooking || !cookingUI) return;

        cookingUI.SetVisible(true);

        float total = cookingTime + burningTime;
        float t = Mathf.Clamp01(currentCookingTime / total);
        float frac = cookingTime / total;

        // Barra e iconos para este cliente (dueño o singleplayer)
        cookingUI.SetProgress(t, frac);

        // En multiplayer, el dueño manda progreso al resto
        if (isNetworkedOwner && PhotonNetwork.IsConnected && currentMeat != null)
        {
            photonView.RPC(nameof(RPC_UpdateUIProgress), RpcTarget.Others, t, frac);
        }
    }

    // ================================================================
    // INICIAR COCCIÓN
    // ================================================================
    public bool TryStartCooking(MeatCookingState meat)
    {
        if (!meat || isCooking) return false;

        bool side1Finished = meat.isSide1Cooked || meat.isSide1Burned;
        bool side2Finished = meat.isSide2Cooked || meat.isSide2Burned;

        // Ambos lados ya terminados: no volver a cocinar
        if (side1Finished && side2Finished)
        {
            ShowAimHint(false, false);
            return false;
        }

        currentMeat = meat;

        PhotonView meatPV = meat.GetComponent<PhotonView>();
        PhotonView panPV = GetComponent<PhotonView>();

        // ---------------- SINGLEPLAYER ----------------
        if (meatPV == null || panPV == null || !PhotonNetwork.IsConnected)
        {
            // Parenting local
            meat.transform.SetParent(panCenter);
            meat.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);

            // Elegir lado inicial
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

            isCooking = true;
            currentCookingTime = 0f;

            float fracSP = cookingTime / (cookingTime + burningTime);
            if (cookingUI)
            {
                cookingUI.SetVisible(true);
                cookingUI.ResetUI(fracSP);
            }
            ShowAimHint(false, false);
            meat.currentState = MeatCookingState.CookingState.Cooking;

            return true;
        }

        // ---------------- MULTIJUGADOR ----------------

        // Asegurar ownership de la carne
        if (!meatPV.IsMine)
            meatPV.RequestOwnership();

        // Colocar carne en el sartén en TODOS
        panPV.RPC(nameof(RPC_SetMeatOnPan), RpcTarget.AllBuffered, meatPV.ViewID);

        // Elegir si empezamos por lado 2
        bool startFlipped = side1Finished && !side2Finished;
        isFlipped = startFlipped;

        // Compartir referencia y flags básicos a todos
        panPV.RPC(nameof(RPC_BeginCookingSharedState), RpcTarget.AllBuffered, meatPV.ViewID, isFlipped);

        // Si empezamos en el lado 2, flip inicial real en todos
        if (startFlipped)
        {
            photonView.RPC(nameof(RPC_DoFlip), RpcTarget.AllBuffered, meatPV.ViewID);
        }

        // Dueño: marca flags y arranca timer
        isCooking = true;
        currentCookingTime = 0f;

        float frac = cookingTime / (cookingTime + burningTime);

        // UI visible y reseteada para todos
        photonView.RPC(nameof(RPC_SetCookingUI), RpcTarget.AllBuffered, true, frac);

        ShowAimHint(false, false);
        meat.currentState = MeatCookingState.CookingState.Cooking;

        return true;
    }

    // Todos los clientes sincronizan referencia y flags base
    [PunRPC]
    void RPC_BeginCookingSharedState(int meatViewID, bool flipped)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        MeatCookingState meat = meatPV.GetComponent<MeatCookingState>();
        if (!meat) return;

        currentMeat = meat;
        isFlipped = flipped;
        isCooking = true;
        currentCookingTime = 0f;
    }

    // ================================================================
    // VOLTEAR (Q) – SISTEMA DE PETICIÓN AL DUEÑO
    // ================================================================
    public bool TryFlipFromInteraccion()
    {
        if (!currentMeat) return false;

        PhotonView meatPV = currentMeat.GetComponent<PhotonView>();

        // SINGLEPLAYER: flip directo local
        if (!PhotonNetwork.IsConnected || meatPV == null)
        {
            return TryFlipLocal();
        }

        // MULTIPLAYER: petición de flip al dueño
        photonView.RPC(nameof(RPC_RequestFlip), RpcTarget.All, meatPV.ViewID);
        return true;
    }

    // Singleplayer: lógica local de flip
    bool TryFlipLocal()
    {
        if (!currentMeat) return false;

        if (isFlipped) return false;
        if (!(currentMeat.isSide1Cooked || currentMeat.isSide1Burned)) return false;

        // Rotar localmente
        currentMeat.transform.SetParent(panCenter, false);
        currentMeat.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);
        currentMeat.transform.Rotate(180f, 0f, 0f, Space.Self);

        currentMeat.SetOnPan(true);
        currentMeat.SetCollidersAsTrigger(true);
        currentMeat.SetKinematic(true);

        isFlipped = true;
        isCooking = true;
        currentCookingTime = 0f;

        float frac = cookingTime / (cookingTime + burningTime);
        if (cookingUI)
        {
            cookingUI.SetVisible(true);
            cookingUI.ResetUI(frac);
        }

        return true;
    }

    // Petición global, pero SOLO el dueño la procesa
    [PunRPC]
    void RPC_RequestFlip(int meatViewID)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        // Solo el dueño de esa carne responde a la petición
        if (!meatPV.IsMine) return;

        // Debe ser la carne que este sartén está cocinando
        if (!currentMeat || currentMeat.GetComponent<PhotonView>() != meatPV) return;

        // No permitir flip doble
        if (isFlipped) return;

        // Solo cuando el lado 1 esté "resuelto"
        if (!(currentMeat.isSide1Cooked || currentMeat.isSide1Burned)) return;

        // Hacer flip REAL en el dueño
        DoFlipOwner(meatPV);
    }

    void DoFlipOwner(PhotonView meatPV)
    {
        // Flip físico + flags en TODOS
        photonView.RPC(nameof(RPC_DoFlip), RpcTarget.AllBuffered, meatPV.ViewID);

        isFlipped = true;
        isCooking = true;
        currentCookingTime = 0f;

        float frac = cookingTime / (cookingTime + burningTime);
        photonView.RPC(nameof(RPC_SetCookingUI), RpcTarget.AllBuffered, true, frac);
    }

    // Flip real en TODOS los clientes
    [PunRPC]
    void RPC_DoFlip(int meatViewID)
    {
        PhotonView meatPV = PhotonView.Find(meatViewID);
        if (!meatPV) return;

        MeatCookingState meat = meatPV.GetComponent<MeatCookingState>();
        if (!meat) return;

        // Reparent y centrar en sartén
        meat.transform.SetParent(panCenter, false);
        meat.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);

        // Rotar 180º
        meat.transform.Rotate(180f, 0f, 0f, Space.Self);

        // Flags coherentes
        meat.SetOnPan(true);
        meat.SetCollidersAsTrigger(true);
        meat.SetKinematic(true);

        // En cada cliente, este sartén se sincroniza
        currentMeat = meat;
        isFlipped = true;
        isCooking = true;
        currentCookingTime = 0f;
    }

    // ================================================================
    // RPC RE-PARENT INICIAL
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

        MeatCookingState meat = meatPV.GetComponent<MeatCookingState>();
        if (meat)
        {
            meat.SetOnPan(true);
            meat.SetCollidersAsTrigger(true);
            meat.SetKinematic(true);
        }
    }

    // ================================================================
    // UI RPCs (no dueños solo reciben esto)
    // ================================================================
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
        // Los no-dueños usan esto como única fuente de progreso
        if (cookingUI)
            cookingUI.SetProgress(t, frac);
    }

    // ================================================================
    // DETENER COCCIÓN (solo dueño en multi)
    // ================================================================
    void StopCookingAndHideUI(bool networked)
{
    isCooking = false;
    currentCookingTime = 0f;

    // Apagar UI para todos
    if (networked && photonView)
        photonView.RPC(nameof(RPC_SetCookingUI), RpcTarget.AllBuffered, false, 0f);
    else if (cookingUI)
        cookingUI.SetVisible(false);

    if (currentMeat)
    {
        // ?? No mover la carne, no soltarla, no tocar parent
        currentMeat.SetOnPan(false);

        // ?? Y nunca llamar a RPC_ClearMeatFromPan aquí
        // Ese RPC lo eliminamos por completo
    }

    currentMeat = null;
    isFlipped = false;
}

public bool IsCookingThis(MeatCookingState meat)
{
    return currentMeat == meat;
}

    public void ForceStopCooking()
{
    StopCookingAndHideUI(PhotonNetwork.IsConnected);
}

    // ================================================================
    // HINT
    // ================================================================
    public void ShowAimHint(bool aimingPan, bool hasMeatInHand)
    {
        bool show = aimingPan && hasMeatInHand && !isCooking && !currentMeat;
    }

    // ================================================================
    // HELPERS
    // ================================================================
    void SetMeatToBurnedOrCookedState()
    {
        if (currentMeat == null) return;

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
    }
}
