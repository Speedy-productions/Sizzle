using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BurgerCompleteSound : MonoBehaviour
{
    [Header("Clip de sonido (completado)")]
    [SerializeField] private AudioClip completeClip;

    [Range(0f, 1f)]
    [SerializeField] private float baseVolume = 1f;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D
    }

    public void Play()
    {
        if (completeClip == null) return;
        float finalVol = baseVolume * AudioSettingsManager.SfxVolume;
        src.PlayOneShot(completeClip, finalVol);
    }
}
