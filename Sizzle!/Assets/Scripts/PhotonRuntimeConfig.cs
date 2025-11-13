using UnityEngine;
using Photon.Pun;

public class PhotonRuntimeConfig : MonoBehaviour
{
    void Awake()
    {
        // tasas razonables para gameplay
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 15;
        Application.runInBackground = true;
    }
}
