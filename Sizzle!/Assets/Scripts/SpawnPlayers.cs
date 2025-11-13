using UnityEngine;
using Photon.Pun;

public class MultiplayerSpawn : MonoBehaviour
{
    public string playerPrefabName = "Player";
    public Transform[] spawnPoints;

    private void Start()
    {
        int index = Random.Range(0, spawnPoints.Length);

        PhotonNetwork.Instantiate(
            playerPrefabName,
            spawnPoints[index].position,
            spawnPoints[index].rotation
        );
    }
}
