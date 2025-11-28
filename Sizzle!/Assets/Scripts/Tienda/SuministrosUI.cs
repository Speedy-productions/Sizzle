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
        public TMP_Text cantidadTexto;
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

    [Header("Almacen destino")]
    public Almacen almacen;

    private Dictionary<string, int> carrito = new Dictionary<string, int>();
    private int total = 0;

    void Start()
    {
        foreach (var item in suministros)
        {
            if (item.botonUI != null)
                item.botonUI.onClick.AddListener(() => AgregarItem(item));
            if (item.cantidadTexto != null)
                item.cantidadTexto.text = "0";
        }

        ActualizarCarritoUI();
    }

    void Update()
    {
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

        dineroUI.QuitarDinero(total);
        Debug.Log("Compra realizada por $" + total);

        // Agrupar por tipo (usa nombre directamente)
        var porTipo = AgruparCarritoPorTipo();

        if (almacen != null)
        {
            almacen.AgregarPorTipo(porTipo);
        }
        else
        {
            Debug.LogWarning("Almacen no asignado en SuministrosUI.");
        }

        carrito.Clear();
        total = 0;

        ReiniciarCantidadesVisuales();
        ActualizarCarritoUI();
    }

    Dictionary<string, int> AgruparCarritoPorTipo()
    {
        var result = new Dictionary<string, int>();

        // Usa el nombre del item directamente como tipo
        foreach (var kv in carrito)
        {
            string tipo = kv.Key; // El nombre ES el tipo
            int cantidad = kv.Value;

            if (!result.ContainsKey(tipo))
                result[tipo] = 0;

            result[tipo] += cantidad;
        }

        return result;
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
