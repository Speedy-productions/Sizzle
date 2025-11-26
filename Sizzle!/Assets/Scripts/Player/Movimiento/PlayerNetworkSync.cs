using UnityEngine;
using Photon.Pun;

public class PlayerNetworkSync : MonoBehaviourPun, IPunObservable
{
    Rigidbody rb;

    Vector3 netPos;
    Quaternion netRot;
    Quaternion netModelRot;

    public Transform playerModel;

    bool firstSync = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // POSICIÓN LOCAL
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(playerModel.rotation);
        }
        else
        {
            // POSICIÓN REMOTA RECIBIDA
            netPos = (Vector3)stream.ReceiveNext();
            netRot = (Quaternion)stream.ReceiveNext();
            netModelRot = (Quaternion)stream.ReceiveNext();

            if (firstSync)
            {
                rb.position = netPos;
                rb.rotation = netRot;
                playerModel.rotation = netModelRot;
                firstSync = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (photonView.IsMine) return;  // local no sincroniza aquí

        // INTERPOLACIÓN SUAVE
        rb.MovePosition(Vector3.Lerp(rb.position, netPos, Time.fixedDeltaTime * 12f));
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, netRot, Time.fixedDeltaTime * 12f));

        playerModel.rotation = Quaternion.Slerp(
            playerModel.rotation,
            netModelRot,
            Time.fixedDeltaTime * 12f
        );
    }
}
