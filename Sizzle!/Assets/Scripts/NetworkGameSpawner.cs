using Photon.Pun;
using UnityEngine;

public class NetworkGameSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn")]
    public Transform[] spawnPoints;
    public string playerPrefabName = "PlayerNetwork"; // asegúrate de tener un prefab con este nombre en Resources/

    public override void OnJoinedRoom()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] No hay spawnPoints. Usando (0,0,0).");
            PhotonNetwork.Instantiate(playerPrefabName, Vector3.zero, Quaternion.identity);
            return;
        }

        var p = spawnPoints[Random.Range(0, spawnPoints.Length)];
        PhotonNetwork.Instantiate(playerPrefabName, p.position, p.rotation);
    }
}
