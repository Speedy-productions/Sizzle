using UnityEngine;
using Photon.Pun;

public class HeadLook : MonoBehaviour
{
    public Transform headBone;
    public Transform cameraTransform;
    public float maxUp = 60f;
    public float maxDown = 45f;
    public float smoothSpeed = 8f;

    Quaternion headInitialLocalRot;
    float initialCameraPitch;
    private PhotonView view;

    void Start()
    {
        view = GetComponentInParent<PhotonView>();

        if (view != null && !view.IsMine) // Solo habilitar para el jugador local
        {
            enabled = false;
            return;
        }

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInParent<Camera>();
            if (cam == null)
            {
                cam = GetComponentInChildren<Camera>();
            }
            if (cam != null)
            {
                cameraTransform = cam.transform;
                Debug.Log($"HeadLook: cámara asignada automáticamente: {cameraTransform.name}");
            }
            else
            {
                Debug.LogWarning("HeadLook: no se encontró la cámara del jugador local.");
                enabled = false;
                return;
            }
        }

        if (headBone == null)
        {
            Debug.LogWarning("HeadLook: asigna headBone y cameraTransform en el Inspector.");
            enabled = false;
            return;
        }

        headInitialLocalRot = headBone.localRotation;
        initialCameraPitch = NormalizeAngle(cameraTransform.eulerAngles.x);
    }

    void LateUpdate() // sobreescribir la animacion que actualiza el hueso antes
    {
        float camPitch = NormalizeAngle(cameraTransform.eulerAngles.x);
        float delta = camPitch - initialCameraPitch;
        // Limitar los angulos en los que puede mirar el personaje
        delta = Mathf.Clamp(delta, -maxDown, maxUp);

        Quaternion targetLocal = headInitialLocalRot * Quaternion.Euler(delta, 0f, 0f);
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetLocal, Time.deltaTime * smoothSpeed);
    }

    float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }

}
