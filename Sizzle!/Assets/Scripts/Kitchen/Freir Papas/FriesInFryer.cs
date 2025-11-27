using UnityEngine;
using Photon.Pun;

public class CookFriesInFryer : MonoBehaviourPun
{
    [Header("Config")]
    [SerializeField] Transform basketCenter;
    [SerializeField] float cookingTime = 8f;
    [SerializeField] float burningTime = 5f;

    [Header("UI")]
    [SerializeField] CookingProgressUI cookingUI;

    [Header("Referencias")]
    public GameObject[] decorativeFries;

    [Header("Materiales")]
    public Material rawMaterial;
    public Material cookedMaterial;
    public Material burnedMaterial;

    private FriesCookingState currentFries;
    private bool isCooking = false;
    private float timeAcc = 0f;

    private bool hasPlayerFries = false;

    // flags para no enviar RPCs duplicados
    private bool cookedBroadcasted = false;
    private bool burnedBroadcasted = false;

    private Interact inter;

    void Start()
    {
        Debug.Log($"[FRYER] Start -> ResetSystem() en {name}");
        inter = Object.FindFirstObjectByType<Interact>();
        ResetSystem();
        ApplyFryerUpgrade();
    }

    void Update()
    {
        // Sin papas asignadas, nada que hacer
        if (!currentFries)
            return;

        if (isCooking)
        {
            timeAcc += Time.deltaTime;

            float total = cookingTime + burningTime;
            float t = Mathf.Clamp01(timeAcc / total);
            float frac = cookingTime / total;

            // La UI se mueve en todos los clientes
            if (cookingUI) cookingUI.SetProgress(t, frac);

            // Sincronizar estado COOKED / BURNED por RPC una sola vez
            if (timeAcc >= total && !burnedBroadcasted)
            {
                burnedBroadcasted = true;
                cookedBroadcasted = true;

                if (PhotonNetwork.IsConnected && photonView != null)
                    photonView.RPC(nameof(RPC_OnFriesBurned), RpcTarget.AllBuffered);
                else
                    OnFriesBurnedLocal();
            }
            else if (timeAcc >= cookingTime && !cookedBroadcasted)
            {
                cookedBroadcasted = true;

                if (PhotonNetwork.IsConnected && photonView != null)
                    photonView.RPC(nameof(RPC_OnFriesCooked), RpcTarget.AllBuffered);
                else
                    OnFriesCookedLocal();
            }
        }

        // Detecta si sacaron las papas de la cesta (esto es verdad en TODOS porque el reparent se hace por RPC)
        if (currentFries && currentFries.transform.parent != basketCenter)
        {
            Debug.Log($"[FRYER] {name} -> currentFries ya no tiene parent basketCenter. StopAll()");
            StopAll();
        }
    }

    // --------------------------------------------------------------------------------
    // ENTRADA PÚBLICA DESDE Interact
    // --------------------------------------------------------------------------------
    public bool TryStartCooking(FriesCookingState fries)
    {
        Debug.Log($"[FRYER] TryStartCooking() llamado con: {(fries ? fries.name : "NULL")}");

        ApplyFryerUpgrade();

        if (!fries || isCooking || currentFries != null)
        {
            Debug.Log($"[FRYER] TryStartCooking() -> Abort. fries={(fries != null)}, isCooking={isCooking}, currentFries={(currentFries != null)}");
            return false;
        }

        if (fries.currentState == FriesCookingState.CookingState.Cooked ||
            fries.currentState == FriesCookingState.CookingState.Burned)
        {
            Debug.LogWarning($"[FRYER] TryStartCooking() -> Abort. Estado actual={fries.currentState}");
            return false;
        }

        PhotonView friesPV = fries.GetComponent<PhotonView>();

        // MULTIJUGADOR: mandar RPC para todos
        if (PhotonNetwork.IsConnected && friesPV != null)
        {
            photonView.RPC(nameof(RPC_StartCooking), RpcTarget.AllBuffered, friesPV.ViewID);
        }
        else
        {
            // SINGLEPLAYER u objeto sin PhotonView
            StartCookingLocal(fries);
        }

        Debug.Log($"[FRYER] Cocción iniciada (solicitada) para {fries.name} en {name}");
        return true;
    }

