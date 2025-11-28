using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Canvas))]
public class InteractCanvas : MonoBehaviour
{
    [Header("Cámara que interactúa con el Canvas")]
    public Camera uiCamera; // Si se deja vacío, usa Camera.main

    [Header("Opciones de interacción")]
    public KeyCode interactKey = KeyCode.E;   // Tecla para “click” desde centro
    public bool useMouseClick = true;         // Permite clics normales de mouse

    Canvas canvas;
    GraphicRaycaster raycaster;
    EventSystem eventSystem;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = gameObject.AddComponent<GraphicRaycaster>();

        // Asignar cámara al Canvas World Space
        if (uiCamera == null) uiCamera = Camera.main;
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.worldCamera = uiCamera;
        }

        // Asegurar EventSystem
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>(); // Usa el sistema de entrada clásico
        }
    }

    void Update()
    {
        if (uiCamera == null) return;

        // Interacción desde el centro de la pantalla SOLO con click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            var result = RaycastUIAtScreenCenter();
            if (result != null)
            {
                // Enviar evento PointerClick al elemento UI
                ExecuteEvents.Execute(result.gameObject, new PointerEventData(eventSystem), ExecuteEvents.pointerClickHandler);
            }
        }

        // Resaltar elemento bajo el centro (hover visual)
        var hovered = RaycastUIAtScreenCenter();
        if (hovered != null)
        {
            ExecuteEvents.Execute(hovered.gameObject, new PointerEventData(eventSystem), ExecuteEvents.pointerEnterHandler);
        }
    }

    // Lanza un raycast UI desde el centro de la pantalla
    GameObject RaycastUIAtScreenCenter()
    {
        if (raycaster == null || eventSystem == null) return null;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        var eventData = new PointerEventData(eventSystem) { position = screenCenter };
        var results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        // Devuelve el primer elemento interactuable (Button, Toggle, etc.)
        foreach (var r in results)
        {
            // Filtra por Graphic habilitado y RaycastTarget
            var graphic = r.gameObject.GetComponent<Graphic>();
            if (graphic != null && graphic.raycastTarget)
                return r.gameObject;
        }

        return null;
    }
}
