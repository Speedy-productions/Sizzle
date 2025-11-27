using UnityEngine;
using System.Collections;

[System.Serializable]
public class FoodGroup
{
    public string name;
    public Sprite[] variants;
}

[System.Serializable]
public class BubbleGroup
{
    public string name;
    public Sprite[] variants;
}

[System.Serializable]
public class FriesGroup
{
    public string name;
    public Sprite[] variants;
}


public class PopupChar : MonoBehaviour
{
    [Header("Prefabs y Sprites")]
    public Popup popupPrefab;           
    public FoodGroup[] foodGroups;      
    public BubbleGroup[] bubbleGroups;  
    [Header("Papas Sprites")]
public FriesGroup[] friesGroups;


    [Header("Caras del NPC (faces-states)")]
    public Sprite smileyFace;           
    public Sprite angryFace;           

    [Header("Configuración")]
    public float switchInterval = 1f;
    public float popupDuration = 5f;

    [HideInInspector] public NpcFollowPath npcFollowPath;

    private Popup currentPopup;
    private Popup facePopup;
    private bool notifyCoroutineRunning = false;

    private void Start()
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogError("OrderManager no inicializado antes de PopupChar");
        }
    }

    // ===================== Pedido (Hamburguesa + Papas) =====================
    public void ShowPopup()
    {
        if (popupPrefab == null)
        {
            Debug.LogError("Popup prefab no asignado");
            return;
        }

        if (currentPopup != null)
        {
            Debug.LogWarning("Popup ya activo. Ignorando ShowPopup() duplicado.");
            return;
        }

        // Orden de hamburguesa desde el NPC
        Order burgerOrder = npcFollowPath != null ? npcFollowPath.GetAssignedOrder() : null;
        if (burgerOrder == null)
        {
            Debug.LogError("[PopupChar] NPC no tiene una orden asignada.");
            return;
        }

        // Orden de papas desde el OrderManager
        Order friesOrder = OrderManager.Instance != null 
            ? OrderManager.Instance.CurrentFriesOrder 
            : null;
            Debug.Log("FriesOrder es NULL? " + (friesOrder == null));

        currentPopup = Instantiate(
            popupPrefab,
            transform.position + Vector3.up * 2f,
            Quaternion.identity
        );

        StartCoroutine(AlternateHamburgerAndFries(burgerOrder, friesOrder, popupDuration));
        StartCoroutine(DestroyPopupAfterTime());

        if (!notifyCoroutineRunning)
            StartCoroutine(NotifyNpcAfterPopup());
    }

    private IEnumerator NotifyNpcAfterPopup()
    {
        notifyCoroutineRunning = true;
        yield return new WaitForSeconds(popupDuration);
        npcFollowPath?.OnPopupClosed();
        notifyCoroutineRunning = false;
    }

    // ===================== Muestra Hamburguesa y luego Papas =====================
    private IEnumerator AlternateHamburgerAndFries(Order burgerOrder, Order friesOrder, float duration)
{
    if (currentPopup == null) yield break;

    Sprite bubbleSprite = GetRandomBubbleAnyGroup();

    // --------- HAMBURGUESA ---------
    foreach (string ingredient in burgerOrder.ingredients)
    {
        if (currentPopup == null) yield break;

        Sprite foodSprite = GetSpriteByName(ingredient);
        currentPopup.Show(transform, foodSprite, bubbleSprite, null);
        yield return new WaitForSeconds(switchInterval);
    }

    // ? PEQUEÑA PAUSA ENTRE HAMBURGUESA Y PAPAS
    yield return new WaitForSeconds(0.25f);

    // --------- PAPAS ---------
    if (friesOrder != null)
    {
        Sprite friesSprite = GetRandomFriesSprite();

        Debug.Log("Fries sprite null? " + (friesSprite == null));

        if (friesSprite != null && currentPopup != null)
        {
            currentPopup.Show(transform, friesSprite, bubbleSprite, null);
            yield return new WaitForSeconds(switchInterval);
        }
    }
}


    // ===================== Destrucción segura del popup =====================
    private IEnumerator DestroyPopupAfterTime()
    {
        yield return new WaitForSeconds(popupDuration);

        if (currentPopup != null)
        {
            Destroy(currentPopup.gameObject);
            currentPopup = null;
        }
    }

    // ===================== Sprites =====================
    private Sprite GetSpriteByName(string name)
    {
        if (foodGroups == null) return null;

        foreach (FoodGroup group in foodGroups)
        {
            if (group.name != name) continue;

            if (group.variants != null && group.variants.Length > 0)
            {
                int idx = Random.Range(0, group.variants.Length);
                return group.variants[idx];
            }
        }

        Debug.LogWarning("No se encontró ingrediente con el nombre: " + name);
        return null;
    }

    private Sprite GetRandomFriesSprite()
{
    if (friesGroups == null || friesGroups.Length == 0)
        return null;

    FriesGroup g = friesGroups[Random.Range(0, friesGroups.Length)];

    if (g.variants != null && g.variants.Length > 0)
        return g.variants[Random.Range(0, g.variants.Length)];

    return null;
}


    // ===================== Resultado (cara + bubble + texto) =====================
    public void MostrarCaraFeliz(string mensaje = "¡Bien hecho!")
    {
        Sprite bubble = GetBubbleByGroup("Calmado");
        MostrarCara(smileyFace, bubble, mensaje, 32f);
    }

    public void MostrarCaraMolesta(string mensaje = "¿Qué es esta $#*!?")
    {
        Sprite bubble = GetBubbleByGroup("Grosero");
        MostrarCara(angryFace, bubble, mensaje, 26f);
    }

    private void MostrarCara(Sprite faceSprite, Sprite bubbleSprite, string mensaje, float fontSize)
    {
        if (popupPrefab == null || faceSprite == null) return;

        if (facePopup != null)
            Destroy(facePopup.gameObject);

        facePopup = Instantiate(
            popupPrefab,
            transform.position + Vector3.up * 2.5f,
            Quaternion.identity
        );

        facePopup.Show(transform, null, bubbleSprite, faceSprite, mensaje, fontSize);
        StartCoroutine(OcultarCara());
    }

    private IEnumerator OcultarCara()
    {
        yield return new WaitForSeconds(4f);

        if (facePopup != null)
        {
            Destroy(facePopup.gameObject);
            facePopup = null;
        }
    }

    // ===================== Helpers de bubbles =====================
    private Sprite GetRandomBubbleAnyGroup()
    {
        if (bubbleGroups == null || bubbleGroups.Length == 0) return null;

        var g = bubbleGroups[Random.Range(0, bubbleGroups.Length)];

        if (g.variants != null && g.variants.Length > 0)
            return g.variants[Random.Range(0, g.variants.Length)];

        return null;
    }

    private Sprite GetBubbleByGroup(string groupName)
    {
        if (bubbleGroups == null || bubbleGroups.Length == 0)
        {
            Debug.LogWarning("[PopupChar] No hay grupos de burbujas configurados.");
            return null;
        }

        foreach (var group in bubbleGroups)
        {
            if (group.name.Equals(groupName, System.StringComparison.OrdinalIgnoreCase))
            {
                if (group.variants != null && group.variants.Length > 0)
                    return group.variants[Random.Range(0, group.variants.Length)];
            }
        }

        Debug.LogWarning("[PopupChar] No se encontró el grupo de burbujas: " + groupName);
        return null;
    }

    public void CambiarEstadoNpcGrosero()
    {
        Debug.Log("[NPC] El NPC está molesto porque la hamburguesa no coincide con la orden.");
    }
}