    // Lógica local de inicio de cocción (usada por singleplayer y RPC)
    void StartCookingLocal(FriesCookingState fries)
    {
        if (!fries) return;

        currentFries = fries;

        // Info debug
        string tagInfo = currentFries.tag;
        int layerInfo = currentFries.gameObject.layer;
        Debug.Log($"[FRYER] Aceptado {currentFries.name} (tag={tagInfo}, layer={layerInfo}, estado={currentFries.currentState})");

        // Parenting y colocación
        var t = currentFries.transform;

        // Entra al espacio del basket SIN conservar mundo (control total en local)
        t.SetParent(basketCenter, false);

        // Centrar en la cesta
        t.localPosition = Vector3.zero;

        // Rotación EXACTA del basket (hereda la orientación del parent)
        t.localRotation = Quaternion.identity;

        // Mantener escala mundial del prefab (evita deformación al meter/sacar)
        Vector3 Sp = basketCenter.lossyScale;
        Vector3 Sw = currentFries.initialWorldScale;
        t.localScale = new Vector3(
            Sp.x != 0f ? Sw.x / Sp.x : t.localScale.x,
            Sp.y != 0f ? Sw.y / Sp.y : t.localScale.y,
            Sp.z != 0f ? Sw.z / Sp.z : t.localScale.z
        );

        // Marcar estado físico y lógico
        currentFries.SetInFryer(true);
        currentFries.currentState = FriesCookingState.CookingState.Cooking;

        isCooking = true;
        timeAcc = 0f;
        cookedBroadcasted = false;
        burnedBroadcasted = false;

        if (cookingUI)
        {
            cookingUI.SetVisible(true);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }

        // Mientras haya papas dentro, no queremos hint de "mete papas"
        ShowAimHint(false, false);

        AddPlayerFriesToFryer();

        Debug.Log($"[FRYER] Cocción iniciada LOCAL para {currentFries.name} en {name}");
    }

    // --------------------------------------------------------------------------------
    // RPCs
    // --------------------------------------------------------------------------------

    [PunRPC]
    void RPC_StartCooking(int friesViewID)
    {
        PhotonView friesPV = PhotonView.Find(friesViewID);
        if (!friesPV)
        {
            Debug.LogWarning($"[FRYER] RPC_StartCooking -> No se encontró PhotonView con ID {friesViewID}");
            return;
        }

        FriesCookingState fries = friesPV.GetComponent<FriesCookingState>();
        if (!fries)
        {
            Debug.LogWarning($"[FRYER] RPC_StartCooking -> El objeto con ViewID {friesViewID} no tiene FriesCookingState");
            return;
        }

        StartCookingLocal(fries);
    }

    [PunRPC]
    void RPC_OnFriesCooked()
    {
        OnFriesCookedLocal();
        cookedBroadcasted = true;
    }

    [PunRPC]
    void RPC_OnFriesBurned()
    {
        OnFriesBurnedLocal();
        burnedBroadcasted = true;
        cookedBroadcasted = true;
    }

    // --------------------------------------------------------------------------------
    // Eventos locales de cambio de estado (invocados desde RPC o singleplayer)
    // --------------------------------------------------------------------------------

    void OnFriesCookedLocal()
    {
        if (currentFries)
        {
            Debug.Log($"[FRYER] {name} -> Marcando COOKED a {currentFries.name} (local/RPC)");
            currentFries.MarkCooked();
        }
        SetDecorativeFriesToCooked();
    }

    void OnFriesBurnedLocal()
    {
        if (currentFries)
        {
            Debug.Log($"[FRYER] {name} -> Marcando BURNED a {currentFries.name} (local/RPC)");
            currentFries.MarkBurned();
        }
        SetDecorativeFriesToBurned();
    }

    // --------------------------------------------------------------------------------
    // UI / HINT
    // --------------------------------------------------------------------------------

    void StopCookingUIOnly()
    {
        isCooking = false;
        timeAcc = 0f;
        cookedBroadcasted = false;
        burnedBroadcasted = false;

        if (cookingUI) cookingUI.SetVisible(false);
        Debug.Log($"[FRYER] StopCookingUIOnly() en {name}");
    }

    public void ShowAimHint(bool aimingFryer, bool hasFriesInHand)
    {
        bool show = false;

        // Si ya hay papas dentro o se está cocinando, NO mostrar hint
        if (isCooking || currentFries != null)
        {
            show = false;
        }
        else if (aimingFryer && hasFriesInHand)
        {
            var inter = Object.FindAnyObjectByType<Interact>();
            var fries = inter ? inter.GetComponentInChildren<FriesCookingState>() : null;

            Debug.Log($"[FRYER] ShowAimHint() aiming={aimingFryer} hasFriesInHand={hasFriesInHand} inter={(inter != null)} fries={(fries != null)} estado={(fries ? fries.currentState : 0)}");

            if (fries && (fries.currentState == FriesCookingState.CookingState.Raw ||
                          fries.currentState == FriesCookingState.CookingState.Cooking))
                show = true;
        }
    }

