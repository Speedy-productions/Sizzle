using UnityEngine;
using System.Collections;
public class UIBoot : MonoBehaviour
{
    public static bool Ready = false;

    IEnumerator Start()
    {
        yield return null; // esperar 1 frame
        Ready = true;
    }
}
