using Photon.Pun;
using UnityEngine;

public class UpgradeManager : MonoBehaviourPun
{
    public static UpgradeManager Instance;

    public int grillLevel { get; private set; }   // 0–3
    public int cutLevel { get; private set; }     // 0–3
    public int fryerLevel { get; private set; }  // 0–3 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadData()
    {
        PlayerPrefs.DeleteKey("GrillLevel");
        grillLevel = PlayerPrefs.GetInt("GrillLevel", 0);
        PlayerPrefs.DeleteKey("CutLevel");
        PlayerPrefs.Save();

        cutLevel = PlayerPrefs.GetInt("CutLevel", 0);
        PlayerPrefs.DeleteKey("FryerLevel");
        fryerLevel = PlayerPrefs.GetInt("FryerLevel", 0);
    }

    public void UpgradeGrill()
    {
        if (grillLevel >= 3)
            return;

        grillLevel++;
        PlayerPrefs.SetInt("GrillLevel", grillLevel);
        PlayerPrefs.Save();

        photonView.RPC(nameof(RPC_SetGrillLevel), RpcTarget.Others, grillLevel);
    }

    public void UpgradeCut()
    {
        if (cutLevel >= 3) 
            return;

        cutLevel++;
        PlayerPrefs.SetInt("CutLevel", cutLevel);
        PlayerPrefs.Save();

        photonView.RPC(nameof(RPC_SetCutLevel), RpcTarget.Others, cutLevel);
        photonView.RPC(nameof(RPC_RefreshCuttingBlades), RpcTarget.All);
    }
    public void UpgradeFryer()
    {
        if (fryerLevel >= 3)
            return;

        fryerLevel++;
        PlayerPrefs.SetInt("FryerLevel", fryerLevel);
        PlayerPrefs.Save();
    }

    [PunRPC]
    void RPC_SetGrillLevel(int lvl)
    {
        grillLevel = lvl;
    }

    [PunRPC]
    void RPC_SetCutLevel(int lvl)
    {
        cutLevel = lvl;
        PlayerPrefs.SetInt("CutLevel", cutLevel);
    }
    [PunRPC]
    void RPC_RefreshCuttingBlades()
    {
        foreach (var blade in FindObjectsOfType<Blade>())
            blade.ApplyCuttingUpgrade();
    }

}
