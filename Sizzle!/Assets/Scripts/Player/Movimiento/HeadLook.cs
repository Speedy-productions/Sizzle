using UnityEngine;

public class HeadLook : MonoBehaviour
{
    public Transform headBone;
    public Transform cameraTransform;
    public float maxUp = 60f;
    public float maxDown = 45f;
    public float smoothSpeed = 8f;

    Quaternion headInitialLocalRot;
    float initialCameraPitch;

    void Start()
    {
        if (headBone == null || cameraTransform == null)
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
