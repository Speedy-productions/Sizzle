using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Popup : MonoBehaviour
{
    public Image foodIcon;             
    public Image bubbleIcon;           
    public Image faceIcon;             
    public TextMeshProUGUI messageText;

    private Transform target;

    public void Show(
        Transform targetTransform,
        Sprite food,
        Sprite bubble,
        Sprite face = null,
        string message = "",
        float? fontSize = null
    )
    {
        // ? Seguridad para evitar NullReference
        if (targetTransform == null) return;

        target = targetTransform;

        if (foodIcon != null)
        {
            foodIcon.enabled = (food != null);
            foodIcon.sprite = food;
        }

        if (bubbleIcon != null)
        {
            bubbleIcon.enabled = (bubble != null);
            bubbleIcon.sprite = bubble;
        }

        if (faceIcon != null)
        {
            faceIcon.enabled = (face != null);
            faceIcon.sprite = face;
        }

        if (messageText != null)
        {
            bool hasMessage = !string.IsNullOrEmpty(message);
            messageText.enabled = hasMessage;
            messageText.text = hasMessage ? message : "";
            if (fontSize.HasValue) messageText.fontSize = fontSize.Value;
        }

        transform.position = target.position + Vector3.up * 2f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
