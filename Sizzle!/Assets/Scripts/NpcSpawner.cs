using Photon.Pun;
using UnityEngine;

public class NpcSpawner : MonoBehaviourPun
{
    public static NpcSpawner Instance { get; private set; }

    public string npcPrefabName = "NPC";   // en Resources
    public Transform spawnPoint;           // un solo punto
    public float spawnInterval = 3f;
    public int maxAlive = 6;

    float t;
    int alive;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        t += Time.deltaTime;
        if (t >= spawnInterval && alive < maxAlive)
        {
            t = 0f;

            int waitIndex = WaitSpotRegistry.MasterTryReserveAny(); // <- RESERVA
            if (waitIndex < 0) return; // no hay hueco

            object[] data = new object[] { waitIndex };

            PhotonNetwork.Instantiate(
                npcPrefabName,
                spawnPoint ? spawnPoint.position : Vector3.zero,
                spawnPoint ? spawnPoint.rotation : Quaternion.identity,
                0,
                data
            );

            alive++;
        }
    }

    // Llama esto cuando un NPC termina su vida (despues de happy path o destroy)
    public void NotifyNpcDespawn()
    {
        alive = Mathf.Max(0, alive - 1);
    }
}
