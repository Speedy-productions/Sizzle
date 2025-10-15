using UnityEngine;

[DisallowMultipleComponent]
public class WorldScaleMemory : MonoBehaviour
{
    public Vector3 initialWorldScale;
    public bool initialized;

    public void EnsureInit(Transform t)
    {
        if (!initialized)
        {
            initialWorldScale = t.lossyScale;
            initialized = true;
        }
    }
}