    // --------------------------------------------------------------------------------
    // RESET / STOP
    // --------------------------------------------------------------------------------

    void StopAll()
    {
        Debug.Log($"[FRYER] StopAll() en {name}. currentFries={(currentFries ? currentFries.name : "NULL")}");
        StopCookingUIOnly();

        if (currentFries)
        {
            currentFries.SetInFryer(false);
            currentFries = null;
        }

        RemovePlayerFriesFromFryer();
    }

    void ResetSystem()
    {
        isCooking = false;
        timeAcc = 0f;
        currentFries = null;
        cookedBroadcasted = false;
        burnedBroadcasted = false;

        if (cookingUI)
        {
            cookingUI.SetVisible(false);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }

        Debug.Log($"[FRYER] ResetSystem() en {name}");
    }

    // --------------------------------------------------------------------------------
    // UPGRADE FRYER
    // --------------------------------------------------------------------------------
    
    void ApplyFryerUpgrade()
    {
        if (UpgradeManager.Instance == null) return;

        int level = UpgradeManager.Instance.fryerLevel;

        switch (level)
        {
            case 0:
                cookingTime = 8f;
                burningTime = 5f;
                break;
            case 1:
                cookingTime = 8f;
                burningTime = 8f;
                break;
            case 2:
                cookingTime = 5f;
                burningTime = 8f;
                break;
            case 3:
                cookingTime = 3f;
                burningTime = 10f;
                break;
            default:
                cookingTime = 8f;
                burningTime = 5f;
                break;
        }

    }

    #region Papas Decorativas (logs incluidos)
    // --------------------------------------------------------------------------------

    public void AddPlayerFriesToFryer()
    {
        if (hasPlayerFries)
        {
            Debug.Log($"[FRYER] AddPlayerFriesToFryer() ignorado: ya había papas decorativas activas.");
            return;
        }

        SetDecorativeFriesToCooking();
        isCooking = true;
        hasPlayerFries = true;

        if (cookingUI) cookingUI.SetVisible(true);

        Debug.Log($"[FRYER] Decorativas -> Cooking (activadas) en {name}");
    }

    public void RemovePlayerFriesFromFryer()
    {
        SetDecorativeFriesToRaw();
        hasPlayerFries = false;

        if (cookingUI) cookingUI.SetVisible(false);

        Debug.Log($"[FRYER] Decorativas -> Raw (desactivadas) en {name}");
    }

    private void SetDecorativeFriesToCooking()
    {
        foreach (var fry in decorativeFries)
        {
            if (fry != null)
            {
                Renderer fryRenderer = fry.GetComponent<Renderer>();
                if (fryRenderer && rawMaterial)
                {
                    fryRenderer.material = rawMaterial;
                }
                fry.SetActive(true);
                Debug.Log($"[FRYER] Deco '{fry.name}' -> ACTIVE + RAW");
            }
        }
    }

    public void SetDecorativeFriesToCooked()
    {
        foreach (var fry in decorativeFries)
        {
            if (fry != null)
            {
                Renderer fryRenderer = fry.GetComponent<Renderer>();
                if (fryRenderer && cookedMaterial)
                {
                    fryRenderer.material = cookedMaterial;
                }
                Debug.Log($"[FRYER] Deco '{fry.name}' -> COOKED");
            }
        }
    }

    public void SetDecorativeFriesToBurned()
    {
        foreach (var fry in decorativeFries)
        {
            if (fry != null)
            {
                Renderer fryRenderer = fry.GetComponent<Renderer>();
                if (fryRenderer && burnedMaterial)
                {
                    fryRenderer.material = burnedMaterial;
                }
                Debug.Log($"[FRYER] Deco '{fry.name}' -> BURNED");
            }
        }
    }

    private void SetDecorativeFriesToRaw()
    {
        foreach (var fry in decorativeFries)
        {
            if (fry != null)
            {
                fry.SetActive(false);
                Renderer fryRenderer = fry.GetComponent<Renderer>();
                if (fryRenderer && rawMaterial)
                {
                    fryRenderer.material = rawMaterial;
                }
                Debug.Log($"[FRYER] Deco '{fry.name}' -> DISABLED + RAW");
            }
        }
    }

    #endregion
}
