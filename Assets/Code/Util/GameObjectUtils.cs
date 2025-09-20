using Unity.VisualScripting;
using UnityEngine;

public static class GameObjectUtils
{
    public static void EnsureActive(this GameObject gameObject, bool active)
    {
        if (gameObject == null)
            return;

        if (gameObject.activeSelf == active)
            return;

        gameObject.SetActive(active);
    }
}
