using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    public Image foodIcon;
    public Image bubbleIcon;

    private Transform target;

    public void Show(Transform targetTransform, Sprite food, Sprite bubble)
    {
        if (foodIcon == null || bubbleIcon == null)
        {
            Debug.LogError("Images no asignadas");
            return;
        }

        target = targetTransform;
        foodIcon.sprite = food;
        bubbleIcon.sprite = bubble;

        transform.position = target.position + Vector3.up * 2f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
