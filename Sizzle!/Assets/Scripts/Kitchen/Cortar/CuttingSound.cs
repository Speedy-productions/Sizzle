using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CuttingSound : MonoBehaviour
{
    [Header("Clips de corte")]
    [SerializeField] private AudioClip cutClip;  // sonido breve de corte

    [Header("Configuración")]
    [Range(0f, 1f)] public float baseVolume = 1f;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // UI/ambiente 2D
        src.dopplerLevel = 0f;
    }

    /// <summary>
    /// Llamar cuando se haga click (cuando sube la barra de cortar)
    /// </summary>
    public void PlayCutSound()
    {
        if (cutClip == null) return;

        float finalVol = baseVolume * AudioSettingsManager.SfxVolume;
        src.PlayOneShot(cutClip, finalVol);
    }
}
