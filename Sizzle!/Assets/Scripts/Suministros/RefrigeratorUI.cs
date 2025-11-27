using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class RefrigeratorUI : MonoBehaviour
{
    [System.Serializable]
    public class RefrigeratedItem
    {
        public string nombre;               // Jitomate, Papas, Pan, Carne, Lechuga
        public GameObject prefab;           // Prefab registrado (Photon) o normal
        public int cantidad;                // Cantidad actual
        public Text cantidadTexto;      // Texto UI para mostrar cantidad
        public Button retirarUnoButton;     // Botón para sacar 1
    }

    [Header("Inventario Refrigerador")]
    public List<RefrigeratedItem> items = new();

    [Header("Referencia al jugador para poner en mano")]
    public Interact interactJugador;          // Asignar el componente Interact del jugador local

    [Header("Transform opcional para spawn (si se requiere)")]
    public Transform posicionSpawnTemporal;   // Si null, usa la mano directamente

    [Header("Detección por Crosshair")]
    public Camera camaraJugador;              // Cámara del jugador
    [Range(0.01f, 0.3f)] public float radioDeteccionPantalla = 0.15f;
    public float distanciaMaximaUI = 5f;

    Dictionary<string, RefrigeratedItem> mapa;
    private Button botonApuntado;
    private Button ultimoBotonApuntado; // nuevo para detectar cambio de apuntado

    void Awake()
    {
        mapa = new Dictionary<string, RefrigeratedItem>();
        foreach (var it in items)
        {
            if (string.IsNullOrEmpty(it.nombre)) continue;
            string key = Normalizar(it.nombre);
            if (!mapa.ContainsKey(key))
                mapa.Add(key, it);
        }
    }

    void Start()
    {
        // Asignar listeners a cada botón
        foreach (var it in items)
        {
            if (it.retirarUnoButton != null)
            {
                var captura = it;
                it.retirarUnoButton.onClick.AddListener(() => RetirarUno(captura.nombre));
            }
            ActualizarTexto(it);
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (!camaraJugador) return;

        botonApuntado = DetectarBotonApuntado();

        // Log cuando cambia el botón apuntado
        if (botonApuntado != ultimoBotonApuntado)
        {
            if (botonApuntado != null)
                Debug.Log("[REFRIGERATOR UI] Apuntando a botón: " + botonApuntado.name);
            else
                Debug.Log("[REFRIGERATOR UI] Ya no se apunta a ningún botón.");
            ultimoBotonApuntado = botonApuntado;
        }

        if (Input.GetMouseButtonDown(0) && botonApuntado != null)
        {
            Debug.Log("[REFRIGERATOR UI] Click en botón: " + botonApuntado.name);
            botonApuntado.onClick.Invoke();
        }
    }

    Button DetectarBotonApuntado()
    {
        if (!camaraJugador) return null;

        Button mejorBoton = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new Vector2(0.5f, 0.5f);

        foreach (var item in items)
        {
            if (item.retirarUnoButton == null) continue;

            // Obtener posición del botón en el mundo
            RectTransform rectTransform = item.retirarUnoButton.GetComponent<RectTransform>();
            if (!rectTransform) continue;

            Vector3 posicionMundo = rectTransform.position;

            // Convertir a viewport
            Vector3 vp = camaraJugador.WorldToViewportPoint(posicionMundo);
            
            // Si está detrás de la cámara, ignorar
            if (vp.z <= 0f) continue;

            // Calcular distancia en pantalla desde el centro
            float distanciaPantalla = Vector2.Distance(new Vector2(vp.x, vp.y), centro);
            if (distanciaPantalla > radioDeteccionPantalla) continue;

            // Calcular distancia 3D
            float distancia3D = Vector3.Distance(camaraJugador.transform.position, posicionMundo);
            if (distancia3D > distanciaMaximaUI) continue;

            // Calcular score (prioriza cercanía en pantalla y distancia 3D)
            float score = distanciaPantalla * 10f + distancia3D;
            if (score < mejorScore)
            {
                mejorScore = score;
                mejorBoton = item.retirarUnoButton;
            }
        }

        return mejorBoton;
    }

    string Normalizar(string n)
    {
        if (string.IsNullOrEmpty(n)) return n;
        return n.Trim().ToLower();
    }

    void ActualizarTexto(RefrigeratedItem it)
    {
        if (it.cantidadTexto != null)
            it.cantidadTexto.text = it.cantidad.ToString();
    }

    public void AgregarCompra(Dictionary<string, int> compra)
    {
        foreach (var kv in compra)
            AgregarItem(kv.Key, kv.Value);

        RefrescarTodo();
    }

    public void AgregarItem(string nombre, int cant)
    {
        string key = Normalizar(nombre);
        if (mapa.TryGetValue(key, out var it))
        {
            it.cantidad += cant;
            ActualizarTexto(it);
        }
        else
        {
            Debug.LogWarning("[REFRIGERATOR] Item no encontrado en la lista: " + nombre);
        }
    }

    void RefrescarTodo()
    {
        foreach (var it in items)
            ActualizarTexto(it);
    }

    public void RetirarUno(string nombre)
    {
        string key = Normalizar(nombre);
        if (!mapa.TryGetValue(key, out var it))
        {
            Debug.LogWarning("[REFRIGERATOR] No existe item: " + nombre);
            return;
        }
        if (it.cantidad <= 0)
        {
            Debug.Log("[REFRIGERATOR] Cantidad 0 de " + nombre);
            return;
        }
        if (!interactJugador)
        {
            Debug.LogWarning("[REFRIGERATOR] Falta referencia a InteractJugador.");
            return;
        }
        if (!it.prefab)
        {
            Debug.LogWarning("[REFRIGERADOR] Prefab nulo para " + nombre);
            return;
        }

        // Solo descontar y delegar a Interact
        it.cantidad--;
        ActualizarTexto(it);

        // Interact se encarga de instanciar y poner en la mano
        interactJugador.RecibirDesdeRefrigerador(it.prefab);

        Debug.Log("[REFRIGERATOR UI] Retirando 1 de: " + nombre + " | Restantes: " + it.cantidad);
    }
}
