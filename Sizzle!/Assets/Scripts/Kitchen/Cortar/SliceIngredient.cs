using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class SliceIngredient : MonoBehaviourPun
{
    public GameObject ingSlicedPrefab;
    public Image progressFill;
    public string ingredientName;

    bool isOnBoard = false;

    void Start()
    {
        if (progressFill != null)
            progressFill.gameObject.SetActive(false);
    }

    public void SetOnBoard(bool on)
    {
        isOnBoard = on;

        // ===============================
        // ?? FIX: UI SOLO PARA EL DUEÑO
        // ===============================
        if (progressFill != null)
        {
            if (photonView.IsMine)
            {
                progressFill.gameObject.SetActive(on);
                if (on) progressFill.fillAmount = 0f;
            }
            else
            {
                progressFill.gameObject.SetActive(false);
            }
        }
    }

    public bool IsOnBoard() => isOnBoard;

    public void Cut()
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning($"[SliceIngredient] No soy dueño de {name}, no corto.");
            return;
        }

        Debug.Log($"[SliceIngredient] Cortando {name}");

        CuttingBoard cb = GetComponentInParent<CuttingBoard>();
        if (cb != null) cb.RemoveIngredient();

        GameObject cortado = PhotonNetwork.Instantiate(
            ingSlicedPrefab.name,
            transform.position,
            ingSlicedPrefab.transform.rotation
        );

        // ======================================
        // ?? FIX: Ownership del nuevo objeto
        // ======================================
        PhotonView pvNew = cortado.GetComponent<PhotonView>();
        if (pvNew != null)
            pvNew.RequestOwnership();

        cortado.transform.localScale = ingSlicedPrefab.transform.localScale;

        Rigidbody rb = cortado.GetComponent<Rigidbody>();
        if (rb == null) rb = cortado.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        var sliced = cortado.GetComponent<SliceIngredient>();
        if (sliced != null && sliced.progressFill != null)
            sliced.progressFill.gameObject.SetActive(false);

        PhotonNetwork.Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Blade"))
        {
            Cut();
        }
    }

    
}
