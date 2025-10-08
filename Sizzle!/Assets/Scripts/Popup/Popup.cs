using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    public Image foodIcon;
    public Image bubbleIcon;

    private Transform target;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera != null)
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
    }

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
