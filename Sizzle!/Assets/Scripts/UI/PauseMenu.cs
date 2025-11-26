using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Voice.PUN;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;

void Start()
{
    if (PhotonNetwork.OfflineMode)
    {
        var voice = FindFirstObjectByType<PunVoiceClient>();

        if (voice != null)
        {
            if (voice.Client != null && voice.Client.IsConnected)
                voice.Disconnect();

            Destroy(voice.gameObject);
        }
    }
}

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

    var voice = FindFirstObjectByType<PunVoiceClient>();
    if (voice != null)
    {
        if (voice.Client != null && voice.Client.IsConnected)
        {
            voice.Disconnect();
        }

        Destroy(voice.gameObject);
    }

    if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
    {
        PhotonNetwork.LeaveRoom();
    }

    if (PhotonNetwork.IsConnected)
    {
        PhotonNetwork.Disconnect();
    }

    SceneManager.LoadScene("Menus");
}


    private void OnDisable()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;
    }

}
