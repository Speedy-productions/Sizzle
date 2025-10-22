using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Blade : MonoBehaviour
{
    private Camera cam;
    [Tooltip("Cuánto suma por click (0-1)")]
    public float fillPerClick = 0.25f;
    [Tooltip("Velocidad a la que se vacía por segundo")]
    public float decayRate = 0.5f;

    private float progress = 0f;
    private SliceIngredient currentIngredient = null;
    private Image currentFillImage = null;

    void Start()
    {
        StartCoroutine(WaitForLocalCamera());
    }

    IEnumerator WaitForLocalCamera()
    {
        // Espera hasta encontrar una cámara del jugador local (PhotonView.IsMine)
        while (cam == null)
        {
            foreach (var view in FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
            {
                if (view.IsMine)
                {
                    Camera playerCam = view.GetComponentInChildren<Camera>();
                    if (playerCam != null)
                    {
                        cam = playerCam;
                        Debug.Log($"Blade: Cámara del jugador local asignada: {cam.name}");
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(0.2f); // esperar un poco y reintentar
        }
    }

    void Update()
    {
        if (cam == null) return;
        
        // Click: intentar incrementar barra sobre ingrediente en tabla
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
        else
        {
            // Si hay un ingrediente objetivo, la barra se va vaciando con el tiempo
            if (currentIngredient != null && currentFillImage != null)
            {
                if (progress > 0f)
                {
                    progress -= decayRate * Time.deltaTime;
                    progress = Mathf.Clamp01(progress);
                    currentFillImage.fillAmount = progress;

                    // Si ya se vació completamente, ocultamos la UI y olvidamos el objetivo
                    if (progress <= 0f)
                    {
                        currentFillImage.gameObject.SetActive(false);
                        currentIngredient = null;
                        currentFillImage = null;
                    }
                }
            }
        }
    }

    void HandleClick()
    {
        if (cam == null)
        {
            Debug.LogError("Blade: Cámara no asignada al intentar hacer click.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit)) return;

        SliceIngredient hitIng = hit.collider.GetComponent<SliceIngredient>();
        if (hitIng == null) return;

        // Solo se permite la mecánica si el ingrediente está colocado en la tabla
        if (!hitIng.IsOnBoard()) return;

        // Si cambiamos de objetivo, reiniciamos progreso / UI del anterior
        if (currentIngredient != hitIng)
        {
            if (currentFillImage != null)
            {
                currentFillImage.gameObject.SetActive(false);
            }

            currentIngredient = hitIng;
            currentFillImage = hitIng.progressFill;
            if (currentFillImage != null)
            {
                currentFillImage.gameObject.SetActive(true);
                // opcional: mantener el progreso previo del mismo ingrediente si quieres, aquí lo reiniciamos
                progress = currentFillImage.fillAmount;
            }
        }

        // Incrementar progreso por click
        progress += fillPerClick;
        progress = Mathf.Clamp01(progress);

        if (currentFillImage != null)
            currentFillImage.fillAmount = progress;

        // Si se completa, cortar
        if (progress >= 1f && currentIngredient != null)
        {
            currentIngredient.Cut();

            // limpiar UI y estado
            if (currentFillImage != null)
            {
                currentFillImage.fillAmount = 0f;
                currentFillImage.gameObject.SetActive(false);
            }
            progress = 0f;
            currentIngredient = null;
            currentFillImage = null;
        }
    }

    public void SetCamera(Camera newCam)
    {
        cam = newCam;
    }
}
