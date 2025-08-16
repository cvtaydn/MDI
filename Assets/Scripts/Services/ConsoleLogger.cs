using UnityEngine;

/// <summary>
/// Console-based logger implementation
/// </summary>
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Debug.Log($"[Logger] {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[Logger] {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[Logger] {message}");
    }

    public void Initialize()
    {
        Debug.Log("[Logger] Console Logger initialized");
    }

    public void Cleanup()
    {
        Debug.Log("[Logger] Console Logger cleanup completed");
    }
}