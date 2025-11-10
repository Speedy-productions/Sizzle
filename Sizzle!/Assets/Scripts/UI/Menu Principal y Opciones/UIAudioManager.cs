using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager I;

    [Header("Clips por defecto")]
    public AudioClip clickClipDefault;

    [Header("Volumen base (local)")]
    [Range(0f, 1f)] public float volume = 0.9f;

    private AudioSource src;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject); // ?? Importante: persiste entre escenas

        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // UI = 2D
        src.dopplerLevel = 0f;
        src.pitch = 1f;

        // ?? Aquí aplicamos el volumen guardado (si existe AudioSettingsManager)
        if (AudioSettingsManager.I != null)
            SetVolume(AudioSettingsManager.SfxVolume);

        // ?? También nos suscribimos al evento para reaccionar cuando cambie el volumen en opciones
        AudioSettingsManager.OnSfxVolumeChanged += SetVolume;
    }

    void OnDestroy()
    {
        AudioSettingsManager.OnSfxVolumeChanged -= SetVolume;
    }

    public void PlayClick(AudioClip overrideClip = null)
    {
        var clip = overrideClip ? overrideClip : clickClipDefault;
        if (clip)
        {
            float finalVol = volume * AudioSettingsManager.SfxVolume; // combina el volumen local y global
            src.PlayOneShot(clip, finalVol);
        }
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        // opcionalmente podrías guardar tu propio volumen aquí si lo quisieras independiente
    }
}
