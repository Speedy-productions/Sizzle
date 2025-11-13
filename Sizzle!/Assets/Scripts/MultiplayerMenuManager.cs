using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class MultiplayerMenuManager : MonoBehaviourPunCallbacks
{
    [Header("Pantallas")]
    public GameObject multiplayerPanel;
    public GameObject crearPanel;
    public GameObject buscarPanel;

    [Header("Inputs")]
    public TMP_InputField inputCrear;
    public TMP_InputField inputBuscar;

    [Header("Botones")]
    public Button botonCrearSala;
    public Button botonUnirseSala;

    private bool estaListo = false;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Jugador" + Random.Range(1000, 9999);
        ConectarAServidor();

        // Desactivar botones al inicio
        if (botonCrearSala) botonCrearSala.interactable = false;
        if (botonUnirseSala) botonUnirseSala.interactable = false;
    }

    void ConectarAServidor()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Conectando a Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado a Master Server, uniéndose al lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Listo: conectado al lobby de Photon");
        estaListo = true;

        if (botonCrearSala) botonCrearSala.interactable = true;
        if (botonUnirseSala) botonUnirseSala.interactable = true;

        MostrarMultiplayer();
    }

    // ========= UI =========
    public void MostrarMultiplayer()
    {
        multiplayerPanel.SetActive(true);
        crearPanel.SetActive(false);
        buscarPanel.SetActive(false);
    }

    public void MostrarCrear()
    {
        multiplayerPanel.SetActive(false);
        crearPanel.SetActive(true);
        buscarPanel.SetActive(false);
    }

    public void MostrarBuscar()
    {
        multiplayerPanel.SetActive(false);
        crearPanel.SetActive(false);
        buscarPanel.SetActive(true);
    }

    public void Regresar()
    {
        MostrarMultiplayer();
    }

    // ========= Salas =========
    public void CrearSala()
    {
        if (!estaListo)
        {
            Debug.LogWarning("Photon aún no está listo para crear sala.");
            return;
        }

        if (string.IsNullOrEmpty(inputCrear.text))
        {
            Debug.LogWarning("El nombre de la sala está vacío");
            return;
        }

        RoomOptions opciones = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(inputCrear.text, opciones);
    }

    public void UnirseSala()
    {
        if (!estaListo)
        {
            Debug.LogWarning("Photon aún no está listo para unirse a una sala.");
            return;
        }

        if (string.IsNullOrEmpty(inputBuscar.text))
        {
            Debug.LogWarning("El nombre de la sala está vacío");
            return;
        }

        PhotonNetwork.JoinRoom(inputBuscar.text);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Entró a la sala: {PhotonNetwork.CurrentRoom.Name}");
        PhotonNetwork.LoadLevel("v0.2"); // tu escena real
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Error al crear sala: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Error al unirse a sala: {message}");
    }
}
