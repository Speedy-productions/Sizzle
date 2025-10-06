using UnityEngine;
using UnityEngine.UI;

public class Blade : MonoBehaviour
{
    [Header("Cut Settings")]
    public Camera cam; // camara que usaremos para el raycast
    public float fillPerClick = 0.25f; // cuanto se llena por click
    public float decayRate = 1.25f; // velocidad en la que se vacia
    public Image progressBar; // barra de progreso

    private float progress = 0f;
    private Ingredient currentIngredient;

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (progressBar != null) 
        {
            progressBar.fillAmount = 0f;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        // Si no se llena, se vacia con el tiempo
        if (progress > 0)
        {
            progress -= decayRate * Time.deltaTime; // Disminuye el progreso
            progress = Mathf.Clamp01(progress); // Calcula el progreso con valores entre 0 y 1

            if (progressBar != null)
            {
                progressBar.fillAmount = progress; //Actualiza la barra
            }
        }
    }

    void HandleClick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Ingredient ingredient = hit.collider.GetComponent<Ingredient>();

            if (ingredient != null)
            {
                currentIngredient = ingredient;
                progress += fillPerClick; // Llena la barra
                progress = Mathf.Clamp01(progress);

                if (progressBar != null)
                {
                    progressBar.fillAmount = progress; // Actualiza la barra
                }
                if (progress >= 1f) // si el progreso llega a 1, se corta e ingrediente
                {
                    CutIngredient();
                }
            }
        }
    }

    void CutIngredient()
    {
        if (currentIngredient != null)
        {
            Debug.Log("Ingrediente cortado");
            // Cambia el ingrediente por su version cortada
            Instantiate(currentIngredient.ingSlicedPrefab, currentIngredient.transform.position, currentIngredient.transform.rotation);
            // Destruye el ingrediente original
            Destroy(currentIngredient.gameObject);

            // Reiniciar barra
            progress = 0f;
            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }
            currentIngredient = null;

        }
    }
}
