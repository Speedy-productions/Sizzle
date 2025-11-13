using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TemporizadorUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoTemporizador; 

    [Header("Tiempo")]
    public float tiempoInicial = 180f;

    [Header("Game Over")]
    public GameObject panelGameOver;
    public AudioSource sonidoGameOver;

    [Header("Referencias")]
    public PlayerCam playerCam;

    float tiempoRestante;
    bool corriendo = true;

    void Awake()
    {
        // NO accedas aquí a TMP, solo prepara variables simples
    }


    void Start()
    {
        // → MOVER AQUÍ TODA LA LÓGICA UI
        if (textoTemporizador == null)
            textoTemporizador = GetComponent<TMP_Text>();

        if (playerCam == null)
            playerCam = FindFirstObjectByType<PlayerCam>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        tiempoRestante = Mathf.Max(0, tiempoInicial);
        Pintar();
    }

    void Update()
    {
        if (!UIBoot.Ready) return;
        if (!corriendo) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            corriendo = false;
            MostrarGameOver();
        }

        Pintar();
    }

    void Pintar()
    {
        if (!textoTemporizador) return;

        int min = Mathf.FloorToInt(tiempoRestante / 60);
        int seg = Mathf.FloorToInt(tiempoRestante % 60);
        textoTemporizador.text = $"{min:00}:{seg:00}";
    }

    void MostrarGameOver()
    {
        if (panelGameOver)
            panelGameOver.SetActive(true);

        if (playerCam)
            playerCam.DesbloquearCursor();

        if (sonidoGameOver)
            sonidoGameOver.Play();

        Time.timeScale = 0;
    }

    public void Reiniciar(float? nuevoTiempo = null)
    {
        tiempoRestante = Mathf.Max(0, nuevoTiempo ?? tiempoInicial);
        corriendo = true;
        Time.timeScale = 1;
        Pintar();
    }

    public void ReiniciarJuego()
    {
        if (panelGameOver)
            panelGameOver.SetActive(false);

        if (playerCam)
            playerCam.BloquearCursor();

        Reiniciar();
    }

    public void Pausar() => corriendo = false;
    public void Reanudar() => corriendo = true;
}
