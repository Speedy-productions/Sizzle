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
        if (progressFill != null)
        {
            progressFill.gameObject.SetActive(on);
            if (on) progressFill.fillAmount = 0f;
        }
    }

    public bool IsOnBoard() => isOnBoard;

    public void Cut()
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning($"[SliceIngredient] No soy el dueño de {name}, no puedo cortar.");
            return;
        }

        Debug.Log($"[SliceIngredient] Cortando ingrediente: {name}");

        // Avisar a la tabla para limpiar referencia
        CuttingBoard cb = GetComponentInParent<CuttingBoard>();
        if (cb != null) cb.RemoveIngredient();

        // Instanciar en red el prefab cortado
        GameObject cortado = PhotonNetwork.Instantiate(
            ingSlicedPrefab.name,  // usa el nombre del prefab registrado en Resources
            transform.position,
            ingSlicedPrefab.transform.rotation
        );

        // Ajustar escala manualmente (Photon no sincroniza localScale)
        cortado.transform.localScale = ingSlicedPrefab.transform.localScale;

        // Si tiene Rigidbody o Collider, asegúrate de mantenerlos correctos
        Rigidbody rb = cortado.GetComponent<Rigidbody>();
        if (rb == null) rb = cortado.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        // Ocultar barra si la tiene
        var sliced = cortado.GetComponent<SliceIngredient>();
        if (sliced != null && sliced.progressFill != null)
            sliced.progressFill.gameObject.SetActive(false);

        // Destruir el original en red
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
