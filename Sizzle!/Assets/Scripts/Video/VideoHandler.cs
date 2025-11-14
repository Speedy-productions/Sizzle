using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoHandler : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    [SerializeField] public PlayList playList;
    [SerializeField] int videoIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        // 🔇 Desactivar audio del VideoPlayer
        if (videoPlayer != null)
        {
            // Opción 1: sin salida de audio
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            // (Opcional) por si en algún momento cambias el modo a Direct,
            // dejamos todas las pistas en mute.
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioMute(i, true);
            }
        }

        playList = PlayListManager.instance.GetPlaylist();
        if (playList.Videos.Count > 0)
        {
            StartCoroutine("startReproduction");
        }
        else
        {
            Debug.Log("revisa carga de archivos");
        }
    }

    IEnumerator startReproduction()
    {
        if (!videoPlayer.isPlaying)
        {
            Debug.Log("//////////////////////////////////" + '\n' + "loading video index:" + videoIndex);
            loadVideo(videoIndex);
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine("startReproduction");
    }

    void loadVideo(int index)
    {
        if (index >= playList.Videos.Count)
        {
            index = 0;
            videoIndex = 0;
        }

        videoPlayer.url = Application.dataPath + playList.Videos[index].videoURL;
        videoPlayer.Play();

        videoIndex++;
    }
}
