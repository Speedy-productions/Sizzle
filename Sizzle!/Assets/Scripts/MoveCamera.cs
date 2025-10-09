using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPos;


    private void LateUpdate()
    {
        transform.position = cameraPos.position;
    }
}
