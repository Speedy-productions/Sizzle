using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager I;

    [Header("Clip de música principal")]
    public AudioClip mainMusicClip;

    [Header("Configuración")]
    [Range(0f, 1f)] public float baseVolume = 0.8f;
    public bool playOnStart = true;
    public bool loop = true;

    private AudioSource src;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;

        // Aplicar volumen inicial
        ApplyFinalVolume();

        // Escuchar cambios del menú
        AudioSettingsManager.OnMusicVolumeChanged += OnGlobalVolumeChanged;
    }

    void Start()
    {
        if (playOnStart && mainMusicClip != null)
            PlayMusic(mainMusicClip);
    }

    void OnDestroy()
    {
        AudioSettingsManager.OnMusicVolumeChanged -= OnGlobalVolumeChanged;
    }

    // ▶ Reproducir música
    public void PlayMusic(AudioClip clip)
    {
        if (!clip)
        {
            Debug.LogWarning("[MusicManager] Intentaste reproducir música null.");
            return;
        }

        src.clip = clip;
        src.loop = loop;
        src.Play();
    }

    // ⏹ Detener música
    public void StopMusic()
    {
        if (src.isPlaying) src.Stop();
    }

    // ===== VOLUMEN =====

    void OnGlobalVolumeChanged(float globalVol)
    {
        ApplyFinalVolume();
    }

    public void ApplyFinalVolume()
    {
        float musicVol = AudioSettingsManager.MusicVolume;
        src.volume = baseVolume * musicVol;
    }
}
