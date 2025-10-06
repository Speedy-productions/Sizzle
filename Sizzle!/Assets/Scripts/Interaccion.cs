using UnityEngine;
using UnityEngine.UI;

public class Interaccion : MonoBehaviour
{
    [Header("Configuración general")]
    public Transform manoJugador;
    public GameObject jugador;
    public Camera camaraJugador;

    [Header("Interfaz")]
    public Image crosshair; // Asigna aquí la imagen o texto del punto/cross

    [Header("Distancias")]
    public float distanciaInteraccion = 2f; // Distancia máxima para agarrar objetos
    public float distanciaSoltar = 1.2f;    // Distancia enfrente de la cámara al soltar

    [Header("Capas")]
    public LayerMask whatIsGround; // Asigna aquí el layer del suelo/mesas

    private GameObject objetoSeleccionado;
    private int cantidadDeObjetosMano;

    void Update()
    {
        cantidadDeObjetosMano = manoJugador.childCount;

        DetectarObjetoFrente();

        // Agarrar
        if (Input.GetKeyDown(KeyCode.E) && objetoSeleccionado != null && cantidadDeObjetosMano == 0)
        {
            AgarrarObjeto(objetoSeleccionado);
        }

        // Soltar
        if (Input.GetKeyDown(KeyCode.G) && cantidadDeObjetosMano > 0)
        {
            SoltarObjeto();
        }
    }

    private void DetectarObjetoFrente()
    {
        Ray ray = camaraJugador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Centro de pantalla
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distanciaInteraccion))
        {
            GameObject objeto = hit.collider.gameObject;

            // Si el objeto pertenece al suelo o a lo que sea "Ground", lo ignoramos
            if (((1 << objeto.layer) & whatIsGround) != 0)
            {
                crosshair.color = Color.white;
                objetoSeleccionado = null;
                return;
            }

            // Si es un objeto válido para agarrar
            crosshair.color = Color.green;
            objetoSeleccionado = objeto;
        }
        else
        {
            crosshair.color = Color.white;
            objetoSeleccionado = null;
        }
    }

    private void AgarrarObjeto(GameObject objeto)
    {
        Debug.Log("Agarrando objeto");

        objeto.transform.SetParent(manoJugador);
        objeto.transform.localPosition = Vector3.zero;
        objeto.transform.localRotation = Quaternion.identity;

        Rigidbody rb = objeto.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = objeto.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Collider playerCollider = jugador.GetComponent<Collider>();
        if (playerCollider != null && col != null)
            Physics.IgnoreCollision(playerCollider, col, true);

        Debug.Log("Objeto tomado");
    }

    private void SoltarObjeto()
    {
        if (manoJugador.childCount == 0) return;

        GameObject objeto = manoJugador.GetChild(0).gameObject;
        objeto.transform.SetParent(null);

        // Soltar enfrente de la cámara
        objeto.transform.position = camaraJugador.transform.position + camaraJugador.transform.forward * distanciaSoltar;

        Rigidbody rb = objeto.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }

        Collider col = objeto.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Collider playerCollider = jugador.GetComponent<Collider>();
        if (playerCollider != null && col != null)
            Physics.IgnoreCollision(playerCollider, col, false);

        Debug.Log("Objeto soltado");
    }
}
