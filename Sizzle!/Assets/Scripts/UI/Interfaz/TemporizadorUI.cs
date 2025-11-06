using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TemporizadorUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoTemporizador; // arrástralo o se auto-busca

    [Header("Tiempo")]
    [SerializeField] private float tiempoInicial = 180f; // 3 min en segundos

    [Header("Game Over")]
    [SerializeField] private GameObject panelGameOver; // Panel que se mostrará al terminar
    [SerializeField] private AudioSource sonidoGameOver; // Opcional: sonido al perder

    [Header("Referencias")]
    [SerializeField] private PlayerCam playerCam;

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
        // Asegurarse que existe un EventSystem
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("No EventSystem found - creating one");
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        if (playerCam == null)
        {
            playerCam = FindFirstObjectByType<PlayerCam>();
        }
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
            MostrarGameOver();
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

    void MostrarGameOver()
    {
        if (panelGameOver != null)
        {
            // Asegurar que el panel tiene los componentes necesarios
            Canvas canvas = panelGameOver.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Panel GameOver needs to be child of a Canvas!");
                return;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogWarning("Adding GraphicRaycaster to Canvas");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            panelGameOver.SetActive(true);
            Debug.Log("Game Over panel activated");
        }
        
        if (playerCam != null)
        {
            playerCam.DesbloquearCursor();
            Debug.Log("Cursor desbloqueado");
        }
        
        if (sonidoGameOver != null)
        {
            sonidoGameOver.Play();
        }
        
        Time.timeScale = 0f;
    }

    // API pública
    public void Reiniciar(float? nuevoTiempo = null)
    {
        tiempoRestante = Mathf.Max(0f, nuevoTiempo ?? tiempoInicial);
        corriendo = true;
        Pintar();
    }

    public void ReiniciarJuego()
    {
        Debug.Log("Intento de reiniciar juego");
        
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }

        if (playerCam != null)
        {
            playerCam.BloquearCursor();
        }

        Time.timeScale = 1f;
        Reiniciar();
        Debug.Log("Juego reiniciado");
    }

    public void Pausar() => corriendo = false;
    public void Reanudar() => corriendo = true;
}
