using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class BandejaSpawner : MonoBehaviourPun
{
    [Header("Prefab (nombre en Resources/Photon)")]
    public string bandejaPrefabName = "BandejaArmado"; // debe coincidir con el nombre del prefab registrado

    [Header("Spawn points (auto: hijos de 'spawnBandeja')")]
    public string spawnParentName = "spawnBandeja";
    public Transform[] spawnPoints;


[Header("Arranque")]
public bool spawnOnStart = false; // <- por defecto NO spawnea


    void Awake()
    {
        // Autodescubrir hijos de "spawnBandeja" si no hay asignados
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            var parent = GameObject.Find(spawnParentName);
            if (parent)
            {
                var list = new List<Transform>();
                foreach (Transform child in parent.transform)
                    list.Add(child);
                spawnPoints = list.ToArray();
            }
            else
            {
                // Fallback: usa el propio transform del spawner
                spawnPoints = new Transform[] { transform };
            }
        }
    }


void Start()
    {
        if (!spawnOnStart) return;
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        foreach (var sp in spawnPoints)
        {
            var pos = sp ? sp.position : Vector3.zero;
            var baseRot = sp ? sp.rotation : Quaternion.identity;

            // Forzar X = -90º (conservar Y/Z del spawn point)
            var e = baseRot.eulerAngles;
            var spawnRot = Quaternion.Euler(-90f, e.y, e.z);

            PhotonNetwork.Instantiate(bandejaPrefabName, pos, spawnRot);
        }
    }

    // En BandejaSpawner
public void RequestSpawnFrom(Vector3 requesterPos)
{
    if (!PhotonNetwork.InRoom)
    {
        SpawnAtClosest(requesterPos);
        return;
    }

    if (PhotonNetwork.IsMasterClient)
        SpawnAtClosest(requesterPos);
    else
        photonView.RPC(nameof(RPC_RequestSpawn), RpcTarget.MasterClient, requesterPos);
}

[PunRPC]
void RPC_RequestSpawn(Vector3 requesterPos)
{
    if (!PhotonNetwork.IsMasterClient) return;
    SpawnAtClosest(requesterPos);
}

void SpawnAtClosest(Vector3 fromPos)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Transform best = spawnPoints[0];
        float bestD = Vector3.SqrMagnitude((best ? best.position : transform.position) - fromPos);

        for (int i = 1; i < spawnPoints.Length; i++)
        {
            var p = spawnPoints[i] ? spawnPoints[i].position : transform.position;
            float d = Vector3.SqrMagnitude(p - fromPos);
            if (d < bestD) { bestD = d; best = spawnPoints[i]; }
        }

        var pos = best ? best.position : transform.position;
        var baseRot = best ? best.rotation : transform.rotation;

        // Forzar X = -90º (conservar Y/Z del spawn point)
        var e = baseRot.eulerAngles;
        var spawnRot = Quaternion.Euler(-90f, e.y, e.z);

        PhotonNetwork.Instantiate(bandejaPrefabName, pos, spawnRot);
    }

}
