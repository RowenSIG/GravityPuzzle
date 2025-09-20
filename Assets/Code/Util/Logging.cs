using System.Diagnostics;
using UnityEngine;

public static class Logging
{
    [Conditional("DEBUG_LOG")]
    public static void Log(string log)
    {
        UnityEngine.Debug.Log(log);
    }
    [Conditional("DEBUG_LOG")]
    public static void LogError(string log)
    {
        UnityEngine.Debug.LogError(log);
    }
}
