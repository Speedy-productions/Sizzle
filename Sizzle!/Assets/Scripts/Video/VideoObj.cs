using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayList
{
    public List<VideoObj>Videos = new List<VideoObj>();
}

[Serializable]
public class VideoObj
{
    public string videoURL;
}
