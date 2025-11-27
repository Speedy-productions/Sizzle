using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MejorasUI : MonoBehaviour
{
    [System.Serializable]
    public class MejoraData
    {
        public string nombre;
        [TextArea] public string descripcion;
        public int nivelActual = 0;
        public int nivelMaximo = 5;
        public int costoPorNivel = 100;
        public Sprite icono;
        public Button botonUI;
    }

    [Header("Lista de Mejoras")]
    public List<MejoraData> mejoras = new List<MejoraData>();

    [Header("UI del Panel de Detalles")]
    public Text tituloTexto;
    public Text descripcionTexto;
    public Text nivelTexto;
    public Image iconoImagen;
    public Text costoTexto;
    public Text dineroDisponibleTexto; // Nuevo: muestra dinero del jugador

    [Header("Paneles")]
    public GameObject panelMejoras;
    public GameObject panelTienda;

    [Header("Sistema de Dinero")]
    public DineroUI dineroUI; // referencia al script de dinero

    private MejoraData mejoraSeleccionada;

    public UpgradeManager upgradeManager;

    void Awake()
    {
        if (dineroUI == null)
            dineroUI = FindObjectOfType<DineroUI>();
    }

    void Start()
    {
        foreach (var m in mejoras)
        {
            if (m.botonUI != null)
                m.botonUI.onClick.AddListener(() => MostrarMejora(m));
        }
        ActualizarDineroUI();
    }

    void Update()
    {
        ActualizarDineroUI(); // refresca cada frame (puedes quitar si no lo necesitas constante)
    }

    void ActualizarDineroUI()
    {
        if (dineroDisponibleTexto != null && dineroUI != null)
            dineroDisponibleTexto.text = "$" + dineroUI.dineroActual.ToString("N0");
    }

    public void MostrarMejora(MejoraData m)
    {
        mejoraSeleccionada = m;

        tituloTexto.text = "Mejora para " + m.nombre;
        descripcionTexto.text = m.descripcion;
        iconoImagen.sprite = m.icono;

        nivelTexto.text = m.nivelActual + "/" + m.nivelMaximo;
        costoTexto.text = "$" + m.costoPorNivel;

        ActualizarDineroUI();
        panelMejoras.SetActive(true);
    }

    public void Comprar()
    {
        if (mejoraSeleccionada == null) return;
        if (dineroUI == null)
        {
            Debug.LogWarning("DineroUI no asignado.");
            return;
        }

        if (mejoraSeleccionada.nivelActual >= mejoraSeleccionada.nivelMaximo)
        {
            Debug.Log("Nivel máximo alcanzado");
            return;
        }

        int costo = mejoraSeleccionada.costoPorNivel;
        if (dineroUI.dineroActual < costo)
        {
            Debug.Log("Dinero insuficiente");
            return;
        }

        if (mejoraSeleccionada.nombre == "Parrilla")
        {
            UpgradeManager.Instance.UpgradeGrill();
        }

        if (mejoraSeleccionada.nombre == "Cortar")
        {
            UpgradeManager.Instance.UpgradeCut();
        }

        dineroUI.QuitarDinero(costo);
        mejoraSeleccionada.nivelActual++;

        nivelTexto.text = mejoraSeleccionada.nivelActual + "/" + mejoraSeleccionada.nivelMaximo;
        costoTexto.text = "$" + costo; 
        ActualizarDineroUI();
    }

    public void Volver()
    {
        panelMejoras.SetActive(false);
        panelTienda.SetActive(true);
    }
}
