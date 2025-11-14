using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class PlayListManager : MonoBehaviour
{
    public PlayList playlistObject = new PlayList();

    [SerializeField] string JSONPath = "/Scripts/Video/";
    [SerializeField] string JSONFile = "playlist.json";

    public static PlayListManager instance;

    private void Awake()
    {
        instance = this;
    }

    public PlayList GetPlaylist()
    {
        return getFromFile();
    }

    PlayList getFromFile()
    {
        PlayList result = new PlayList();
        string path = Application.dataPath + JSONPath + JSONFile;
        bool fileValidation = File.Exists(path);
        if (fileValidation)
        {
            string streamContent = "";
            StreamReader fileReader = new StreamReader(Application.dataPath + JSONPath + JSONFile, Encoding.Default);
            streamContent = fileReader.ReadToEnd();
            fileReader.Close();
            Debug.Log(streamContent);
            result = JsonUtility.FromJson<PlayList>(streamContent);
        }
        else
        {
            Debug.Log("El archivo no existe");
        }
        RefreshEditorProjectWindow();
        return result;

    }

    PlayList getFromWeb()
    {
        PlayList result = new PlayList();
        return result;
    }

    void RefreshEditorProjectWindow()
    {
    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
    #endif
    }
}
