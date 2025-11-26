using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;

public class DestroyVoiceManagerOnSingleplayer : MonoBehaviour
{
    void Start()
    {
        if (PhotonNetwork.OfflineMode)
        {
            if (PunVoiceClient.Instance != null)
            {
                Debug.Log("Eliminando VoiceManager en Singleplayer...");

                // ✅ SOLO desconectar si realmente está conectado
                if (PunVoiceClient.Instance.Client != null &&
                    PunVoiceClient.Instance.Client.IsConnected)
                {
                    PunVoiceClient.Instance.Disconnect();
                }

                // ✅ Siempre destruir el GameObject igualmente
                Destroy(PunVoiceClient.Instance.gameObject);
            }
        }
    }
}
