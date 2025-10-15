using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TemporizadorUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public TMP_Text textoTemporizador;
    public float tiempoInicial = 180f; // 3 minutos en segundos
    private float tiempoRestante;
    private bool corriendo = true;

    void Start()
    {
        tiempoRestante = tiempoInicial;
    }

    void Update()
    {
        if (corriendo)
        {
            tiempoRestante -= Time.deltaTime;

            if (tiempoRestante > 0)
            {
                slider.value = tiempoRestante / tiempoInicial;
            }

            if (tiempoRestante < 0)
            {
                tiempoRestante = 0;
                corriendo = false;
                // Aquí puedes poner lo que pasa cuando termina el tiempo
                // Por ejemplo: GameOver(), siguiente nivel, etc.
            }

            int minutos = Mathf.FloorToInt(tiempoRestante / 60);
            int segundos = Mathf.FloorToInt(tiempoRestante % 60);
            textoTemporizador.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }
    }

    public void Reiniciar()
    {
        tiempoRestante = tiempoInicial;
        corriendo = true;
    }

    public void Pausar()
    {
        corriendo = false;
    }

    public void Reanudar()
    {
        corriendo = true;
    }
}
