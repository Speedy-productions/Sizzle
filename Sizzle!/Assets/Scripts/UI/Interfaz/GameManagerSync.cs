using UnityEngine;
using Photon.Pun;

public class GameManagerTiempo : MonoBehaviourPun, IPunObservable
{
    public static GameManagerTiempo Instance;

    [Header("Tiempo inicial (segundos)")]
    public float tiempoInicial = 180f;

    [HideInInspector] public float tiempoRestante;

    [HideInInspector] public bool corriendo = true;

    // === NUEVO: Dinero sincronizado ===
    [Header("Dinero inicial")]
    public int dineroActual = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        tiempoRestante = tiempoInicial;
    }

    void Start()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            corriendo = true;   // host o singleplayer sí arranca el timer
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode)
        {
            // Modo singleplayer → funciona igual que antes
            if (corriendo)
            {
                tiempoRestante -= Time.deltaTime;
                if (tiempoRestante <= 0)
                {
                    tiempoRestante = 0;
                    corriendo = false;
                }
            }
            return;
        }

        // Solo el MASTER controla el tiempo
        if (!PhotonNetwork.IsMasterClient) return;

        if (corriendo)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0)
            {
                tiempoRestante = 0;
                corriendo = false;
            }
        }
    }

    // ==========================
    //   📡 Sincronización Photon
    // ==========================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // MASTER ENVÍA
        {
            stream.SendNext(tiempoRestante);
            stream.SendNext(corriendo);
            stream.SendNext(dineroActual); // NUEVO: sincroniza dinero
        }
        else // CLIENTE RECIBE
        {
            tiempoRestante = (float)stream.ReceiveNext();
            corriendo = (bool)stream.ReceiveNext();
            dineroActual = (int)stream.ReceiveNext(); // NUEVO: recibe dinero
        }
    }

    // === NUEVO: Métodos para modificar el dinero de forma centralizada ===
    public void AgregarDinero(int cantidad)
    {
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            dineroActual += cantidad;
        }
    }

    public void QuitarDinero(int cantidad)
    {
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            dineroActual = Mathf.Max(0, dineroActual - cantidad);
        }
    }
}
