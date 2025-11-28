// WaitSpotRegistry.cs
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class WaitSpotRegistry : MonoBehaviourPunCallbacks
{
    public static WaitSpotRegistry Instance;

    [Tooltip("Puntos donde los NPCs esperan su turno (índice 0 puede ser 'la caja').")]
    public Transform[] waitSpots;

    const string PROP = "WS_MASK"; // bitmask de ocupación
    int localMask = 0;             // copia local cacheada

    void Awake()
    {
        Instance = this;
    }

    public static int Count => Instance ? Instance.waitSpots.Length : 0;
    public static Transform Get(int i) =>
        (Instance && i >= 0 && i < Instance.waitSpots.Length) ? Instance.waitSpots[i] : null;

    public static int GetMask()
    {
        if (!PhotonNetwork.InRoom) return Instance?.localMask ?? 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PROP, out object v) && v is int m)
            return m;
        return 0;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null &&
            propertiesThatChanged.TryGetValue(PROP, out object v) && v is int m)
        {
            localMask = m;
        }
    }

    // ---------------- MASTER ONLY ----------------

    public static int MasterTryReserveAny()
    {
        if (!PhotonNetwork.IsMasterClient || !Instance) return -1;
        int mask = GetMask();
        for (int i = 0; i < Count; i++)
        {
            if ((mask & (1 << i)) == 0)
                return MasterSet(i, occupied: true) ? i : -1;
        }
        return -1;
    }

    public static bool MasterRelease(int index)
    {
        if (!PhotonNetwork.IsMasterClient || !Instance) return false;
        return MasterSet(index, occupied: false);
    }

    static bool MasterSet(int index, bool occupied)
    {
        if (index < 0 || index >= Count) return false;

        int mask = GetMask();
        if (occupied) mask |=  (1 << index);
        else          mask &= ~(1 << index);

        var ht = new Hashtable { [PROP] = mask };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        Instance.localMask = mask;
        return true;
    }
}
