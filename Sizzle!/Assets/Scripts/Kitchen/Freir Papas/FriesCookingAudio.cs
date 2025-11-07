using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FriesCookingAudio : MonoBehaviour
{
    [Header("Clips de Sonido")]
    [SerializeField] private AudioClip fryStartClip;
    [SerializeField] private AudioClip fryLoopClip;

    [Header("Volumen")]
    [Range(0f, 1f)][SerializeField] private float startVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float loopVolume = 0.8f;

    private AudioSource mainSource;
    private AudioSource loopSource;
    private FriesCookingState fries;
    private bool wasInFryer = false;
    private Coroutine startRoutine;

    void Awake()
    {
        mainSource = gameObject.AddComponent<AudioSource>();
        loopSource = gameObject.AddComponent<AudioSource>();

        SetupSource(mainSource, false);
        SetupSource(loopSource, true);

        fries = GetComponent<FriesCookingState>();
    }

    void OnEnable() => AudioSettingsManager.OnSfxVolumeChanged += ApplyVolumeGlobal;
    void OnDisable() => AudioSettingsManager.OnSfxVolumeChanged -= ApplyVolumeGlobal;

    void Update()
    {
        if (!fries) return;

        if (fries.isInFryer && !wasInFryer)
        {
            StartFrying();
            wasInFryer = true;
        }

        if (!fries.isInFryer && wasInFryer)
        {
            StopAllAudio();
            wasInFryer = false;
        }
    }

    void SetupSource(AudioSource src, bool loop)
    {
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
    }

    void StartFrying()
    {
        StopAllAudio();

        if (fryStartClip)
        {
            mainSource.clip = fryStartClip;
            mainSource.volume = startVolume * AudioSettingsManager.SfxVolume;
            mainSource.Play();

            if (startRoutine != null) StopCoroutine(startRoutine);
            startRoutine = StartCoroutine(StartLoopAfter(mainSource.clip.length));
        }
        else StartLoopImmediately();
    }

    IEnumerator StartLoopAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartLoopImmediately();
    }

    void StartLoopImmediately()
    {
        if (fryLoopClip)
        {
            loopSource.clip = fryLoopClip;
            loopSource.volume = loopVolume * AudioSettingsManager.SfxVolume;
            loopSource.Play();
        }
    }

    void StopAllAudio()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
        mainSource.Stop();
        loopSource.Stop();
    }

    void ApplyVolumeGlobal(float vol)
    {
        if (mainSource.isPlaying) mainSource.volume = startVolume * vol;
        if (loopSource.isPlaying) loopSource.volume = loopVolume * vol;
    }

    public void StopImmediately()
    {
        StopAllAudio();
        wasInFryer = false;
    }
}
