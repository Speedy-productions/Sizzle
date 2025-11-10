using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.CreateRoom("OfflineRoom");
            Debug.Log("Modo aventura");
        }
        SceneManager.LoadScene(sceneName);
    }
}
