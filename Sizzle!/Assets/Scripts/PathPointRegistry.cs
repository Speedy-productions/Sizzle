using System.Collections.Generic;
using UnityEngine;

public class PathPointRegistry : MonoBehaviour
{
    public static PathPointRegistry Instance;

    [Header("Camino normal (en orden)")]
    public List<Transform> normalPath = new List<Transform>();

    [Header("Camino feliz (en orden)")]
    public List<Transform> happyPath = new List<Transform>();

    void Awake()
    {
        Instance = this;
    }

    public IReadOnlyList<Transform> GetNormal() => normalPath;
    public IReadOnlyList<Transform> GetHappy()  => happyPath;
}
