using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class Blade : MonoBehaviour
{
    private Camera cam;

    [Tooltip("Cuánto suma por click (valor base)")]
    [SerializeField] private float baseFillPerClick = 0.25f;

    [Tooltip("Velocidad a la que se vacía por segundo")]
    public float decayRate = 0.5f;

    [SerializeField] private CuttingSound cuttingSound;

    // Valor efectivo que se usa en runtime. Lo serializamos para que lo veas en Inspector.
    [SerializeField] private float syncedFillPerClick = 0.25f;

    private float progress = 0f;
    private SliceIngredient currentIngredient = null;
    private Image currentFillImage = null;

    PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
        // Inicializar con el valor base
        syncedFillPerClick = baseFillPerClick;
        Debug.Log($"Blade.Awake - baseFillPerClick={baseFillPerClick} syncedFillPerClick inicial={syncedFillPerClick}");
    }

    void Start()
    {
        // Aplicar mejora desde UpgradeManager al iniciar (si corresponde)
        ApplyCuttingUpgrade();

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

        // Nota: ya aplicamos ApplyCuttingUpgrade() en Start y lo llamamos desde la UI cuando se compra.
        // Si quieres forzar actualización cada click, descomenta la línea siguiente:
        // ApplyCuttingUpgrade();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        SliceIngredient hitIng = hit.collider.GetComponent<SliceIngredient>();
        if (hitIng == null) return;
        if (!hitIng.IsOnBoard()) return;

        // FIX MULTIJUGADOR: pedir ownership
        PhotonView hitPv = hitIng.GetComponent<PhotonView>();
        if (hitPv != null && !hitPv.IsMine)
            hitPv.RequestOwnership();

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

        // Usar el valor sincronizado / aplicado
        progress += syncedFillPerClick;
        progress = Mathf.Clamp01(progress);

        Debug.Log($"Blade.HandleClick - Añadido {syncedFillPerClick} progreso -> progress={progress} (cutLevel={UpgradeManager.Instance?.cutLevel ?? -1})");

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

    // Hacemos público ApplyCuttingUpgrade para poder invocarlo desde la UI después de comprar
    public void ApplyCuttingUpgrade()
    {
        if (UpgradeManager.Instance == null) return;

        int level = UpgradeManager.Instance.cutLevel;
        float newValue = baseFillPerClick;

        switch (level)
        {
            case 0: newValue = baseFillPerClick; break;
            case 1: newValue = baseFillPerClick * 1.50f; break;
            case 2: newValue = baseFillPerClick * 2.0f; break;
            case 3: newValue = baseFillPerClick * 2.50f; break;
        }

        float old = syncedFillPerClick;
        syncedFillPerClick = newValue;

        Debug.Log($"Blade.ApplyCuttingUpgrade -> cutLevel={level} old={old} new={syncedFillPerClick}");

        // OPCIONAL: Si quieres que los demás vean tu velocidad (no necesario para que el asset cortado aparezca),
        // y SOLO si este Blade pertenece al jugador local (pv.IsMine), podrías mandar un RPC para informar a los demás.
        // Pero NO es necesario porque SliceIngredient.Cut() ya sincroniza el resultado.
    }

    public void SetCamera(Camera newCam)
    {
        cam = newCam;
    }
}
