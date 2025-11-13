using System;
using UnityEngine;
using TMPro;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager I;

    [Header("Referencias UI")]
    [SerializeField] private TMP_Text sonidosText;
    [SerializeField] private TMP_Text musicaText;

    [Header("Valores actuales (porcentaje entero 0–100)")]
    private int volumenSonidosPct = 100;
    private int volumenMusicaPct = 100;

    public static event Action<float> OnSfxVolumeChanged;
    public static event Action<float> OnMusicVolumeChanged;

    private const string PREF_SONIDOS = "VolumenSonidos";
    private const string PREF_MUSICA = "VolumenMusica";

    public static float SfxVolume => I ? I.volumenSonidosPct / 100f : 1f;
    public static float MusicVolume => I ? I.volumenMusicaPct / 100f : 1f;

    private void Awake()
{
    if (I != null && I != this) { Destroy(gameObject); return; }
    I = this;
    DontDestroyOnLoad(gameObject);

    // Cargar desde PlayerPrefs, default 100
    volumenSonidosPct = PlayerPrefs.GetInt(PREF_SONIDOS, 100);
    volumenMusicaPct = PlayerPrefs.GetInt(PREF_MUSICA, 100);

    volumenSonidosPct = Mathf.RoundToInt(volumenSonidosPct / 10f) * 10;
    volumenMusicaPct = Mathf.RoundToInt(volumenMusicaPct / 10f) * 10;

    ActualizarTextos();
    AplicarVolumenGlobal();

    // ?? NUEVO: notificar volumen actual a todos los listeners
    OnSfxVolumeChanged?.Invoke(SfxVolume);
    OnMusicVolumeChanged?.Invoke(MusicVolume);
}


    // ===== Cambiar volumen en pasos de 10 =====
    public void CambiarSonidos(float delta)
    {
        int sign = delta >= 0 ? 1 : -1;

        volumenSonidosPct += sign * 10;
        volumenSonidosPct = Mathf.Clamp(volumenSonidosPct, 0, 100);

        PlayerPrefs.SetInt(PREF_SONIDOS, volumenSonidosPct);
        PlayerPrefs.Save();

        ActualizarTextos();
        AplicarVolumenGlobal();
        OnSfxVolumeChanged?.Invoke(SfxVolume);
    }

    public void CambiarMusica(float delta)
    {
        int sign = delta >= 0 ? 1 : -1;

        volumenMusicaPct += sign * 10;
        volumenMusicaPct = Mathf.Clamp(volumenMusicaPct, 0, 100);

        PlayerPrefs.SetInt(PREF_MUSICA, volumenMusicaPct);
        PlayerPrefs.Save();

        ActualizarTextos();
        OnMusicVolumeChanged?.Invoke(MusicVolume);
    }

    // ===== UI =====
    private void ActualizarTextos()
    {
        if (sonidosText) sonidosText.text = $"{volumenSonidosPct}%";
        if (musicaText) musicaText.text = $"{volumenMusicaPct}%";
    }

    private void AplicarVolumenGlobal()
{
    if (UIAudioManager.I)
        UIAudioManager.I.SetVolume(SfxVolume);

    // ?? NUEVO: aplicar volumen también al resto del juego
    OnSfxVolumeChanged?.Invoke(SfxVolume);
}

}
