using UnityEngine;

public class CuttingBoard : MonoBehaviour
{
    [Tooltip("Empty transform donde se colocará el ingrediente (hijo de la tabla).")]
    public Transform posicionIngrediente;

    private SliceIngredient currentIngredient;

    public void TryPlaceIngredient(SliceIngredient ingrediente)
    {
        if (ingrediente == null) return;

        currentIngredient = ingrediente;

        // Desactivar fisica antes de mover
        var rb = ingrediente.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Quitar el parent actual para evitar errores de escala
        ingrediente.transform.SetParent(null);

        // Posicionar justo en el punto designado
        ingrediente.transform.position = posicionIngrediente.position;

        // Rotacion fija: -90 grados en el eje X
        ingrediente.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        // Marcar que esta sobre la tabla (activa la barra)
        ingrediente.SetOnBoard(true);

        Debug.Log("Ingrediente colocado en la tabla.");
    }



    // Llamar cuando el ingrediente se quite o se corte
    public void RemoveIngredient()
    {
        if (currentIngredient == null) return;

        // desmarcar y quitar parent
        currentIngredient.SetOnBoard(false);
        // no forzamos reposicionar; lo hará quien lo agarre
        currentIngredient.transform.SetParent(null);
        currentIngredient = null;
    }

    public bool HasIngredient() => currentIngredient != null;
    public SliceIngredient GetIngredient() => currentIngredient;

    // (Opcional) visual hint
    public void ShowAimHint(bool show, bool _) { /* implementar si tienes UI para la tabla */ }
}
