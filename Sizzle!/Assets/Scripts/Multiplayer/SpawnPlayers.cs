using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;

    // Este callback se llama cuando YA estás dentro de una room
    public override void OnJoinedRoom()
    {
        // Si estás en offline mode, no hagas nada (singleplayer)
        if (PhotonNetwork.OfflineMode)
            return;

        // Buscar el player de singleplayer en la escena y destruirlo
        GameObject existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
        {
            Destroy(existingPlayer);   // mejor que SetActive(false) para que no estorbe
        }

        // Spawnear el player de multijugador
        Vector3 randomPosition = new Vector3(
            Random.Range(minX, maxX),
            transform.position.y,
            Random.Range(minZ, maxZ)
        );

        PhotonNetwork.Instantiate(playerPrefab.name, randomPosition, Quaternion.identity);
    }
}
