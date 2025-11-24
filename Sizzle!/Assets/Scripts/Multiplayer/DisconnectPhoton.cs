using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;

public class DisconnectPhoton : MonoBehaviour
{
    public string menuScene = "Menus";

    public void GoBackToMenu()
    {
        StartCoroutine(HandleReturn());
    }

    IEnumerator HandleReturn()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();

            while (PhotonNetwork.IsConnected)
                yield return null;

            PhotonNetwork.OfflineMode = false;
        }

        SceneManager.LoadScene(menuScene);
    }
}