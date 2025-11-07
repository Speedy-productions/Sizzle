using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NpcAudio : MonoBehaviour
{
    [Header("Clips de Voz")]
    [SerializeField] private AudioClip talkClip;
    [SerializeField] private AudioClip happyClip;
    [SerializeField] private AudioClip angryClip;

    [Header("Configuración")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.9f, 1.1f)] public float randomPitchRange = 1f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.pitch = 1f;
    }

    void OnEnable() => AudioSettingsManager.OnSfxVolumeChanged += ApplyVolumeGlobal;
    void OnDisable() => AudioSettingsManager.OnSfxVolumeChanged -= ApplyVolumeGlobal;

    void PlayClip(AudioClip clip)
    {
        if (clip == null || source == null) return;

        source.pitch = Random.Range(1f / randomPitchRange, randomPitchRange);
        source.volume = volume * AudioSettingsManager.SfxVolume;
        source.clip = clip;
        source.Play();
    }

    void ApplyVolumeGlobal(float vol)
    {
        if (source.isPlaying)
            source.volume = volume * vol;
    }

    public void PlayTalk() => PlayClip(talkClip);
    public void PlayHappy() => PlayClip(happyClip);
    public void PlayAngry() => PlayClip(angryClip);
}
