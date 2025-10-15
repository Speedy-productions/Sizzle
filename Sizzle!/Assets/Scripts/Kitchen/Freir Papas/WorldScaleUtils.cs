using UnityEngine;

public static class WorldScaleUtils
{
    // Reparent conservando escala mundial "targetWorldScale"
    public static void ReparentKeepWorldScale(Transform t, Transform newParent, Vector3 targetWorldScale)
    {
        // Evita NaN con padres faltantes
        Vector3 Sp = newParent ? newParent.lossyScale : Vector3.one;

        t.SetParent(newParent, false); // NO conservar mundo; nosotros lo fijamos

        t.localScale = new Vector3(
            Sp.x != 0f ? targetWorldScale.x / Sp.x : t.localScale.x,
            Sp.y != 0f ? targetWorldScale.y / Sp.y : t.localScale.y,
            Sp.z != 0f ? targetWorldScale.z / Sp.z : t.localScale.z
        );
    }

    // Asegura que el objeto tenga memoria de escala y devuelve la escala “original”
    public static Vector3 GetOrInitWorldScaleMemory(Transform t)
    {
        var mem = t.GetComponent<WorldScaleMemory>();
        if (!mem) mem = t.gameObject.AddComponent<WorldScaleMemory>();
        mem.EnsureInit(t);
        return mem.initialWorldScale;
    }
}
