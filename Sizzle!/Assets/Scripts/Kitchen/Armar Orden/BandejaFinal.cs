using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BandejaFinal : MonoBehaviour, IPunInstantiateMagicCallback
{
    public List<string> ingredientesHamburguesa = new List<string>();
    public bool contienePapas;

    // Para SP o llamadas manuales – acepta string[] por comodidad interna
    public void SetDatos(List<string> ingredientes, bool papas)
    {
        ingredientesHamburguesa = ingredientes ?? new List<string>();
        contienePapas = papas;
    }

    // PUN instantiation: le llega un string join-eado y el bool
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        var data = info.photonView?.InstantiationData;
        if (data == null || data.Length < 2) return;

        string joined = data[0] as string ?? string.Empty;
        bool papas = (bool)data[1];

        if (string.IsNullOrEmpty(joined))
            ingredientesHamburguesa = new List<string>();
        else
            ingredientesHamburguesa = new List<string>(joined.Split('|'));

        contienePapas = papas;
    }

    // Fallback para late-joiners o si quieres re-sincronizar
    [PunRPC]
    public void RPC_ApplyDatos(string joined, bool papas)
    {
        if (string.IsNullOrEmpty(joined))
            ingredientesHamburguesa = new List<string>();
        else
            ingredientesHamburguesa = new List<string>(joined.Split('|'));

        contienePapas = papas;
    }
}
