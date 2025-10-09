using UnityEngine;

public class CookFriesInFryer : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] Transform basketCenter;   // Punto donde posas las papas (cesta)
    [SerializeField] float cookingTime = 8f;   // Tiempo a “lista”
    [SerializeField] float burningTime = 5f;   // De lista ? quemada

    [Header("UI")]
    [SerializeField] GameObject fryerCanvasHint; // Canvas “Presiona T para freír”
    [SerializeField] CookingProgressUI cookingUI;

    [Header("Referencias")]
    public GameObject[] decorativeFries;              // Papas decorativas (prefabs decorativos en la freidora)

    [Header("Materiales")]
    public Material rawMaterial;                      // Material para las papas crudas
    public Material cookedMaterial;                   // Material para las papas cocidas
    public Material burnedMaterial;                   // Material para las papas quemadas

    private FriesCookingState currentFries;
    private bool isCooking = false;
    private float timeAcc = 0f;

    private bool hasPlayerFries = false;
    private float currentCookingTime = 0f;

    void Start() => ResetSystem();

    void Update()
    {
        if (!currentFries) return;

        if (isCooking)
        {
            timeAcc += Time.deltaTime;

            float total = cookingTime + burningTime;
            float t = Mathf.Clamp01(timeAcc / total);
            float frac = cookingTime / total; // Umbral de “lista”
            if (cookingUI) cookingUI.SetProgress(t, frac);

            if (timeAcc >= total)
            {
                currentFries.MarkBurned();
                SetDecorativeFriesToBurned();  // Asegurarse de quemar las papas decorativas también
            }
            else if (timeAcc >= cookingTime && currentFries.currentState != FriesCookingState.CookingState.Cooked)
            {
                currentFries.MarkCooked();
                SetDecorativeFriesToCooked();  // Cambiar las papas decorativas a cocidas
            }
        }

        // Si alguien retiró las papas de la cesta
        if (currentFries && currentFries.transform.parent != basketCenter)
            StopAll();
    }

    public bool TryStartCooking(FriesCookingState fries)
    {
        if (!fries || isCooking) return false;
        if (fries.currentState == FriesCookingState.CookingState.Cooked ||
            fries.currentState == FriesCookingState.CookingState.Burned)
            return false;

        currentFries = fries;

        // No manipulamos la escala aquí

        // Asignar la posición del basketCenter sin afectar la escala
        currentFries.transform.SetParent(basketCenter);
        currentFries.transform.position = basketCenter.position;
        currentFries.transform.rotation = basketCenter.rotation;

        currentFries.transform.rotation = Quaternion.Euler(76.13f, 4.7f, 90.32f); // Los valores de rotación que has proporcionado

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

        // Activar las papas decorativas (cuando se empieza a cocinar)
        AddPlayerFriesToFryer();

        return true;
    }

    void StopCookingUIOnly()
    {
        isCooking = false;
        timeAcc = 0f;
        if (cookingUI) cookingUI.SetVisible(false);
    }

    public void ShowAimHint(bool aimingFryer, bool hasFriesInHand)
    {
        bool show = false;

        if (aimingFryer && hasFriesInHand && !isCooking && !currentFries)
        {
            var inter = FindObjectOfType<Interact>();
            var fries = inter ? inter.GetComponentInChildren<FriesCookingState>() : null;
            if (fries && (fries.currentState == FriesCookingState.CookingState.Raw ||
                          fries.currentState == FriesCookingState.CookingState.Cooking))
                show = true;
        }

        if (fryerCanvasHint) fryerCanvasHint.SetActive(show);
    }

    void StopAll()
    {
        StopCookingUIOnly();
        if (fryerCanvasHint) fryerCanvasHint.SetActive(false);

        if (currentFries)
        {
            currentFries.SetInFryer(false);
            currentFries = null;
        }

        // Cuando las papas del jugador son retiradas, las papas decorativas deben resetearse
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
    }

    #region Papas Decorativas (Gestionando Materiales y Estado)

    public void AddPlayerFriesToFryer()
    {
        // Si las papas ya están en la freidora, no hacer nada
        if (hasPlayerFries) return;

        // Activar las papas decorativas y empezar la cocción
        SetDecorativeFriesToCooking();
        isCooking = true;
        hasPlayerFries = true;
        currentCookingTime = 0f;  // Reiniciar el tiempo de cocción
        cookingUI.SetVisible(true);  // Mostrar el UI de progreso cuando las papas empiezan a cocinarse
    }

    public void RemovePlayerFriesFromFryer()
    {
        // Desactivar las papas decorativas
        SetDecorativeFriesToRaw();

        // Detener la cocción y resetear el sistema
        isCooking = false;
        hasPlayerFries = false;
        currentCookingTime = 0f;
        cookingUI.SetVisible(false);  // Ocultar el UI de progreso cuando se retiran las papas
    }

    private void SetDecorativeFriesToCooking()
    {
        // Activar las papas decorativas
        foreach (var fry in decorativeFries)
        {
            if (fry != null)
            {
                Renderer fryRenderer = fry.GetComponent<Renderer>();
                if (fryRenderer)
                {
                    fryRenderer.material = rawMaterial; // Asignar material de papas crudas
                }
                fry.SetActive(true);
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
                    fryRenderer.material = cookedMaterial; // Cambiar material a cocinado
                }
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
                    fryRenderer.material = burnedMaterial; // Cambiar material a quemado
                }
            }
        }
    }

    private void SetDecorativeFriesToRaw()
    {
        foreach (var fry in decorativeFries)
        {
            if (fry != null)
            {
                fry.SetActive(false); // Desactivar las papas decorativas

                Renderer fryRenderer = fry.GetComponent<Renderer>();
                if (fryRenderer && rawMaterial)
                {
                    fryRenderer.material = rawMaterial; // Cambiar material a crudo
                }
            }
        }
    }

    #endregion
}
