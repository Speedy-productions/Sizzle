using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;

public class Interact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform manoJugador;     // hueso/objeto de la mano
    public GameObject jugador;        // para ignorar colisiones al agarrar
    public Camera camaraJugador;      // cámara del jugador

    [Header("Distancias")]
    public float distanciaInteraccion = 2.5f; // pickables
    public float distanciaSoltar = 1.2f;      // drop frente a la cámara

    [Header("Capas")]
    public LayerMask pickableLayers;          // qué se puede agarrar

    [Header("Detección por pantalla")]
    [Range(0.01f, 0.3f)] public float radioPantalla = 0.12f;

    [Header("Sartén (apuntar)")]
    public float distanciaPan = 6f;
    public float radioPantallaPan = 0.12f;

    [Header("Punto de agarre")]
    public Transform puntoDeAgarre;         // empty hijo de la mano

    [Header("Lanzamiento al soltar")]
    public float fuerzaLanzamiento = 3f;
    public float fuerzaVertical = 0.75f;
    public float torqueLanzamiento = 1f;

    [Header("Highlight (overlay unlit)")]
    public Material highlightOverlayMat;    // material unlit/transparent
    [ColorUsage(true, true)] public Color highlightColor = new(1f, 1f, 0.2f, 0.6f);
    public float highlightIntensity = 2.5f;
    public bool instanciarOverlay = true;

    GameObject objetoSeleccionado;
    CookMeatInPan ultimoPanApuntado;
    CookFriesInFryer ultimaFreidoraApuntada;

    GameObject objetoActualHighlight;
    readonly Dictionary<Renderer, Material[]> originales = new();


    void Update()
    {
        NpcFollowPath npcApuntado = DetectarNPCApuntado();

        DetectarObjetoPorCapaSinRaycast_ConHighlight();
        CookMeatInPan panApuntado = DetectarPanApuntado();
        MeatCookingState carneEnMano = GetCarneEnMano();

        // Detectar tabla de cortar
        CuttingBoard tablaApuntada = DetectarTablaApuntada();
        SliceIngredient ingredienteEnMano = GetIngredienteEnMano();

        // --- MESA DE ARMADO ---
        MesaArmado mesaApuntada = DetectarMesaApuntada();
        Ingredient ingredienteEnManoPedido = GetIngredienteEnManoPedido();

        // --- FREIDORA ---
        CookFriesInFryer freidoraApuntada = DetectarFreidoraApuntada();
        FriesCookingState friesEnMano = GetFriesEnMano();


        if (panApuntado != ultimoPanApuntado)
        {
            if (ultimoPanApuntado) ultimoPanApuntado.ShowAimHint(false, false);
            ultimoPanApuntado = panApuntado;
        }
        if (panApuntado) panApuntado.ShowAimHint(true, carneEnMano != null);

        if (freidoraApuntada != ultimaFreidoraApuntada)
        {
            if (ultimaFreidoraApuntada) ultimaFreidoraApuntada.ShowAimHint(false, false);
            ultimaFreidoraApuntada = freidoraApuntada;
            Debug.Log($"[INTERACT] Freidora apuntada -> {(freidoraApuntada ? freidoraApuntada.name : "NULL")}");
        }

        if (freidoraApuntada) freidoraApuntada.ShowAimHint(true, friesEnMano != null);


        // =======================================================
        // CONTROLES
        // =======================================================

        // E: empezar a cocinar en el sartén apuntado
        if (Input.GetKeyDown(KeyCode.E) && panApuntado && carneEnMano)
        {
            panApuntado.TryStartCooking(carneEnMano);
            return;
        }

        // Q: voltear sartén
        if (Input.GetKeyDown(KeyCode.Q) && panApuntado && carneEnMano == null)
        {
            panApuntado.TryFlipFromInteraccion();
            return;
        }

        // E: colocar en tabla
        if (Input.GetKeyDown(KeyCode.E) && tablaApuntada && ingredienteEnMano)
        {
            tablaApuntada.TryPlaceIngredient(ingredienteEnMano);
            return;
        }

        // E: colocar en mesa de armado
        if (Input.GetKeyDown(KeyCode.E) && mesaApuntada && ingredienteEnManoPedido)
        {
            mesaApuntada.TryPlaceIngredientFromHand(ingredienteEnManoPedido);
            return;
        }

        // E: freidora
        if (Input.GetKeyDown(KeyCode.E) && freidoraApuntada && friesEnMano)
        {
            bool ok = freidoraApuntada.TryStartCooking(friesEnMano);
            return;
        }

        // E/Q: agarrar / soltar objetos generales
        if (Input.GetKeyDown(KeyCode.E) && objetoSeleccionado && ObjetosEnMano() == 0)
            AgarrarObjeto(objetoSeleccionado);

        if (Input.GetKeyDown(KeyCode.Q) && ObjetosEnMano() > 0)
            SoltarObjeto();

        // Q: crear hamburguesa custom en mesa de armado
        if (Input.GetKeyDown(KeyCode.Q) && mesaApuntada != null)
        {
            mesaApuntada.CreateCustomBurger();
        }

        // =======================================================
        // E: INTERACCIÓN CON NPC (PEDIDO / ENTREGA BURGER)
        // =======================================================

        if (Input.GetKeyDown(KeyCode.E) && npcApuntado)
        {
            Hamburguesa hamburguesaEnMano = ObtenerHamburguesaEnMano();

            if (hamburguesaEnMano != null && npcApuntado.GetAssignedOrder() != null)
            {
                Order npcOrder = npcApuntado.GetAssignedOrder();
                bool esCorrecta = CompararHamburguesaConOrden(hamburguesaEnMano, npcOrder);

                bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

                if (!mp)
                {
                    // --------- SINGLE PLAYER ---------
                    if (esCorrecta)
                    {
                        TransferirHamburguesaAlNpc(npcApuntado, hamburguesaEnMano);

                        DineroUI dineroUI = Object.FindFirstObjectByType<DineroUI>();
                        npcApuntado.popupChar?.MostrarCaraFeliz("¡Bien hecho!");
                        npcApuntado.GetComponent<NpcAudio>()?.PlayHappy();

                        if (dineroUI != null)
                            dineroUI.AgregarDinero(10);

                        Debug.Log("[INTERACT] (SP) Hamburguesa correcta. Dinero agregado.");
                    }
                    else
                    {
                        npcApuntado.popupChar?.MostrarCaraMolesta("¿Qué es esta $#*!?");
                        npcApuntado.GetComponent<NpcAudio>()?.PlayAngry();

                        DineroUI dineroUI = Object.FindFirstObjectByType<DineroUI>();
                        if (dineroUI != null)
                            dineroUI.QuitarDinero(5);

                        Debug.Log("[INTERACT] (SP) Hamburguesa incorrecta. Dinero restado.");
                    }
                }
                else
                {
                    // --------- MULTIPLAYER ---------
                    if (esCorrecta)
                    {
                        // 🔹 CUALQUIER jugador puede pedir la entrega
                        TransferirHamburguesaAlNpc(npcApuntado, hamburguesaEnMano);

                        // El dinero y el audio solo los controla el Master (opcional)
                        if (PhotonNetwork.IsMasterClient)
                        {
                            npcApuntado.GetComponent<NpcAudio>()?.PlayHappy();

                            DineroUI dineroUI = Object.FindFirstObjectByType<DineroUI>();
                            if (dineroUI != null)
                                dineroUI.AgregarDinero(10);
                        }

                        Debug.Log("[INTERACT] (MP) Hamburguesa correcta. Entrega solicitada al NPC.");
                    }
                    else
                    {
                        // Mantengo la lógica de incorrecta igual que tenías
                        if (PhotonNetwork.IsMasterClient)
                        {
                            npcApuntado.GetComponent<NpcAudio>()?.PlayAngry();

                            // Cara enojada para TODOS
                            npcApuntado.photonView.RPC(
                                nameof(NpcFollowPath.RPC_ShowAngryFace),
                                RpcTarget.All,
                                "¿Qué es esta $#*!?"
                            );

                            DineroUI dineroUI = Object.FindFirstObjectByType<DineroUI>();
                            if (dineroUI != null)
                                dineroUI.QuitarDinero(5);

                            Debug.Log("[INTERACT] (MP Master) Hamburguesa incorrecta. Dinero restado.");
                        }
                        else
                        {
                            Debug.Log("[INTERACT] (MP Client) Hamburguesa incorrecta, el Master se encarga de la penalización.");
                        }
                    }
                }
            }
            else
            {
                // No hay hamburguesa válida: solo abrir popup de pedido
                npcApuntado.OnPlayerInteracted();
            }
        }
    }


    // -------------------------------------------------------------------
    // HAMBURGUESA / ORDEN
    // -------------------------------------------------------------------

    Hamburguesa ObtenerHamburguesaEnMano()
    {
        var slot = SlotMano();
        if (slot == null || slot.childCount == 0) return null;
        return slot.GetChild(0).GetComponent<Hamburguesa>();
    }

    bool CompararHamburguesaConOrden(Hamburguesa hamburguesa, Order npcOrder)
    {
        List<string> ingredientesHamburguesa = hamburguesa.GetIngredientes();
        List<string> ingredientesOrden = new List<string>(npcOrder.ingredients);

        ingredientesHamburguesa = ingredientesHamburguesa.Select(NormalizarNombre).ToList();
        ingredientesOrden = ingredientesOrden.Select(NormalizarNombre).ToList();

        ingredientesHamburguesa.Sort();
        ingredientesOrden.Sort();

        Debug.Log("[DEBUG] Ingredientes Hamburguesa: " + string.Join(", ", ingredientesHamburguesa));
        Debug.Log("[DEBUG] Ingredientes Orden: " + string.Join(", ", ingredientesOrden));

        return ingredientesHamburguesa.SequenceEqual(ingredientesOrden);
    }

    void TransferirHamburguesaAlNpc(NpcFollowPath npcApuntado, Hamburguesa hamburguesa)
    {
        Debug.Log("[INTERACT] La hamburguesa ha sido transferida al NPC (solicitado).");
        npcApuntado.SetHamburguesaEnMano(hamburguesa);
    }


    // -------------------------------------------------------------------
    // UTILIDAD: MANO / OBJETOS
    // -------------------------------------------------------------------

    int ObjetosEnMano() => SlotMano() ? SlotMano().childCount : 0;

    Transform SlotMano() => puntoDeAgarre ? puntoDeAgarre : manoJugador;


    // -------------------------------------------------------------------
    // DETECCIÓN Y HIGHLIGHT (igual que lo tenías)
    // -------------------------------------------------------------------

    void DetectarObjetoPorCapaSinRaycast_ConHighlight()
    {
        if (!camaraJugador) { LimpiarHighlight(); objetoSeleccionado = null; return; }

        var todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int mask = pickableLayers.value == 0 ? ~0 : pickableLayers.value;
        var slot = SlotMano();

        var candidatos = new List<GameObject>();
        foreach (var go in todos)
        {
            if (((1 << go.layer) & mask) == 0 && !go.CompareTag("Food")) continue;
            if (slot && go.transform.IsChildOf(slot)) continue;
            if (go == jugador) continue;

            var mesaPadre = go.GetComponentInParent<MesaArmado>();
            if (mesaPadre && mesaPadre.Contains(go) && !mesaPadre.IsTopIngredient(go))
                continue;
            candidatos.Add(go);
        }

        GameObject mejor = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new(0.5f, 0.5f);

        foreach (var go in candidatos)
        {
            var r = go.GetComponentInChildren<Renderer>();
            Vector3 pos = r ? r.bounds.center : go.transform.position;

            var vp = camaraJugador.WorldToViewportPoint(pos);
            if (vp.z <= 0f) continue;

            float dPantalla = Vector2.Distance(new(vp.x, vp.y), centro);
            if (dPantalla > radioPantalla) continue;

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > Mathf.Max(7f, distanciaInteraccion + 0.6f)) continue;

            float score = dPantalla * 10f + dist;
            if (score < mejorScore) { mejorScore = score; mejor = go; }
        }

        if (mejor != objetoActualHighlight)
        {
            LimpiarHighlight();
            if (mejor) AplicarHighlight(mejor);
        }

        objetoSeleccionado = mejor;
    }

    void AplicarHighlight(GameObject go)
    {
        var meat = go.GetComponentInParent<MeatCookingState>();
        if (meat && meat.isOnPan) return;

        var fries = go.GetComponentInParent<FriesCookingState>() ?? go.GetComponent<FriesCookingState>();
        if (fries && fries.isInFryer) return;

        if (!highlightOverlayMat) return;

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        objetoActualHighlight = go;
        originales.Clear();

        Color hdr = highlightColor * highlightIntensity;

        foreach (var rend in renderers)
        {
            if (!rend) continue;

            var matsOrig = rend.sharedMaterials;
            originales[rend] = matsOrig;

            var nuevos = new List<Material>(matsOrig.Length + 1);
            for (int i = 0; i < matsOrig.Length; i++) nuevos.Add(matsOrig[i]);

            var overlay = instanciarOverlay ? new Material(highlightOverlayMat) : highlightOverlayMat;
            if (overlay.HasProperty("_BaseColor")) overlay.SetColor("_BaseColor", hdr);
            if (overlay.HasProperty("_Color")) overlay.SetColor("_Color", hdr);

            nuevos.Add(overlay);
            rend.materials = nuevos.ToArray();
        }
    }

    public void LimpiarHighlight()
    {
        if (!objetoActualHighlight) return;

        foreach (var kv in originales)
        {
            var rend = kv.Key;
            if (!rend) continue;
            try { rend.materials = kv.Value; } catch { }
        }

        originales.Clear();
        objetoActualHighlight = null;
    }

    CookMeatInPan DetectarPanApuntado()
    {
        var pans = Object.FindObjectsByType<CookMeatInPan>(FindObjectsSortMode.None);
        if (pans.Length == 0) return null;

        CookMeatInPan mejor = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new(0.5f, 0.5f);

        foreach (var pan in pans)
        {
            var r = pan.GetComponentInChildren<Renderer>();
            Vector3 pos = r ? r.bounds.center : pan.transform.position;

            var vp = camaraJugador.WorldToViewportPoint(pos);
            if (vp.z <= 0f) continue;

            float dPantalla = Vector2.Distance(new(vp.x, vp.y), centro);
            if (dPantalla > radioPantallaPan) continue;

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > distanciaPan) continue;

            float score = dPantalla * 10f + dist;
            if (score < mejorScore) { mejorScore = score; mejor = pan; }
        }

        return mejor;
    }

    CookFriesInFryer DetectarFreidoraApuntada()
    {
        var fryers = Object.FindObjectsByType<CookFriesInFryer>(FindObjectsSortMode.None);
        if (fryers.Length == 0) return null;

        CookFriesInFryer mejor = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new(0.5f, 0.5f);

        foreach (var fryer in fryers)
        {
            var r = fryer.GetComponentInChildren<Renderer>();
            Vector3 pos = r ? r.bounds.center : fryer.transform.position;

            var vp = camaraJugador.WorldToViewportPoint(pos);
            if (vp.z <= 0f) continue;

            float dPantalla = Vector2.Distance(new(vp.x, vp.y), centro);
            if (dPantalla > radioPantallaPan) continue;

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > distanciaPan) continue;

            float score = dPantalla * 10f + dist;
            if (score < mejorScore) { mejorScore = score; mejor = fryer; }
        }

        return mejor;
    }

    FriesCookingState GetFriesEnMano()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return null;
        return slot.GetChild(0).GetComponent<FriesCookingState>();
    }

    MeatCookingState GetCarneEnMano()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return null;
        return slot.GetChild(0).GetComponent<MeatCookingState>();
    }

    void ConfigurarFisicaObjeto(GameObject objeto, bool enMano)
    {
        var rb = objeto.GetComponent<Rigidbody>();
        if (!rb) return;

        if (enMano)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void AgarrarObjeto(GameObject objeto)
    {
        LimpiarHighlight();

        var audio = objeto.GetComponent<MeatCookingAudio>();
        if (audio != null) audio.StopImmediately();

        var friesAudio = objeto.GetComponent<FriesCookingAudio>();
        if (friesAudio != null) friesAudio.StopImmediately();

        var mesaTopCheck = objeto.GetComponentInParent<MesaArmado>();
        if (mesaTopCheck && mesaTopCheck.Contains(objeto) && !mesaTopCheck.IsTopIngredient(objeto))
        {
            Debug.Log("[INTERACT] No puedes agarrar un ingrediente que no sea el tope de la pila.");
            return;
        }

        var destino = SlotMano();

        var mesa = objeto.GetComponentInParent<MesaArmado>();
        if (mesa != null) mesa.RemoveIngredient(objeto);

        var tabla = objeto.GetComponentInParent<CuttingBoard>();
        if (tabla != null) tabla.RemoveIngredient();

        Vector3 Sw = WorldScaleUtils.GetOrInitWorldScaleMemory(objeto.transform);
        WorldScaleUtils.ReparentKeepWorldScale(objeto.transform, destino, Sw);

        objeto.transform.localPosition = Vector3.zero;
        objeto.transform.localRotation = Quaternion.identity;

        ConfigurarFisicaObjeto(objeto, true);

        var meat = objeto.GetComponent<MeatCookingState>();
        if (meat != null) meat.LockOnTable(false);

        foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;

        if (jugador)
        {
            var playerCol = jugador.GetComponent<Collider>();
            if (playerCol)
                foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
                    Physics.IgnoreCollision(playerCol, c, true);
        }
    }

    void SoltarObjeto()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return;

        var objeto = slot.GetChild(0).gameObject;
        objeto.transform.SetParent(null);

        objeto.transform.position = camaraJugador.transform.position + camaraJugador.transform.forward * distanciaSoltar;

        var rb = objeto.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 impulso = camaraJugador.transform.forward * fuerzaLanzamiento + Vector3.up * fuerzaVertical;
            rb.AddForce(impulso, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * torqueLanzamiento, ForceMode.VelocityChange);
        }

        foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
            c.isTrigger = false;

        if (objeto.TryGetComponent(out MeatCookingState meat))
        {
            meat.LockOnTable(false);
            meat.SetCollidersAsTrigger(false);
        }
        if (objeto.TryGetComponent(out FriesCookingState fries))
        {
            fries.isInFryer = false;
            fries.SetCollidersAsTrigger(false);
        }

        if (jugador)
        {
            var playerCol = jugador.GetComponent<Collider>();
            if (playerCol)
                foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
                    Physics.IgnoreCollision(playerCol, c, false);
        }
    }

    CuttingBoard DetectarTablaApuntada()
    {
        var tablas = Object.FindObjectsByType<CuttingBoard>(FindObjectsSortMode.None);
        if (tablas.Length == 0) return null;

        CuttingBoard mejor = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new(0.5f, 0.5f);

        foreach (var tabla in tablas)
        {
            var r = tabla.GetComponentInChildren<Renderer>();
            Vector3 pos = r ? r.bounds.center : tabla.transform.position;

            var vp = camaraJugador.WorldToViewportPoint(pos);
            if (vp.z <= 0f) continue;

            float dPantalla = Vector2.Distance(new(vp.x, vp.y), centro);
            if (dPantalla > radioPantallaPan) continue;

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > distanciaPan) continue;

            float score = dPantalla * 10f + dist;
            if (score < mejorScore) { mejorScore = score; mejor = tabla; }
        }

        return mejor;
    }

    SliceIngredient GetIngredienteEnMano()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return null;
        return slot.GetChild(0).GetComponent<SliceIngredient>();
    }

    MesaArmado DetectarMesaApuntada()
    {
        var mesas = Object.FindObjectsByType<MesaArmado>(FindObjectsSortMode.None);
        if (mesas.Length == 0) return null;

        MesaArmado mejor = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new(0.5f, 0.5f);

        foreach (var mesa in mesas)
        {
            var r = mesa.GetComponentInChildren<Renderer>();
            Vector3 pos = r ? r.bounds.center : mesa.transform.position;

            var vp = camaraJugador.WorldToViewportPoint(pos);
            if (vp.z <= 0f) continue;

            float dPantalla = Vector2.Distance(new(vp.x, vp.y), centro);
            if (dPantalla > radioPantallaPan) continue;

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > distanciaPan) continue;

            float score = dPantalla * 10f + dist;
            if (score < mejorScore) { mejorScore = score; mejor = mesa; }
        }

        return mejor;
    }

    Ingredient GetIngredienteEnManoPedido()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return null;

        var go = slot.GetChild(0).gameObject;

        var ing = go.GetComponent<Ingredient>();
        if (ing != null) return ing;

        var meat = go.GetComponent<MeatCookingState>();
        if (meat != null)
        {
            var tmp = go.AddComponent<Ingredient>();
            tmp.ingredientName = meat.meatName;
            tmp.requireCookedMeat = true;
            return tmp;
        }

        var slice = go.GetComponent<SliceIngredient>();
        if (slice != null)
        {
            var tmp = go.AddComponent<Ingredient>();
            tmp.ingredientName = NormalizarNombre(slice.ingredientName);
            tmp.requireCookedMeat = false;
            return tmp;
        }

        return null;
    }

    string NormalizarNombre(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace("Sliced", "").Replace("Slice", "").Replace("Cortado", "");
        return name.Trim();
    }

    NpcFollowPath DetectarNPCApuntado()
    {
        var npcs = Object.FindObjectsByType<NpcFollowPath>(FindObjectsSortMode.None);
        if (npcs.Length == 0) return null;

        NpcFollowPath mejor = null;
        float mejorScore = float.MaxValue;
        Vector2 centro = new(0.5f, 0.5f);

        foreach (var npc in npcs)
        {
            if (!npc.IsWaitingForPlayer()) continue;

            var r = npc.GetComponentInChildren<Renderer>();
            Vector3 pos = r ? r.bounds.center : npc.transform.position;

            var vp = camaraJugador.WorldToViewportPoint(pos);
            if (vp.z <= 0f) continue;

            float dPantalla = Vector2.Distance(new(vp.x, vp.y), centro);
            if (dPantalla > radioPantallaPan) continue;

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > distanciaPan) continue;

            float score = dPantalla * 10f + dist;
            if (score < mejorScore)
            {
                mejorScore = score;
                mejor = npc;
            }
        }

        return mejor;
    }

    void OnDisable() => LimpiarHighlight();
}
