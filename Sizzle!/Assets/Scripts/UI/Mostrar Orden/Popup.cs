using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Popup : MonoBehaviour
{
    public Image foodIcon;            // puede ser null si el popup es sólo de cara
    public Image bubbleIcon;          // puede ser null si el popup es sólo de cara
    public Image faceIcon;            // puede ser null si el popup es sólo de ingredientes
    public TextMeshProUGUI messageText; // NUEVO: texto en el popup (opcional)

    private Transform target;

    /// <summary>
    /// Muestra el popup.
    /// </summary>
    /// <param name="targetTransform">Transform a seguir</param>
    /// <param name="food">Sprite de comida (o null)</param>
    /// <param name="bubble">Sprite de burbuja (o null)</param>
    /// <param name="face">Sprite de cara (o null)</param>
    /// <param name="message">Texto a mostrar (opcional)</param>
    /// <param name="fontSize">Tamaño de fuente (opcional). Si es null, se mantiene el del prefab.</param>
    public void Show(Transform targetTransform, Sprite food, Sprite bubble, Sprite face = null, string message = "", float? fontSize = null)
    {
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
