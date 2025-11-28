using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class OrdersBoard : MonoBehaviour
{
    [Header("Texto destino")]
    public TextMeshProUGUI targetText;

    [Header("Opcional")]
    public string header = "Órdenes:";

    [Tooltip("Si true, solo muestra NPCs en estado 'esperando'. "
           + "Si false, muestra a cualquiera que ya tenga orden asignada (aparece instantáneo tras interactuar).")]
    public bool showOnlyWaiting = false;

    [Tooltip("Si true, reconstruye el texto cada frame. "
           + "Si false, solo cuando detecta cambios (más eficiente).")]
    public bool rebuildEveryFrame = true;

    string _lastSignature;

    void Reset()
    {
        targetText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
{
    if (targetText != null)
    {
        targetText.enableWordWrapping = false;                 // no cortar líneas
        targetText.overflowMode = TextOverflowModes.Overflow;  // que desborde si hace falta
        targetText.alignment = TextAlignmentOptions.TopLeft;   // opcional
    }

    RebuildIfChanged(force:true);
}


    void Update()
    {
        RebuildIfChanged(force: rebuildEveryFrame);
    }

    void RebuildIfChanged(bool force)
    {
        if (!targetText) return;

        string signature = BuildSignature();
        if (!force && signature == _lastSignature) return;

        _lastSignature = signature;
        targetText.text = BuildDisplayLines();
    }

    // ---------- Firma mínima para detectar cambios ----------
    string BuildSignature()
    {
        var npcs = FindObjectsByType<NpcFollowPath>(FindObjectsSortMode.None);
        if (npcs == null || npcs.Length == 0) return "EMPTY";

        var list = FilterNPCs(npcs);
        if (list.Count == 0) return "NONE";

        var ordered = list
            .OrderBy(n => n.GetWaitIndex() < 0 ? int.MaxValue : n.GetWaitIndex())
            .ThenBy(n => n.GetInstanceID());

        var sb = new StringBuilder(256);
        foreach (var npc in ordered)
        {
            int wi = npc.GetWaitIndex();
            var ord = npc.GetAssignedOrder();
            bool fries = npc.WantsFries();

            sb.Append(wi).Append('|');

            if (ord?.ingredients != null && ord.ingredients.Length > 0)
            {
                for (int i = 0; i < ord.ingredients.Length; i++)
                {
                    sb.Append(ord.ingredients[i]);
                    if (i < ord.ingredients.Length - 1) sb.Append(',');
                }
            }
            else sb.Append("NO_ORDER");

            sb.Append('|').Append(fries ? '1' : '0').Append(';');
        }
        return sb.ToString();
    }

    // ---------- Render ----------
    string BuildDisplayLines()
    {
        var npcs = FindObjectsByType<NpcFollowPath>(FindObjectsSortMode.None);
        if (npcs == null || npcs.Length == 0) return header;

        var list = FilterNPCs(npcs);
        if (list.Count == 0) return header;

        var ordered = list
            .OrderBy(n => n.GetWaitIndex() < 0 ? int.MaxValue : n.GetWaitIndex())
            .ThenBy(n => n.GetInstanceID())
            .ToList();

        var lines = new List<string>(ordered.Count + 1);
        if (!string.IsNullOrWhiteSpace(header)) lines.Add(header);

        int idx = 1;
        foreach (var npc in ordered)
        {
            var order = npc.GetAssignedOrder();
            var items = new List<string>(order.ingredients);
            if (npc.WantsFries()) items.Add("Papas");
            string ingredientes = string.Join(",\u00A0", items);

            int lugar = ComputePlaceIndex(npc);
            lines.Add($"Cliente {idx}: {ingredientes}  Lugar: {lugar}");
            idx++;
        }

        return string.Join("\n", lines);
    }

    // ---------- Filtro configurable ----------
    List<NpcFollowPath> FilterNPCs(NpcFollowPath[] all)
    {
        var result = new List<NpcFollowPath>();
        foreach (var npc in all)
        {
            if (!npc) continue;

            var ord = npc.GetAssignedOrder();
            if (ord == null || ord.ingredients == null || ord.ingredients.Length == 0)
                continue;

            if (showOnlyWaiting)
            {
                // Modo “estricto”: solo cuando están realmente esperando
                if (!npc.IsWaitingNow()) continue;
            }
            // else: modo “instantáneo”: con que tenga orden asignada, lo mostramos

            result.Add(npc);
        }
        return result;
    }

    // ---------- Lugar ----------
    int ComputePlaceIndex(NpcFollowPath npc)
    {
        int wi = npc.GetWaitIndex();
        if (wi >= 0) return wi + 1;

        var normal = PathPointRegistry.Instance ? PathPointRegistry.Instance.GetNormal() : null;
        if (normal == null) return 0;

        int best = -1;
        float bestDist = float.MaxValue;
        Vector3 pos = npc.transform.position;

        for (int i = 0; i < normal.Count; i++)
        {
            var t = normal[i];
            if (!t) continue;
            float d = Vector3.SqrMagnitude(new Vector3(pos.x - t.position.x, 0f, pos.z - t.position.z));
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return (best >= 0) ? best + 1 : 0;
    }
}
