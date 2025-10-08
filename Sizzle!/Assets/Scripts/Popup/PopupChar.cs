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

    private Popup currentPopup;

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
        ShowPopup();
    }
    public void ShowPopup()
    {
        if (popupPrefab == null)
        {
            Debug.LogError("Popup prefab no asignado");
            return;
        }

        currentPopup = Instantiate(popupPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
        StartCoroutine(AlternateSprites());

        Destroy(currentPopup.gameObject, popupDuration);
    }

    IEnumerator AlternateSprites()
    {
        if (currentPopup == null)
        {
            Debug.LogError("Popup no asignado");
            yield break;
        }

        while (currentPopup != null)
        {
            Order newOrder = OrderManager.Instance.GenerateHamburgerOrder();
            if (newOrder == null)
            {
                Debug.LogError("No se generó una orden");
                yield break;
            }

            BubbleGroup bGroup = bubbleGroups[Random.Range(0, bubbleGroups.Length)];
            Sprite nextBubbleSprite = bGroup.variants[Random.Range(0, bGroup.variants.Length)];

            foreach (string ingredientName in newOrder.ingredients)
            {
                Sprite foodSprite = GetSpriteByName(ingredientName);
                currentPopup.Show(transform, foodSprite, nextBubbleSprite);
                yield return new WaitForSeconds(switchInterval); 
            }
        }
    }
}
