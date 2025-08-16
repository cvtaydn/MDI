using UnityEngine;

/// <summary>
/// Logging service interface
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
    void Initialize();
    void Cleanup();
}