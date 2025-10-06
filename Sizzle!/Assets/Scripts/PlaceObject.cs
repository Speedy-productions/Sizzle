using UnityEngine;

public class PlaceObject : MonoBehaviour
{
    [SerializeField] private Transform boardCenter;
    [SerializeField] private KeyCode cutKey = KeyCode.T;

    [SerializeField] private GameObject ingredient;

    [SerializeField] private GameObject ingredientInHand;
    [SerializeField] private bool isNearBoard = false;

    private void Start()
    {
        
    }

    private void Update()
    {
        if (isNearBoard && ingredientInHand != null && Input.GetKeyDown(cutKey))
        {
            StartCutting();
        }
    }

    private void StartCutting()
    {
        ingredientInHand.transform.position = boardCenter.position;
        ingredientInHand.transform.rotation = boardCenter.rotation;

        ingredientInHand.transform.parent = boardCenter;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isNearBoard = true;
        }

        if (other.CompareTag("Ingredient") && other.transform.parent != null && other.transform.parent.CompareTag("PlayerHand"))
        {
            ingredientInHand = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isNearBoard = false;
        }

        if (other.CompareTag("Ingredient"))
        {
            ingredientInHand = null;
        }
    }
}
