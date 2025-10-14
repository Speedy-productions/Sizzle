using UnityEngine;

public class CookFriesInFryer : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] Transform basketCenter;
    [SerializeField] float cookingTime = 8f;
    [SerializeField] float burningTime = 5f;

    [Header("UI")]
    [SerializeField] GameObject fryerCanvasHint;
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
    private float currentCookingTime = 0f;

    void Start()
    {
        Debug.Log($"[FRYER] Start -> ResetSystem() en {name}");
        ResetSystem();
    }

    void Update()
    {
        if (!currentFries)
            return;

        if (isCooking)
        {
            timeAcc += Time.deltaTime;

            float total = cookingTime + burningTime;
            float t = Mathf.Clamp01(timeAcc / total);
            float frac = cookingTime / total;
            if (cookingUI) cookingUI.SetProgress(t, frac);

            if (timeAcc >= total)
            {
                Debug.Log($"[FRYER] {name} -> Tiempo total alcanzado. Marcando BURNED a {currentFries.name}");
                currentFries.MarkBurned();
                SetDecorativeFriesToBurned();
            }
            else if (timeAcc >= cookingTime && currentFries.currentState != FriesCookingState.CookingState.Cooked)
            {
                Debug.Log($"[FRYER] {name} -> Alcanzó tiempo de COOKED para {currentFries.name}");
                currentFries.MarkCooked();
                SetDecorativeFriesToCooked();
            }
        }

        // Detecta si sacaron las papas de la cesta
        if (currentFries && currentFries.transform.parent != basketCenter)
        {
            Debug.LogWarning($"[FRYER] {name} -> currentFries ya no tiene parent basketCenter. StopAll()");
            StopAll();
        }
    }

    public bool TryStartCooking(FriesCookingState fries)
    {
        Debug.Log($"[FRYER] TryStartCooking() llamado con: {(fries ? fries.name : "NULL")}");

        if (!fries || isCooking)
        {
            Debug.LogWarning($"[FRYER] TryStartCooking() -> Abort. fries={(fries != null)}, isCooking={isCooking}");
            return false;
        }

        if (fries.currentState == FriesCookingState.CookingState.Cooked ||
            fries.currentState == FriesCookingState.CookingState.Burned)
        {
            Debug.LogWarning($"[FRYER] TryStartCooking() -> Abort. Estado actual={fries.currentState}");
            return false;
        }

        currentFries = fries;

        // Logear info del objeto
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

        // (deja el Debug.Log que ya tienes debajo)

        currentFries.SetInFryer(true);

        isCooking = true;
        timeAcc = 0f;

        if (cookingUI)
        {
            cookingUI.SetVisible(true);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }

        ShowAimHint(false, false);
        currentFries.currentState = FriesCookingState.CookingState.Cooking;

        AddPlayerFriesToFryer();

        Debug.Log($"[FRYER] Cocción iniciada para {currentFries.name} en {name}");
        return true;
    }

    void StopCookingUIOnly()
    {
        isCooking = false;
        timeAcc = 0f;
        if (cookingUI) cookingUI.SetVisible(false);
        Debug.Log($"[FRYER] StopCookingUIOnly() en {name}");
    }

    public void ShowAimHint(bool aimingFryer, bool hasFriesInHand)
    {
        bool show = false;

        if (aimingFryer && hasFriesInHand && !isCooking && !currentFries)
        {
            var inter = FindObjectOfType<Interact>();
            var fries = inter ? inter.GetComponentInChildren<FriesCookingState>() : null;

            Debug.Log($"[FRYER] ShowAimHint() aiming={aimingFryer} hasFriesInHand={hasFriesInHand} inter={(inter != null)} fries={(fries != null)} estado={(fries ? fries.currentState : 0)}");

            if (fries && (fries.currentState == FriesCookingState.CookingState.Raw ||
                          fries.currentState == FriesCookingState.CookingState.Cooking))
                show = true;
        }

        if (fryerCanvasHint) fryerCanvasHint.SetActive(show);
        Debug.Log($"[FRYER] ShowAimHint() -> SetActive({show})");
    }

    void StopAll()
    {
        Debug.Log($"[FRYER] StopAll() en {name}. currentFries={(currentFries ? currentFries.name : "NULL")}");
        StopCookingUIOnly();
        if (fryerCanvasHint) fryerCanvasHint.SetActive(false);

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

        if (fryerCanvasHint) fryerCanvasHint.SetActive(false);
        if (cookingUI)
        {
            cookingUI.SetVisible(false);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }

        Debug.Log($"[FRYER] ResetSystem() en {name}");
    }

    #region Papas Decorativas (logs incluidos)

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
        currentCookingTime = 0f;
        if (cookingUI) cookingUI.SetVisible(true);

        Debug.Log($"[FRYER] Decorativas -> Cooking (activadas) en {name}");
    }

    public void RemovePlayerFriesFromFryer()
    {
        SetDecorativeFriesToRaw();
        isCooking = false;
        hasPlayerFries = false;
        currentCookingTime = 0f;
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
                if (fryRenderer)
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
