using System.Collections;
using UnityEngine;

public class CoroutineHelper : MonoBehaviour
{
    private static CoroutineHelper instance;

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject();
        go.name = "CoroutineHelper";
        instance = go.AddComponent<CoroutineHelper>();
        DontDestroyOnLoad(go);
    }

    public static Coroutine StartRemoteCoroutine(IEnumerator function)
    {
        EnsureInstance();
        return instance.StartCoroutine(function);
    }

    public static void StopRemoteCoroutine(Coroutine coroutine)
    {
        EnsureInstance();
        instance.StopCoroutine(coroutine);
    }


}
