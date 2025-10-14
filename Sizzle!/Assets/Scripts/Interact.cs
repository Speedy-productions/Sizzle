using UnityEngine;
using System.Collections.Generic;

public class Interact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform manoJugador;     // hueso/objeto de la mano
    public GameObject jugador;        // para ignorar colisiones al agarrar
    public Camera camaraJugador;      // cámara del jugador

    [Header("Distancias")]
    public float distanciaInteraccion = 2.5f; // pickables
    public float distanciaSoltar = 1.2f;    // drop frente a la cámara

    [Header("Capas")]
    public LayerMask pickableLayers;        // qué se puede agarrar

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
        DetectarObjetoPorCapaSinRaycast_ConHighlight();

        // Sartén apuntado (se usa para T y Q)
        CookMeatInPan panApuntado = DetectarPanApuntado();
        MeatCookingState carneEnMano = GetCarneEnMano();

        if (panApuntado != ultimoPanApuntado)
        {
            if (ultimoPanApuntado) ultimoPanApuntado.ShowAimHint(false, false);
            ultimoPanApuntado = panApuntado;
        }

        if (panApuntado) panApuntado.ShowAimHint(true, carneEnMano != null);

        // T: empezar a cocinar en el sartén apuntado
        if (Input.GetKeyDown(KeyCode.T) && panApuntado && carneEnMano)
            panApuntado.TryStartCooking(carneEnMano);

        // Detectar tabla de cortar
        CuttingBoard tablaApuntada = DetectarTablaApuntada();
        SliceIngredient ingredienteEnMano = GetIngredienteEnMano();

        // T: colocar ingrediente en la tabla apuntada (cortar)
        if (Input.GetKeyDown(KeyCode.T) && tablaApuntada && ingredienteEnMano)
            tablaApuntada.TryPlaceIngredient(ingredienteEnMano);

        // --- MESA DE ARMADO: detectar y colocar mientras sostienes --
        MesaArmado mesaApuntada = DetectarMesaApuntada();
        Ingredient ingredienteEnManoPedido = GetIngredienteEnManoPedido();

        if (Input.GetKeyDown(KeyCode.T) && mesaApuntada && ingredienteEnManoPedido)
            mesaApuntada.TryPlaceIngredientFromHand(ingredienteEnManoPedido);

        // Q: voltear solo el sartén apuntado
        if (Input.GetKeyDown(KeyCode.Q) && panApuntado)
            panApuntado.TryFlipFromInteraccion();

        // --- FREIDORA: detectar y usar mientras sostienes papas ---
        CookFriesInFryer freidoraApuntada = DetectarFreidoraApuntada();
        FriesCookingState friesEnMano = GetFriesEnMano();

        // si cambió la freidora apuntada, oculta hint de la anterior
        if (freidoraApuntada != ultimaFreidoraApuntada)
        {
            if (ultimaFreidoraApuntada) ultimaFreidoraApuntada.ShowAimHint(false, false);
            ultimaFreidoraApuntada = freidoraApuntada;
            Debug.Log($"[INTERACT] Freidora apuntada -> {(freidoraApuntada ? freidoraApuntada.name : "NULL")}");
        }

        // mostrar hint en la freidora actual
        if (freidoraApuntada)
            freidoraApuntada.ShowAimHint(true, friesEnMano != null);

        // T: empezar a freír en la freidora apuntada
        if (Input.GetKeyDown(KeyCode.T) && freidoraApuntada && friesEnMano)
        {
            Debug.Log($"[INTERACT] T -> TryStartCooking FRIES con {friesEnMano.name} en {freidoraApuntada.name}");
            bool ok = freidoraApuntada.TryStartCooking(friesEnMano);
            Debug.Log($"[INTERACT] TryStartCooking(FRIES) resultado={ok}");
        }


        // E/G: agarrar/soltar
        if (Input.GetKeyDown(KeyCode.E) && objetoSeleccionado && ObjetosEnMano() == 0)
            AgarrarObjeto(objetoSeleccionado);

        if (Input.GetKeyDown(KeyCode.G) && ObjetosEnMano() > 0)
            SoltarObjeto();
    }

    // n de objetos en mano 
    int ObjetosEnMano() => SlotMano() ? SlotMano().childCount : 0;

    // slot de mano (usa el puntoDeAgarre si existe)
    Transform SlotMano() => puntoDeAgarre ? puntoDeAgarre : manoJugador;

    // Selección por centro de pantalla + overlay highlight
    void DetectarObjetoPorCapaSinRaycast_ConHighlight()
    {
        if (!camaraJugador) { LimpiarHighlight(); objetoSeleccionado = null; return; }

        var todos = FindObjectsOfType<GameObject>(false);
        int mask = pickableLayers.value == 0 ? ~0 : pickableLayers.value;
        var slot = SlotMano();

        var candidatos = new List<GameObject>();
        foreach (var go in todos)
        {
            if (((1 << go.layer) & mask) == 0 && !go.CompareTag("Food")) continue;
            if (slot && go.transform.IsChildOf(slot)) continue;
            if (go == jugador) continue;



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

    // Aplica overlay unlit sin tocar los materiales
    void AplicarHighlight(GameObject go)
    {
        // No resaltar carne si está en el sartén
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

    // Quitar el overlay y restaura los materiales
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

    // Sartén apuntado (por centro de pantalla y distancia)
    CookMeatInPan DetectarPanApuntado()
    {
        var pans = FindObjectsOfType<CookMeatInPan>(false);
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
        var fryers = FindObjectsOfType<CookFriesInFryer>(false);
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
            if (dPantalla > radioPantallaPan) continue;   // usamos mismo radio que sartén

            float dist = Vector3.Distance(camaraJugador.transform.position, pos);
            if (dist > distanciaPan) continue;            // mismo alcance que sartén

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

    // Carne que lleva en la mano (primer hijo del slot)
    MeatCookingState GetCarneEnMano()
    {
        var slot = SlotMano();
        if (!slot || slot.childCount == 0) return null;
        return slot.GetChild(0).GetComponent<MeatCookingState>();
    }

    // Físicas al agarrar/soltar
    void ConfigurarFisicaObjeto(GameObject objeto, bool enMano)
    {
        var rb = objeto.GetComponent<Rigidbody>();
        if (!rb) return;

        rb.isKinematic = enMano;
        rb.useGravity = !enMano;
        if (!enMano) rb.linearVelocity = Vector3.zero;
    }

    void AgarrarObjeto(GameObject objeto)
    {
        LimpiarHighlight();

        var destino = SlotMano();
        var tabla = objeto.GetComponentInParent<CuttingBoard>();
        if (tabla != null)
        {
            tabla.RemoveIngredient();
        }
        objeto.transform.SetParent(destino);
        objeto.transform.localPosition = Vector3.zero;
        objeto.transform.localRotation = Quaternion.identity;


        ConfigurarFisicaObjeto(objeto, true);

        var meat = objeto.GetComponent<MeatCookingState>();
        if (meat != null)
        {
            meat.LockOnTable(false);
        }

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

        // Suelta delante de la cámara
        objeto.transform.position = camaraJugador.transform.position + camaraJugador.transform.forward * distanciaSoltar;

        var rb = objeto.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Impulso natural al soltar
            Vector3 impulso = camaraJugador.transform.forward * fuerzaLanzamiento + Vector3.up * fuerzaVertical;
            rb.AddForce(impulso, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * torqueLanzamiento, ForceMode.VelocityChange);
        }

        foreach (var c in objeto.GetComponentsInChildren<Collider>(true))
            c.isTrigger = false;

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
        var tablas = FindObjectsOfType<CuttingBoard>(false);
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
            if (dPantalla > radioPantallaPan) continue; // mismo radio del sartén

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
        var mesas = FindObjectsOfType<MesaArmado>(false);
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

        // Intenta obtener Ingredient (para prefabs ya procesados)
        var ing = slot.GetChild(0).GetComponent<Ingredient>();
        if (ing != null) return ing;

        // Si es carne (Meat) devolvemos su Ingredient si existe
        var meat = slot.GetChild(0).GetComponent<MeatCookingState>();
        if (meat != null)
        {
            Ingredient tmp = slot.GetChild(0).gameObject.GetComponent<Ingredient>();
            if (tmp == null)
            {
                // si por alguna razón no tiene Ingredient, le añadimos uno permanente
                tmp = slot.GetChild(0).gameObject.AddComponent<Ingredient>();
                tmp.ingredientName = meat.meatName;
                tmp.requireCookedMeat = true;
            }
            return tmp;
        }

        // Si no es ni Ingredient ni carne, no es válido para mesa de armado
        return null;
    }

    void OnDisable() => LimpiarHighlight();
}
