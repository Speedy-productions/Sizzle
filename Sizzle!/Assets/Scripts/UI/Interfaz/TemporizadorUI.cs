using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TemporizadorUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoTemporizador;

    [Header("Game Over")]
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private AudioSource sonidoGameOver;

    [Header("Referencias")]
    [SerializeField] private PlayerCam playerCam;

    private bool corriendo = true;

    void Awake()
    {
        if (textoTemporizador == null)
        {
            textoTemporizador = GetComponent<TMP_Text>();
            if (textoTemporizador == null)
                textoTemporizador = GetComponentInChildren<TMP_Text>(true);
        }
    }

    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        if (playerCam == null)
            playerCam = FindFirstObjectByType<PlayerCam>();

        Pintar(); // inicial
    }

    void Update()
    {
        if (!corriendo) return;

        float tiempo = 0f;

        // ============================
        //   SINGLEPLAYER O MULTI
        // ============================
        if (GameManagerTiempo.Instance != null)
        {
            tiempo = GameManagerTiempo.Instance.tiempoRestante;

            if (!GameManagerTiempo.Instance.corriendo)
            {
                corriendo = false;
                MostrarGameOver();
            }
        }
        else
        {
            // Si NO hay GameManagerTiempo → fallback seguro
            tiempo = 0f;
        }

        if (tiempo <= 0f)
        {
            tiempo = 0f;
            corriendo = false;
            MostrarGameOver();
        }

        Pintar();
    }

    // =============================
    //   ACTUALIZA EL TEXTO EN UI
    // =============================
    void Pintar()
    {
        if (!textoTemporizador) return;
        if (GameManagerTiempo.Instance == null) return;

        float tiempoRestante = GameManagerTiempo.Instance.tiempoRestante;

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);

        textoTemporizador.text = $"{minutos:00}:{segundos:00}";
    }

    // =============================
    //   GAME OVER
    // =============================
    void MostrarGameOver()
    {
        if (panelGameOver != null)
        {
            Canvas canvas = panelGameOver.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Panel GameOver debe ser hijo de un Canvas");
                return;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            panelGameOver.SetActive(true);
        }

        if (playerCam != null)
            playerCam.DesbloquearCursor();

        if (sonidoGameOver != null)
            sonidoGameOver.Play();

        Time.timeScale = 0f;
    }

    // =============================
    //   REINICIAR JUEGO
    // =============================
    public void ReiniciarJuego()
    {
        if (panelGameOver != null)
            panelGameOver.SetActive(false);

        if (playerCam != null)
            playerCam.BloquearCursor();

        Time.timeScale = 1f;

        if (GameManagerTiempo.Instance != null)
        {
            GameManagerTiempo.Instance.tiempoRestante = GameManagerTiempo.Instance.tiempoInicial;
            GameManagerTiempo.Instance.corriendo = true;
        }

        corriendo = true;
        Pintar();
    }

    public void Pausar() => corriendo = false;
    public void Reanudar() => corriendo = true;
}
