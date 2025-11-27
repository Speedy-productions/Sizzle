using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SuministrosUI : MonoBehaviour
{
    [System.Serializable]
    public class ItemSuministro
    {
        public string nombre;
        public int precio;
        public Button botonUI;
        public TMP_Text cantidadTexto; // Nuevo: muestra cantidad
    }

    [Header("Lista de Suministros")]
    public List<ItemSuministro> suministros = new List<ItemSuministro>();

    [Header("UI")]
    public Text carritoTexto;
    public Text totalTexto;
    public Text dineroDisponibleTexto; 
    public GameObject panelSuministros;
    public GameObject panelTienda;

    [Header("Sistema de Dinero")]
    public DineroUI dineroUI;

    [Header("Refrigerador destino")]
    public RefrigeratorUI refrigeratorUI;   // ASIGNAR en Inspector

    private Dictionary<string, int> carrito = new Dictionary<string, int>();
    private int total = 0;

    void Start()
    {
        foreach (var item in suministros)
        {
            if (item.botonUI != null)
                item.botonUI.onClick.AddListener(() => AgregarItem(item));
            if (item.cantidadTexto != null)
                item.cantidadTexto.text = "0"; // inicia en 0
        }

        ActualizarCarritoUI();
    }

    void Update()
    {
        // Actualiza el dinero disponible cada frame
        if (dineroDisponibleTexto != null && dineroUI != null)
        {
            dineroDisponibleTexto.text = "$" + dineroUI.dineroActual.ToString("N0");
        }
    }

    public void AgregarItem(ItemSuministro item)
    {
        if (!carrito.ContainsKey(item.nombre))
            carrito[item.nombre] = 0;

        carrito[item.nombre]++;
        total += item.precio;

        ActualizarCantidadItem(item.nombre);
        ActualizarCarritoUI();
    }

    public void VaciarCarrito()
    {
        carrito.Clear();
        total = 0;
        ReiniciarCantidadesVisuales();
        ActualizarCarritoUI();
    }

    public void ConfirmarCompra()
    {
        if (dineroUI.dineroActual < total)
        {
            Debug.Log("Dinero insuficiente!");
            return;
        }

        // Antes de limpiar carrito → pasar al refrigerador
        if (refrigeratorUI != null)
            refrigeratorUI.AgregarCompra(carrito);

        dineroUI.QuitarDinero(total);
        Debug.Log("Compra realizada por $" + total);

        carrito.Clear();
        total = 0;

        ReiniciarCantidadesVisuales(); // reinicia visibles
        ActualizarCarritoUI();
    }


    void ActualizarCarritoUI()
    {
        carritoTexto.text = "";

        foreach (var kv in carrito)
            carritoTexto.text += $"{kv.Key}: {kv.Value}\n";

        totalTexto.text = "$" + total;

        if (dineroDisponibleTexto != null && dineroUI != null)
            dineroDisponibleTexto.text = "$" + dineroUI.dineroActual.ToString("N0");
    }

    void ActualizarCantidadItem(string nombre)
    {
        var item = suministros.Find(i => i.nombre == nombre);
        if (item != null && item.cantidadTexto != null)
        {
            carrito.TryGetValue(nombre, out int cant);
            item.cantidadTexto.text = cant.ToString();
        }
    }

    void ReiniciarCantidadesVisuales()
    {
        foreach (var item in suministros)
        {
            if (item.cantidadTexto != null)
                item.cantidadTexto.text = "0";
        }
    }
}
