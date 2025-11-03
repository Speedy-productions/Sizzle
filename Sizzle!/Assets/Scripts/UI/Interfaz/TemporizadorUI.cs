using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TemporizadorUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoTemporizador; // arrástralo o se auto-busca

    [Header("Tiempo")]
    [SerializeField] private float tiempoInicial = 180f; // 3 min en segundos

    private float tiempoRestante;
    private bool corriendo = true;

    void Awake()
    {
        // Autodescubrimiento si no está asignado
        if (textoTemporizador == null)
        {
            // primero en este GO, si no, en hijos
            textoTemporizador = GetComponent<TMP_Text>();
            if (textoTemporizador == null)
                textoTemporizador = GetComponentInChildren<TMP_Text>(true);
        }
    }

    void Start()
    {
        tiempoRestante = Mathf.Max(0f, tiempoInicial);
        Pintar();
    }

    void Update()
    {
        if (!corriendo) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            corriendo = false;
            // TODO: aquí tu lógica al terminar (GameOver, etc.)
        }

        Pintar();
    }

    void Pintar()
    {
        if (!textoTemporizador) return; // evita NullReference si sigue sin asignar
        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        textoTemporizador.text = $"{minutos:00}:{segundos:00}";
    }

    // API pública
    public void Reiniciar(float? nuevoTiempo = null)
    {
        tiempoRestante = Mathf.Max(0f, nuevoTiempo ?? tiempoInicial);
        corriendo = true;
        Pintar();
    }

    public void Pausar() => corriendo = false;
    public void Reanudar() => corriendo = true;
}
