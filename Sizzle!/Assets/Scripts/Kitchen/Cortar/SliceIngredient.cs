using UnityEngine;
using UnityEngine.UI;

public class SliceIngredient : MonoBehaviour
{
    public GameObject ingSlicedPrefab;
    [Tooltip("Image (tipo Filled) que sirve como barra situada en el prefab (World Space Canvas). Asignar en el prefab).")]
    public Image progressFill;
    public string ingredientName;

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
        GameObject cortado = Instantiate(
            ingSlicedPrefab,
            transform.position,
            ingSlicedPrefab.transform.rotation 
        );

        cortado.transform.localScale = ingSlicedPrefab.transform.localScale;

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
        cortado.tag = gameObject.tag;

        // si el prefab cortado trae FriesCookingState, asegurar estado crudo y fuera de freidora
        var fries = cortado.GetComponent<FriesCookingState>();
        if (fries != null)
        {
            fries.ResetFries();
            fries.SetInFryer(false);
        }

        // Si el prefab tiene Ingredient (para poder volver a cortarlo o mostrar UI), asegurarnos que su barra está oculta
        SliceIngredient newIng = cortado.GetComponent<SliceIngredient>();
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
