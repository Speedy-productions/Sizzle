using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;

public class Interact : MonoBehaviourPun
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

    private PhotonView view;

    GameObject objetoActualHighlight;
    readonly Dictionary<Renderer, Material[]> originales = new();

    void Start()
    {
        view = GetComponentInParent<PhotonView>();
    }

    void Update()
    {
        if (view != null && !view.IsMine) return;
        
        NpcFollowPath npcApuntado = DetectarNPCApuntado();

        DetectarObjetoPorCapaSinRaycast_ConHighlight();
        CookMeatInPan panApuntado = DetectarPanApuntado();
        MeatCookingState carneEnMano = GetCarneEnMano();

        // Detectar tabla de cortar
        CuttingBoard tablaApuntada = DetectarTablaApuntada();
        SliceIngredient ingredienteEnMano = GetIngredienteEnMano();

        // --- MESA DE ARMADO: detectar y colocar mientras sostienes --
        MesaArmado mesaApuntada = DetectarMesaApuntada();
        Ingredient ingredienteEnManoPedido = GetIngredienteEnManoPedido();

        // --- FREIDORA: detectar y usar mientras sostienes papas ---
        CookFriesInFryer freidoraApuntada = DetectarFreidoraApuntada();
        FriesCookingState friesEnMano = GetFriesEnMano();

        if (panApuntado != ultimoPanApuntado)
        {
            if (ultimoPanApuntado) ultimoPanApuntado.ShowAimHint(false, false);
            ultimoPanApuntado = panApuntado;
        }
        if (panApuntado) panApuntado.ShowAimHint(true, carneEnMano != null);

        // si cambió la freidora apuntada, oculta hint de la anterior
        if (freidoraApuntada != ultimaFreidoraApuntada)
        {
            if (ultimaFreidoraApuntada) ultimaFreidoraApuntada.ShowAimHint(false, false);
            ultimaFreidoraApuntada = freidoraApuntada;
            Debug.Log($"[INTERACT] Freidora apuntada -> {(freidoraApuntada ? freidoraApuntada.name : "NULL")}");
        }

        // mostrar hint en la freidora actual
        if (freidoraApuntada) freidoraApuntada.ShowAimHint(true, friesEnMano != null);


        // ========================================= CONTROLES =========================================

        // E: empezar a cocinar en el sartén apuntado (cocinar)
        if (Input.GetKeyDown(KeyCode.E) && panApuntado && carneEnMano)
        {
            panApuntado.TryStartCooking(carneEnMano);
            return;
        }

        // Q: voltear solo el sartén apuntado (cocinar)
        if (Input.GetKeyDown(KeyCode.Q) && panApuntado && carneEnMano == null)
        {
            panApuntado.TryFlipFromInteraccion();
            return;
        }

        // E: colocar ingrediente en la tabla apuntada (cortar)
        if (Input.GetKeyDown(KeyCode.E) && tablaApuntada && ingredienteEnMano)
        {
            tablaApuntada.TryPlaceIngredient(ingredienteEnMano);
            return;
        }

        // E: colocar ingrediente en la mesa (armar)
        if (Input.GetKeyDown(KeyCode.E) && mesaApuntada && ingredienteEnManoPedido)
        {
            mesaApuntada.TryPlaceIngredientFromHand(ingredienteEnManoPedido);
            return;
        }

        // E: empezar a freír en la freidora apuntada (freir)
        if (Input.GetKeyDown(KeyCode.E) && freidoraApuntada && friesEnMano)
        {
            bool ok = freidoraApuntada.TryStartCooking(friesEnMano);
            return;
        }

        // E/Q: agarrar/soltar
        if (Input.GetKeyDown(KeyCode.E) && objetoSeleccionado && ObjetosEnMano() == 0)
            AgarrarObjeto(objetoSeleccionado);
        if (Input.GetKeyDown(KeyCode.Q) && ObjetosEnMano() > 0)
            SoltarObjeto();

        if (Input.GetKeyDown(KeyCode.Q) && mesaApuntada != null)
        {
            mesaApuntada.CreateCustomBurger(); // Crear la hamburguesa con los ingredientes actuales
        }

        // E: interactuar con el NPC
        if (Input.GetKeyDown(KeyCode.E) && npcApuntado)
        {
            Hamburguesa hamburguesaEnMano = ObtenerHamburguesaEnMano();

            if (hamburguesaEnMano != null && npcApuntado.GetAssignedOrder() != null)
            {
                Order npcOrder = npcApuntado.GetAssignedOrder();
                if (CompararHamburguesaConOrden(hamburguesaEnMano, npcOrder))
                {
                    TransferirHamburguesaAlNpc(npcApuntado, hamburguesaEnMano);

                    DineroUI dineroUI = FindFirstObjectByType<DineroUI>();
                    npcApuntado.popupChar?.MostrarCaraFeliz("¡Bien hecho!");
                    npcApuntado.GetComponent<NpcAudio>()?.PlayHappy();

                    if (dineroUI != null)
                        dineroUI.AgregarDinero(10);

                    Debug.Log("[INTERACT] ¡Hamburguesa entregada correctamente! Dinero agregado.");
                }
                else
                {
                    npcApuntado.popupChar?.MostrarCaraMolesta("¿Qué es esta $#*!?");
                    npcApuntado.GetComponent<NpcAudio>()?.PlayAngry();

                    DineroUI dineroUI = FindFirstObjectByType<DineroUI>();
                    if (dineroUI != null)
                        dineroUI.QuitarDinero(5);

                    Debug.Log("[INTERACT] La hamburguesa no coincide con la orden del NPC. Dinero restado.");
                }
            }
            else
            {
                npcApuntado.OnPlayerInteracted();
            }
        }
    }

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
        Debug.Log("[INTERACT] La hamburguesa ha sido transferida al NPC.");
        npcApuntado.SetHamburguesaEnMano(hamburguesa);
    }

    int ObjetosEnMano() => SlotMano() ? SlotMano().childCount : 0;

    Transform SlotMano() => puntoDeAgarre ? puntoDeAgarre : manoJugador;

    // ====================== DETECCIÓN / HIGHLIGHT ==========================

    void DetectarObjetoPorCapaSinRaycast_ConHighlight()
    {
        if (!camaraJugador) { LimpiarHighlight(); objetoSeleccionado = null; return; }

        var todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int mask = pickableLayers.value == 0 ? ~0 : pickableLayers.value;
        var slot = SlotMano();

        var candidatos = new List<GameObject>();
        foreach (var go in todos)
{
    // ✅ Aceptamos:
    // - Objetos en capas pickables
    // - O con tag "Food"
    // - O con tag "Burger"
    // - O que tengan componente Hamburguesa (por si falla el tag)
    if (((1 << go.layer) & mask) == 0 
        && !go.CompareTag("Food") 
        && !go.CompareTag("Burger")
        && !go.GetComponent<Hamburguesa>())
        continue;

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

    // ====================== FÍSICAS BASE ==========================

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

    // ====================== AGARRAR / SOLTAR ==========================

    void AgarrarObjeto(GameObject objeto)
    {
        LimpiarHighlight();

        PhotonView pv = objeto.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            pv.RequestOwnership();

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

        var mesa = objeto.GetComponentInParent<MesaArmado>();
        if (mesa != null) mesa.RemoveIngredient(objeto);

        var tabla = objeto.GetComponentInParent<CuttingBoard>();
        if (tabla != null) tabla.RemoveIngredient();

        // ⚠ Carne: si estaba en sartén, dejamos de tratarla como 'OnPan' SOLO en el jugador que la agarra
        var meat = objeto.GetComponent<MeatCookingState>();
        if (meat != null)
        {
            meat.LockOnTable(false);
            meat.SetOnPan(false);   // ahora es "en mano" / "suelta", no en sartén
        }

        // 🔥 FIX UNIVERSAL: congelar físicas inmediatamente en el cliente local
        Rigidbody rbUniversal = objeto.GetComponent<Rigidbody>();
        if (rbUniversal)
        {
            rbUniversal.isKinematic = true;
            rbUniversal.useGravity = false;
            rbUniversal.linearVelocity = Vector3.zero;
            rbUniversal.angularVelocity = Vector3.zero;
        }

        // Ignorar colisión con el jugador local
        if (jugador)
        {
            var playerCol = jugador.GetComponent<Collider>();
            if (playerCol)
                foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
                    Physics.IgnoreCollision(playerCol, c, true);
        }

        // La parte de reparent & físicas la hace el RPC para TODOS (incluido este cliente)
        if (pv != null && view != null)
        {
            view.RPC(nameof(RPC_AvisarAgarrarObjeto), RpcTarget.AllBuffered, pv.ViewID, view.ViewID);
        }
        else
        {
            // Single player / sin Photon
            Transform destino = SlotMano();
            if (!destino) return;

            Vector3 Sw = WorldScaleUtils.GetOrInitWorldScaleMemory(objeto.transform);
            WorldScaleUtils.ReparentKeepWorldScale(objeto.transform, destino, Sw);
            objeto.transform.localPosition = Vector3.zero;
            objeto.transform.localRotation = Quaternion.identity;
            ConfigurarFisicaObjeto(objeto, true);

            foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
                c.isTrigger = true;
        }
    }

    void SoltarObjeto()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return;

        var objeto = slot.GetChild(0).gameObject;
        PhotonView pv = objeto.GetComponent<PhotonView>();

        // Posición de drop calculada localmente
        Vector3 dropPos = camaraJugador.transform.position + camaraJugador.transform.forward * distanciaSoltar;
        objeto.transform.position = dropPos;
        objeto.transform.SetParent(null);

        // Flags locales (no re-parent aquí, eso lo maneja el RPC también pero da igual)
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

        // Todas las físicas del drop se aplican en el RPC para que TODOS vean lo mismo
        if (pv != null && view != null)
        {
            view.RPC(nameof(RPC_AvisarSoltarObjeto), RpcTarget.AllBuffered,
                pv.ViewID,
                dropPos,
                camaraJugador.transform.forward);
        }
        else
        {
            // Single player
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
        }
    }

    // ====================== DETECCIÓN EXTRA ==========================

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

    // ====================== RPCs AGARRAR / SOLTAR ==========================

    [PunRPC]
    void RPC_AvisarAgarrarObjeto(int objetoViewID, int jugadorViewID)
    {
        PhotonView objetoPV = PhotonView.Find(objetoViewID);
        PhotonView jugadorPV = PhotonView.Find(jugadorViewID);
        if (!objetoPV || !jugadorPV) return;

        Transform destino = jugadorPV.GetComponentInChildren<Interact>().SlotMano();
        if (!destino) return;

        Transform t = objetoPV.transform;

        // Guardar/obtener escala mundial coherente
        Vector3 worldScale = WorldScaleUtils.GetOrInitWorldScaleMemory(t);
        WorldScaleUtils.ReparentKeepWorldScale(t, destino, worldScale);

        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        Rigidbody rb = objetoPV.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Collider c in objetoPV.GetComponentsInChildren<Collider>())
            c.isTrigger = true;
    }

    [PunRPC]
    void RPC_AvisarSoltarObjeto(int objetoViewID, Vector3 position, Vector3 forward)
    {
        PhotonView objetoPV = PhotonView.Find(objetoViewID);
        if (!objetoPV) return;

        Transform t = objetoPV.transform;
        t.SetParent(null);
        t.position = position;

        Rigidbody rb = objetoPV.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = forward * 3f + Vector3.up * 0.75f;
            rb.angularVelocity = Random.insideUnitSphere * 1f;
        }

        foreach (Collider c in objetoPV.GetComponentsInChildren<Collider>())
            c.isTrigger = false;
    }

    void OnDisable() => LimpiarHighlight();
}
