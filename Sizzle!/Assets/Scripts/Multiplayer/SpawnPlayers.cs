using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject playerPrefab;

    // Valores fijos de spawn
    // (Usando el mínimo y máximo que diste: X: 226–264, Z: 126–147)
    private const float minX = 180f;
    private const float maxX = 230f;
    private const float minZ = 126f;
    private const float maxZ = 150f;

    private void Start()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
        {
            // Desactivar el jugador de singleplayer
            GameObject existingPlayer = GameObject.Find("PlayerSingle");
            if (existingPlayer)
            {
                existingPlayer.SetActive(false);
            }

            // Posición aleatoria dentro del rango fijo
            Vector3 randomPosition = new Vector3(
                Random.Range(minX, maxX),
                transform.position.y,
                Random.Range(minZ, maxZ)
            );

            PhotonNetwork.Instantiate(playerPrefab.name, randomPosition, Quaternion.identity);
        }
    }
}
