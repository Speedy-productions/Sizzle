using UnityEngine;
using Photon.Pun;

public class PlayerCam : MonoBehaviour
{
    PhotonView pv;

    public float sensX;
    public float sensY;

    public Transform orientacion;

    float rotacionX;
    float rotacionY;

    private void Awake()
    {
        pv = GetComponentInParent<PhotonView>();
    }

    private void Start()
    {
        if (!pv.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }

        BloquearCursor();
    }

    private void Update()
    {
        if (!pv.IsMine) return;

        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;

        rotacionY += mouseX;
        rotacionX -= mouseY;

        rotacionX = Mathf.Clamp(rotacionX, -75f, 90f);

        transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0);
        orientacion.rotation = Quaternion.Euler(0, rotacionY, 0);
    }

    public void BloquearCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DesbloquearCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
