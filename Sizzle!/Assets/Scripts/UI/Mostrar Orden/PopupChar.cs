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

public class PopupChar : MonoBehaviour
{
    [Header("Prefabs y Sprites")]
    public Popup popupPrefab;           // Prefab para popups
    public FoodGroup[] foodGroups;      // sprites por ingrediente
    public BubbleGroup[] bubbleGroups;  // sprites de burbujas

    [Header("Caras del NPC (faces-states)")]
    public Sprite smileyFace;           // "smileyface"
    public Sprite angryFace;            // "angryface"

    [Header("Configuración")]
    public float switchInterval = 1f;
    public float popupDuration = 5f;

    [HideInInspector] public NpcFollowPath npcFollowPath;

    private Popup currentPopup; // popup de ingredientes/burbujas (pedido)
    private Popup facePopup;    // popup independiente para la cara + mensaje
    private bool notifyCoroutineRunning = false;

    private void Start()
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogError("OrderManager no inicializado antes de PopupChar");
        }
    }

    // Genera y asigna pedido al NPC
    private Order GenerateNpcOrder()
    {
        Order newOrder = OrderManager.Instance.GenerateHamburgerOrder();
        if (newOrder != null && npcFollowPath != null)
            npcFollowPath.AssignNpcOrder(newOrder);
        return newOrder;
    }

    // ===================== Pedido (ingredientes + bubble aleatorio) =====================
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

        currentPopup = Instantiate(popupPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);

        // Generar y fijar el pedido que este NPC quiere
        GenerateNpcOrder();

        StartCoroutine(AlternateSpritesLimitedTime(popupDuration));

        Destroy(currentPopup.gameObject, popupDuration);
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

    private IEnumerator AlternateSpritesLimitedTime(float duration)
    {
        if (currentPopup == null) yield break;

        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            Order currentNpcOrder = npcFollowPath != null ? npcFollowPath.GetAssignedOrder() : null;
            if (currentNpcOrder == null)
            {
                Debug.LogError("No se generó una orden");
                break;
            }

            OrderManager.Instance.SetCurrentOrder(currentNpcOrder);

            // bubble aleatorio para mostrar el pedido
            Sprite nextBubbleSprite = GetRandomBubbleAnyGroup();

            foreach (string ingredientName in currentNpcOrder.ingredients)
            {
                if (Time.time >= endTime) break;

                Sprite foodSprite = GetSpriteByName(ingredientName);
                // Nota: no mostramos texto aquí
                currentPopup.Show(transform, foodSprite, nextBubbleSprite, null);
                yield return new WaitForSeconds(switchInterval);
            }
        }

        currentPopup = null;
    }

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

        Debug.LogWarning($"No se encontró algún ingrediente con el nombre: '{name}'");
        return null;
    }

    // ===================== Resultado (cara + bubble + texto) =====================
    public void MostrarCaraFeliz(string mensaje = "¡Bien hecho!")
    {
        Sprite bubble = GetBubbleByGroup("Calmado"); // Usa los bubbles de "Calmado"
        MostrarCara(smileyFace, bubble, mensaje, 32f);     // tamaño por defecto 32
    }

    public void MostrarCaraMolesta(string mensaje = "¿Qué es esta $#*!?")
    {
        Sprite bubble = GetBubbleByGroup("Grosero"); // Usa los bubbles de "Grosero"
        MostrarCara(angryFace, bubble, mensaje, 26f);      // ?? tamaño 26 para insulto
    }

    private void MostrarCara(Sprite faceSprite, Sprite bubbleSprite, string mensaje, float fontSize)
    {
        if (popupPrefab == null || faceSprite == null) return;

        // Si ya había una cara mostrándose, reemplazarla
        if (facePopup != null)
            Destroy(facePopup.gameObject);

        facePopup = Instantiate(popupPrefab, transform.position + Vector3.up * 2.5f, Quaternion.identity);

        // Sólo cara + bubble + texto (sin food)
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
                {
                    return group.variants[Random.Range(0, group.variants.Length)];
                }
            }
        }

        Debug.LogWarning($"[PopupChar] No se encontró un grupo de burbujas con el nombre '{groupName}'.");
        return null;
    }

    // Mantengo este método porque otros scripts pueden llamarlo (logs)
    public void CambiarEstadoNpcGrosero()
    {
        Debug.Log("[NPC] El NPC está molesto porque la hamburguesa no coincide con la orden.");
    }
}
