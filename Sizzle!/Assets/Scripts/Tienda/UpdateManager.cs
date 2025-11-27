using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    public int grillLevel { get; private set; }   // 0–3
    public int cutLevel { get; private set; }     // 0–3

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
    }

    public void UpgradeGrill()
    {
        if (grillLevel >= 3)
            return;

        grillLevel++;
        PlayerPrefs.SetInt("GrillLevel", grillLevel);
        PlayerPrefs.Save();
    }

    public void UpgradeCut()
    {
        if (cutLevel >= 3) 
            return;

        cutLevel++;
        PlayerPrefs.SetInt("CutLevel", cutLevel);
        PlayerPrefs.Save();
    }

}
