using UnityEngine;

public static class SDKDebug
{
    public static bool enabled = Debug.isDebugBuild;

    public static void Log(string message)
    {
        if (enabled) Debug.Log(message);
    }

    public static void Log(object message)
    {
        if (enabled) Debug.Log(message);
    }

    public static void LogWarning(string message)
    {
        if (enabled) Debug.LogWarning(message);
    }

    public static void LogError(string message)
    {
        if (enabled) Debug.LogError(message);
    }
}
