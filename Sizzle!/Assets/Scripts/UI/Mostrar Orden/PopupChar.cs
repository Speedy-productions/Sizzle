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
    public Popup popupPrefab;
    public FoodGroup[] foodGroups;
    public BubbleGroup[] bubbleGroups;

    [Header("Configuración")]
    public float switchInterval = 1f;
    public float popupDuration = 5f;

    [HideInInspector]
    public NpcFollowPath npcFollowPath;

    private Popup currentPopup;
    private bool notifyCoroutineRunning = false;

    // Obtiene un sprite aleatorio de un grupo de sprites de comida por su nombre
    private Sprite GetSpriteByName(string name)
    {
        foreach (FoodGroup group in foodGroups)
        {
            if (group.name != name)
                continue;

            if (group.variants.Length > 0)
            {
                int randIndex = Random.Range(0, group.variants.Length); // Escoge un sprite aleatorio del grupo
                return group.variants[randIndex];
            }
        }

        Debug.LogWarning($"No se encontró algún ingrediente con el nombre: '{name}'");
        return null;
    }

    void Start()
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogError("OrderManager no inicializado antes de PopupChar");
            return;
        }
    }
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
        StartCoroutine(AlternateSpritesLimitedTime(popupDuration));

        Destroy(currentPopup.gameObject, popupDuration);
        if (!notifyCoroutineRunning)
            StartCoroutine(NotifyNpcAfterPopup(popupDuration));
    }

    private IEnumerator NotifyNpcAfterPopup()
    {
        yield return new WaitForSeconds(popupDuration); // Wait until popup is destroyed
        npcFollowPath?.OnPopupClosed(); // Tell the NPC it can resume
    }

    private IEnumerator AlternateSpritesLimitedTime(float duration)
    {
        if (currentPopup == null)
        {
            Debug.LogError("Popup no asignado");
            yield break;
        }

        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            Order newOrder = OrderManager.Instance.GenerateHamburgerOrder();
            if (newOrder == null)
            {
                Debug.LogError("No se generó una orden");
                break;
            }
            OrderManager.Instance.SetCurrentOrder(newOrder);

            BubbleGroup bGroup = bubbleGroups[Random.Range(0, bubbleGroups.Length)];
            Sprite nextBubbleSprite = bGroup.variants[Random.Range(0, bGroup.variants.Length)];

            foreach (string ingredientName in newOrder.ingredients)
            {
                if (Time.time >= endTime) break;
                Sprite foodSprite = GetSpriteByName(ingredientName);
                currentPopup.Show(transform, foodSprite, nextBubbleSprite);
                yield return new WaitForSeconds(switchInterval);
            }
        }

        currentPopup = null;
    }

    private IEnumerator NotifyNpcAfterPopup(float duration)
    {
        notifyCoroutineRunning = true;
        yield return new WaitForSeconds(duration);
        npcFollowPath?.OnPopupClosed();
        notifyCoroutineRunning = false;
    }
}
