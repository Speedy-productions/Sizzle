using UnityEngine;

public class Ingredient : MonoBehaviour
{
    public GameObject ingSlicedPrefab;
    
    private void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Blade")
        {
            Debug.Log("Ingrediente cortado");
            Instantiate(ingSlicedPrefab);
            Destroy(gameObject);
        }
    }
}
