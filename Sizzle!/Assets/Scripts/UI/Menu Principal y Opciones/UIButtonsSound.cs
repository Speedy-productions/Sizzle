// UIButtonAudioRelay.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAudioRelay : MonoBehaviour, IPointerClickHandler
{
    [Header("Overrides (opcional)")]
    public AudioClip clickOverride;

    public void OnPointerClick(PointerEventData e)
        => UIAudioManager.I?.PlayClick(clickOverride);
}
