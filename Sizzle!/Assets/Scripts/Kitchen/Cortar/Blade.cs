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

    [SerializeField] private CuttingSound cuttingSound;

    private float progress = 0f;
    private SliceIngredient currentIngredient = null;
    private Image currentFillImage = null;

    void Start()
    {
        StartCoroutine(WaitForLocalCamera());
    }

    IEnumerator WaitForLocalCamera()
    {
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
            yield return new WaitForSeconds(0.2f);
        }
    }

    void Update()
    {
        if (cam == null) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Q))
        {
            HandleClick();
        }
        else
        {
            if (currentIngredient != null && currentFillImage != null)
            {
                if (progress > 0f)
                {
                    progress -= decayRate * Time.deltaTime;
                    progress = Mathf.Clamp01(progress);
                    currentFillImage.fillAmount = progress;

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
            Debug.LogError("Blade: Cámara no asignada.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        SliceIngredient hitIng = hit.collider.GetComponent<SliceIngredient>();
        if (hitIng == null) return;
        if (!hitIng.IsOnBoard()) return;

        // =======================================
        // ?? FIX MULTIJUGADOR: pedir ownership
        // =======================================
        PhotonView pv = hitIng.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            pv.RequestOwnership();

        if (cuttingSound != null)
            cuttingSound.PlayCutSound();

        if (currentIngredient != hitIng)
        {
            if (currentFillImage != null)
                currentFillImage.gameObject.SetActive(false);

            currentIngredient = hitIng;
            currentFillImage = hitIng.progressFill;

            if (currentFillImage != null)
            {
                currentFillImage.gameObject.SetActive(true);
                progress = currentFillImage.fillAmount;
            }
        }

        progress += fillPerClick;
        progress = Mathf.Clamp01(progress);

        if (currentFillImage != null)
            currentFillImage.fillAmount = progress;

        if (progress >= 1f && currentIngredient != null)
        {
            currentIngredient.Cut();

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
