using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MeatCookingAudio : MonoBehaviour
{
    [Header("Clips de sonido")]
    [SerializeField] private AudioClip fryingLoopClip;
    [SerializeField] private AudioClip flipSizzleClip;

    [Header("Configuración")]
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float maxHearingDistance = 12f;
    [SerializeField] private float minHearingDistance = 1.5f;
    [SerializeField, Range(0f, 2f)] private float flipSizzleVolume = 1f;

    private AudioSource mainSource;
    private AudioSource secondarySource;
    private MeatCookingState meat;
    private Transform player;
    private float baseVolume = 1f;
    private bool usingMain = true;

    void Awake()
    {
        var sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            mainSource = gameObject.AddComponent<AudioSource>();
            secondarySource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            mainSource = sources[0];
            secondarySource = sources[1];
        }

        ConfigureSource(mainSource);
        ConfigureSource(secondarySource);
    }

    void OnEnable() => AudioSettingsManager.OnSfxVolumeChanged += ApplyVolumeGlobal;
    void OnDisable() => AudioSettingsManager.OnSfxVolumeChanged -= ApplyVolumeGlobal;

    void Start()
    {
        meat = GetComponent<MeatCookingState>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (fryingLoopClip)
        {
            mainSource.clip = fryingLoopClip;
            secondarySource.clip = fryingLoopClip;
        }
    }

    void Update()
    {
        if (!meat) return;

        if (!meat.isOnPan)
        {
            mainSource.volume = Mathf.Lerp(mainSource.volume, 0f, Time.deltaTime * fadeSpeed);
            secondarySource.volume = Mathf.Lerp(secondarySource.volume, 0f, Time.deltaTime * fadeSpeed);

            if (mainSource.volume < 0.01f && secondarySource.volume < 0.01f)
            {
                mainSource.Stop();
                secondarySource.Stop();
            }
            return;
        }

        float targetVol = GetDistanceBasedVolume() * AudioSettingsManager.SfxVolume;

        if (!mainSource.isPlaying && !secondarySource.isPlaying && fryingLoopClip)
            mainSource.Play();

        if (usingMain)
            mainSource.volume = Mathf.Lerp(mainSource.volume, targetVol, Time.deltaTime * fadeSpeed);
        else
            secondarySource.volume = Mathf.Lerp(secondarySource.volume, targetVol, Time.deltaTime * fadeSpeed);
    }

    void ConfigureSource(AudioSource src)
    {
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.volume = 0f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 1f;
        src.maxDistance = maxHearingDistance;
    }

    float GetDistanceBasedVolume()
    {
        if (!player) return baseVolume;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > maxHearingDistance) return 0f;
        if (dist < minHearingDistance) return baseVolume;
        float t = Mathf.InverseLerp(maxHearingDistance, minHearingDistance, dist);
        return Mathf.Lerp(0f, baseVolume, t);
    }

    public void RestartCookingSound()
    {
        if (flipSizzleClip)
            AudioSource.PlayClipAtPoint(flipSizzleClip, transform.position,
                flipSizzleVolume * AudioSettingsManager.SfxVolume);

        if (!fryingLoopClip) return;
        StartCoroutine(CrossfadeRestart());
    }

    private IEnumerator CrossfadeRestart()
    {
        AudioSource active = usingMain ? mainSource : secondarySource;
        AudioSource next = usingMain ? secondarySource : mainSource;

        next.clip = fryingLoopClip;
        next.time = 0f;
        next.Play();
        next.volume = 0f;

        float fadeTime = 0.25f;
        float targetVol = GetDistanceBasedVolume() * AudioSettingsManager.SfxVolume;

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float k = t / fadeTime;
            active.volume = Mathf.Lerp(targetVol, 0f, k);
            next.volume = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }

        active.Stop();
        next.volume = targetVol;
        usingMain = !usingMain;
    }

    void ApplyVolumeGlobal(float vol)
    {
        if (mainSource.isPlaying) mainSource.volume *= vol;
        if (secondarySource.isPlaying) secondarySource.volume *= vol;
    }

    public void StopImmediately()
    {
        mainSource.Stop();
        secondarySource.Stop();
    }
}
