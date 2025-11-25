using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Voice.PUN;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        if (PhotonNetwork.OfflineMode) Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void LoadMenu()
    {
        Debug.Log("Loading Menu...");
        if (PhotonNetwork.IsConnected)
        {
            if (PunVoiceClient.Instance != null)
            {
                if (PunVoiceClient.Instance.Client.IsConnected)
                {
                    GameObject vozGO = PunVoiceClient.Instance.gameObject;
                    PunVoiceClient.Instance.Disconnect();
                    Destroy(vozGO);
                }
            }

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            PhotonNetwork.Disconnect();
            SceneManager.LoadScene("Menus");
        }else
        {
            SceneManager.LoadScene("Menus");
        }


    }

    private void OnDisable()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;
    }

}
