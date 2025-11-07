using UnityEngine;

public class CookMeatInPan : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] Transform panCenter;      // dónde se posa la carne
    [SerializeField] public float cookingTime = 10f;
    [SerializeField] public float burningTime = 5f;

    [Header("UI")]
    [SerializeField] GameObject cookingCanvas; // “Presiona T para cocinar”
    [SerializeField] CookingProgressUI cookingUI;

    MeatCookingState currentMeat;
    bool isCooking = false;
    float currentCookingTime = 0f;
    bool isFlipped = false;

    void Start() => ResetCookingSystem();

    void Update()
    {
        if (!currentMeat) return;

        // Tick de cocción del lado activo
        if (isCooking)
        {
            currentCookingTime += Time.deltaTime;

            float total = cookingTime + burningTime;       // tiempo total hasta quemar
            float t = Mathf.Clamp01(currentCookingTime / total);
            float frac = cookingTime / total;             // umbral de “lista”

            if (cookingUI) cookingUI.SetProgress(t, frac);

            // pasa a quemado si supera el total
            if (currentCookingTime >= total)
            {
                if (!isFlipped && !currentMeat.isSide1Burned) currentMeat.BurnSide1();
                else if (isFlipped && !currentMeat.isSide2Burned) currentMeat.BurnSide2();

                isCooking = false;
                SetMeatToBurnedOrCookedState();
            }
            // marca “cocido” cuando pasa el umbral de ese lado
            else if (currentCookingTime >= cookingTime)
            {
                if (!isFlipped && !currentMeat.isSide1Cooked && !currentMeat.isSide1Burned) currentMeat.CookSide1();
                else if (isFlipped && !currentMeat.isSide2Cooked && !currentMeat.isSide2Burned) currentMeat.CookSide2();
            }
        }

        // Si la carne ya no es hija del pan, corta
        if (currentMeat.transform.parent != panCenter) StopCookingAndHideUI();
    }

    // Empezar a cocinar (desde Interaccion)
    public bool TryStartCooking(MeatCookingState meat)
    {
        if (!meat || isCooking) return false;

        bool side1Finished = meat.isSide1Cooked || meat.isSide1Burned;
        bool side2Finished = meat.isSide2Cooked || meat.isSide2Burned;
        if (side1Finished && side2Finished) { ShowAimHint(false, false); return false; }

        currentMeat = meat;
        currentMeat.transform.SetParent(panCenter);
        currentMeat.transform.SetPositionAndRotation(panCenter.position, panCenter.rotation);

        // Si ya estaba listo el lado 1, voltea para cocinar el 2
        if (side1Finished && !side2Finished)
        {
            currentMeat.transform.rotation = panCenter.rotation;
            currentMeat.transform.Rotate(180f, 0f, 0f, Space.Self);
            isFlipped = true;
        }
        else
        {
            isFlipped = false;
        }

        currentMeat.SetOnPan(true);
        currentMeat.SetCollidersAsTrigger(true);
        currentMeat.SetKinematic(true);

        isCooking = true;
        currentCookingTime = 0f;

        if (cookingUI)
        {
            cookingUI.SetVisible(true);
            cookingUI.ResetUI(cookingTime / (cookingTime + burningTime));
        }

        ShowAimHint(false, false);
        currentMeat.currentState = MeatCookingState.CookingState.Cooking;
        return true;
    }

    // Voltear solo por Interaccion en el sartén apuntado
    public bool TryFlipFromInteraccion()
    {
        if (!currentMeat) return false;

        if (!isFlipped && (currentMeat.isSide1Cooked || currentMeat.isSide1Burned))
        {
            currentMeat.transform.Rotate(180f, 0f, 0f);
            isFlipped = true;

            currentCookingTime = 0f;
            if (cookingUI) cookingUI.ResetUI(cookingTime / (cookingTime + burningTime)); // limpia íconos

            isCooking = true;

            // ?? Reproducir sonido de cocción al voltear
            var audio = currentMeat.GetComponent<MeatCookingAudio>();
            if (audio != null)
                audio.RestartCookingSound();

            return true;
        }
        return false;
    }


    // Hint “Presiona T…” (se muestra solo si apunta, tiene carne y no hay otra en este sarten)
    public void ShowAimHint(bool aimingPan, bool hasMeatInHand)
    {
        bool show = false;

        if (aimingPan && hasMeatInHand && !isCooking && !currentMeat)
        {
            var inter = Object.FindFirstObjectByType<Interact>();
            var carne = inter ? inter.GetComponentInChildren<MeatCookingState>() : null;
            if (!carne || carne.currentState == MeatCookingState.CookingState.Raw || carne.currentState == MeatCookingState.CookingState.Cooking)
                show = true;
        }

        if (cookingCanvas) cookingCanvas.SetActive(show);
    }

    // Estado global al terminar cada lado
    void SetMeatToBurnedOrCookedState()
    {
        bool side1Finished = currentMeat.isSide1Cooked || currentMeat.isSide1Burned;
        bool side2Finished = currentMeat.isSide2Cooked || currentMeat.isSide2Burned;

        if (side1Finished && side2Finished)
            currentMeat.currentState = (currentMeat.isSide1Burned || currentMeat.isSide2Burned)
                ? MeatCookingState.CookingState.Burned
                : MeatCookingState.CookingState.Cooked;
        else if (side1Finished || side2Finished)
            currentMeat.currentState = MeatCookingState.CookingState.Cooking;
        else
            currentMeat.currentState = MeatCookingState.CookingState.Raw;
    }

    // Parar y ocultar UI (cuando se retira del sartén)
    void StopCookingAndHideUI()
    {
        isCooking = false;
        currentCookingTime = 0f;

        if (cookingCanvas) cookingCanvas.SetActive(false);
        if (cookingUI) cookingUI.SetVisible(false);

        if (currentMeat) currentMeat.SetOnPan(false);

        currentMeat = null;
        isFlipped = false;
    }

    // Estado inicial del componente
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
