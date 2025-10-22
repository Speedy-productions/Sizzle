using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    
    public GameObject playerPrefab;


    public float mixX;
    public float maxX;
    public float minZ;
    public float maxZ;

    private void Start()
    {
        Vector3 randomPosition = new Vector3(Random.Range(mixX, maxX), transform.position.y, Random.Range(minZ, maxZ));
        PhotonNetwork.Instantiate(playerPrefab.name, randomPosition, Quaternion.identity);
    }


}
