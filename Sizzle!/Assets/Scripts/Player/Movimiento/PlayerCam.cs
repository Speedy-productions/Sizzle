using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientacion;

    float rotacionX;
    float rotacionY;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Mantiene el mouse en el centro de la pantalla
        Cursor.visible = false; // Oculta el mouse
    }

    private void Update()
    {
        // Input del mouse
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;

        rotacionY += mouseX;

        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -75f, 90f); // Limitaciones en los ejes Y y Z

        transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0);
        orientacion.rotation = Quaternion.Euler(0, rotacionY, 0);
    }
}
