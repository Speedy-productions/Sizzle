using UnityEngine;
using TMPro;

public class AudioPause : MonoBehaviour
{
    [SerializeField] TMP_Text soundText;
    [SerializeField] TMP_Text musicText;

    private void OnEnable()
    {
        Actualizar();
    }

    // Update is called once per frame
    public void Actualizar()
    {
        soundText.text = $"{AudioSettingsManager.SfxVolume * 100f:0}%";
        musicText.text = $"{AudioSettingsManager.MusicVolume * 100f:0}%";
    }

    public void IncrementarSonidos() { AudioSettingsManager.I.CambiarSonidos(+1); Actualizar(); }
    public void DecrementarSonidos() { AudioSettingsManager.I.CambiarSonidos(-1); Actualizar(); }
    public void IncrementarMusica() { AudioSettingsManager.I.CambiarMusica(+1); Actualizar(); }
    public void DecrementarMusica() { AudioSettingsManager.I.CambiarMusica(-1); Actualizar(); }

}
