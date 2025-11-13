using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject panelMainMenu;
    public GameObject panelCreateRoom;
    public GameObject panelJoinRoom;
    public GameObject panelRoomLobby;

    [Header("Inputs")]
    public TMP_InputField inputCreateRoom;
    public TMP_InputField inputJoinRoom;

    [Header("Lobby UI")]
    public Transform playerListContainer;
    public GameObject playerListItemPrefab;
    public TMP_Text roomNameText;

    [Header("Buttons")]
    public GameObject btnStartGame;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        ConnectToServer();
    }

    void ConnectToServer()
    {
        Debug.Log("Conectando al servidor Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado a Photon Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Entró al lobby.");
        ShowMainMenu();
    }

    #region UI Navigation
    public void ShowMainMenu()
    {
        panelMainMenu.SetActive(true);
        panelCreateRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
        panelRoomLobby.SetActive(false);
    }

    public void ShowCreateRoomPanel()
    {
        panelMainMenu.SetActive(false);
        panelCreateRoom.SetActive(true);
    }

    public void ShowJoinRoomPanel()
    {
        panelMainMenu.SetActive(false);
        panelJoinRoom.SetActive(true);
    }

    public void BackToMainMenu()
    {
        ShowMainMenu();
    }
    #endregion

    #region Room Management
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(inputCreateRoom.text)) return;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;

        PhotonNetwork.CreateRoom(inputCreateRoom.text, roomOptions);
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(inputJoinRoom.text)) return;
        PhotonNetwork.JoinRoom(inputJoinRoom.text);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Entró a la sala: " + PhotonNetwork.CurrentRoom.Name);
        ShowRoomLobby();
        UpdatePlayerList();
        roomNameText.text = "Sala: " + PhotonNetwork.CurrentRoom.Name;

        btnStartGame.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            item.GetComponentInChildren<TMP_Text>().text = p.NickName;
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("GameScene"); // tu escena de juego
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        ShowMainMenu();
    }

    void ShowRoomLobby()
    {
        panelMainMenu.SetActive(false);
        panelCreateRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
        panelRoomLobby.SetActive(true);
    }
    #endregion
}
