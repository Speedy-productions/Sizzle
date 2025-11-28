using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;

[System.Serializable]
public class ItemData
{
    public string name;
    public int quantity;
    public GameObject prefab;
    public Button boton;
    public Text cantidadTexto;
}

[System.Serializable]
public class PanData
{
    public string name;
    public GameObject prefab;
}

public class Almacen : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();
    
    [Header("Pan (2 prefabs con un solo botón y texto)")]
    public List<PanData> panes = new List<PanData>();
    public Button panBoton;
    public Text panCantidadTexto;
    private int cantidadPanTotal = 5;

    [Header("Offsets de spawn para las dos piezas de pan")]
    public Vector3 panOffsetA = new Vector3(-0.12f, 0f, 0f);
    public Vector3 panOffsetB = new Vector3(0.12f, 0f, 0f);

    [Header("Punto de aparición")]
    public Transform spawnPoint;

    void Start()
    {
        // Botones de items
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            if (items[i].boton != null)
                items[i].boton.onClick.AddListener(() => SpawnItem(index));
        }

        // Botón compartido de pan: spawnea ambas piezas
        if (panBoton != null)
        {
            panBoton.onClick.RemoveAllListeners();
            panBoton.onClick.AddListener(SpawnPanAmbos);
        }

        ActualizarTextos();
    }

    public void AgregarPorTipo(Dictionary<string, int> porTipo)
    {
        if (porTipo == null) return;

        foreach (var kv in porTipo)
        {
            string tipo = kv.Key;
            int cantidad = kv.Value;

            if (string.IsNullOrEmpty(tipo) || cantidad <= 0) continue;

            var item = items.Find(i => i.name == tipo);
            if (item != null)
            {
                item.quantity += cantidad;
                ActualizarTextos();
                continue;
            }

            // Si es pan (coincide por nombre), suma a cantidad total compartida
            bool esPan = panes.Exists(p => p.name == tipo);
            if (esPan)
            {
                cantidadPanTotal += cantidad;
                ActualizarTextos();
            }
            else
            {
                Debug.LogWarning($"Tipo '{tipo}' no encontrado en items ni panes del Almacen.");
            }
        }
    }

    public void SpawnItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        var data = items[index];
        if (data.quantity <= 0) return;
        if (data.prefab == null)
        {
            Debug.LogError($"Prefab nulo para '{data.name}'.");
            return;
        }

        // Requiere que el prefab esté en una carpeta Resources y se use su nombre
        string prefabName = data.prefab.name;
        GameObject go = PhotonNetwork.Instantiate(prefabName, spawnPoint.position, spawnPoint.rotation);

        // Opcional: asegurar que el local sea el owner/controller
        var pv = go.GetComponent<PhotonView>();
        if (pv != null && !pv.AmOwner)
        {
            // Si OwnershipTransfer del PV permite Request/Takeover
            pv.RequestOwnership();
        }

        data.quantity--;
        ActualizarTextos();
    }

    // Spawnea ambas piezas de pan a la vez y descuenta 1 del stock compartido
    public void SpawnPanAmbos()
    {
        if (cantidadPanTotal <= 0) return;
        if (panes == null || panes.Count < 2)
        {
            Debug.LogWarning("Se requieren 2 prefabs en 'panes' (superior e inferior).");
            // Fallback: si hay solo 1, instancia el primero
            if (panes != null && panes.Count == 1 && panes[0].prefab != null)
            {
                PhotonNetwork.Instantiate(panes[0].prefab.name, GetPanPos(panOffsetA), spawnPoint ? spawnPoint.rotation : Quaternion.identity);
                cantidadPanTotal--;
                ActualizarTextos();
            }
            return;
        }

        var pA = panes[0]?.prefab;
        var pB = panes[1]?.prefab;
        if (pA == null || pB == null)
        {
            Debug.LogError("Prefabs de pan no asignados en 'panes[0]' y 'panes[1]'.");
            return;
        }

        var goA = PhotonNetwork.Instantiate(pA.name, GetPanPos(panOffsetA), spawnPoint ? spawnPoint.rotation : Quaternion.identity);
        var goB = PhotonNetwork.Instantiate(pB.name, GetPanPos(panOffsetB), spawnPoint ? spawnPoint.rotation : Quaternion.identity);

        var pvA = goA.GetComponent<Photon.Pun.PhotonView>();
        var pvB = goB.GetComponent<Photon.Pun.PhotonView>();
        if (pvA != null && !pvA.AmOwner) pvA.RequestOwnership();
        if (pvB != null && !pvB.AmOwner) pvB.RequestOwnership();

        cantidadPanTotal--; // 1 unidad de "pan" produce ambas piezas
        ActualizarTextos();
    }

    // Mantengo este por si alguna vez quieres spawnear una sola pieza (no usado por el botón)
    public void SpawnPan(int indexPrefab)
    {
        if (indexPrefab < 0 || indexPrefab >= panes.Count) return;
        if (cantidadPanTotal <= 0) return;

        var pd = panes[indexPrefab];
        if (pd.prefab == null)
        {
            Debug.LogError("Prefab de Pan nulo.");
            return;
        }

        string prefabName = pd.prefab.name;
        GameObject go = PhotonNetwork.Instantiate(prefabName, spawnPoint ? spawnPoint.position : transform.position, spawnPoint ? spawnPoint.rotation : transform.rotation);

        var pv = go.GetComponent<Photon.Pun.PhotonView>();
        if (pv != null && !pv.AmOwner)
            pv.RequestOwnership();

        cantidadPanTotal--;
        ActualizarTextos();
    }

    Vector3 GetPanPos(Vector3 localOffset)
    {
        if (spawnPoint != null)
            return spawnPoint.TransformPoint(localOffset);
        return transform.TransformPoint(localOffset);
    }

    void ActualizarTextos()
    {
        foreach (var item in items)
        {
            if (item.cantidadTexto != null)
                item.cantidadTexto.text = item.quantity.ToString();
        }

        if (panCantidadTexto != null)
            panCantidadTexto.text = cantidadPanTotal.ToString();
    }
}