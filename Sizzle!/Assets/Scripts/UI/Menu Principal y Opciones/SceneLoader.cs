using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SceneLoader : MonoBehaviourPunCallbacks
{
    [Header("Red")]
    [SerializeField] private string gameVersion = "1.0.0";
    [SerializeField] private byte maxPlayersPerRoom = 6;
    [SerializeField] private string roomName = "KitchenRoom";

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        // 🔥 SI NO ES MULTIJUGADOR → NO CONECTAR A PHOTON
        if (!GameMode.Multiplayer)
        {
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.Disconnect();
            return;
        }

        // 🔥 SI ES MULTIJUGADOR → CONECTAR COMO SIEMPRE
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.OfflineMode = false;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (!GameMode.Multiplayer) return;

        var options = new RoomOptions { MaxPlayers = maxPlayersPerRoom };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (!GameMode.Multiplayer) return;

        var options = new RoomOptions { MaxPlayers = maxPlayersPerRoom };
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    public void LoadScene(string sceneName)
    {
        if (GameMode.Multiplayer && PhotonNetwork.InRoom)
            PhotonNetwork.LoadLevel(sceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
