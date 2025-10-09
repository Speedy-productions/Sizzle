using UnityEngine;
using UnityEngine.UI;

public class Ingredient : MonoBehaviour
{

    public GameObject ingSlicedPrefab;
    [Tooltip("Image (tipo Filled) que sirve como barra situada en el prefab (World Space Canvas). Asignar en el prefab).")]
    public Image progressFill;

    // indica si el ingrediente está actualmente en la tabla
    bool isOnBoard = false;

    void Start()
    {
        if (progressFill != null)
            progressFill.gameObject.SetActive(false);
    }

    // llamado por la tabla cuando se coloca o se quita
    public void SetOnBoard(bool on)
    {
        isOnBoard = on;

        if (progressFill != null)
        {
            // Activar el canvas padre, no solo la imagen
            if (progressFill.transform.parent != null)
                progressFill.transform.parent.gameObject.SetActive(on);

            progressFill.gameObject.SetActive(on);
            if (on) progressFill.fillAmount = 0f;
        }
    }


    public bool IsOnBoard() => isOnBoard;

    // orienta el objeto hacia el jugador (manteniendo upright)
    public void FacePlayer(Transform playerCam)
    {
        if (playerCam == null) return;
        Vector3 dir = playerCam.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    // método público para cortar (puede llamarse desde Blade)
    public void Cut()
    {
        Debug.Log("Ingrediente cortado");

        // avisar a la tabla (si existe) para que limpie su referencia
        CuttingBoard cb = GetComponentInParent<CuttingBoard>();
        if (cb != null) cb.RemoveIngredient();

        // instanciar prefab cortado en misma posición y rotación
        GameObject cortado = Instantiate(ingSlicedPrefab, transform.position, transform.rotation);

        // asegurar que el prefab sea interactuable: collider + rigidbody + capa/tag
        if (cortado.GetComponent<Collider>() == null)
        {
            // agregar box collider por seguridad si no tiene ninguno
            cortado.AddComponent<BoxCollider>();
        }

        Rigidbody rb = cortado.GetComponent<Rigidbody>();
        if (rb == null) rb = cortado.AddComponent<Rigidbody>();

        // heredar la capa del original (para que siga siendo 'pickable')
        cortado.layer = gameObject.layer;
        cortado.tag = gameObject.tag; // opcional: mantener el tag

        // Si el prefab tiene Ingredient (para poder volver a cortarlo o mostrar UI), asegurarnos que su barra está oculta
        Ingredient newIng = cortado.GetComponent<Ingredient>();
        if (newIng != null && newIng.progressFill != null)
            newIng.progressFill.gameObject.SetActive(false);

        Destroy(gameObject);
    }

    // si quieres mantener la detección por colisión también:
    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Blade"))
        {
            Cut();
        }
    }
}
