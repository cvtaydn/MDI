using UnityEngine;

/// <summary>
/// UI service implementation for managing user interface
/// </summary>
public class UIService : IUIService
{
    private bool _dialogVisible = false;
    private float _currentHealth = 100f;
    private int _currentScore = 0;

    public void UpdateHealthBar(float health)
    {
        _currentHealth = health;
        Debug.Log($"[UIService] Health bar updated: {health:F1}%");
        // Health bar UI güncelleme logic'i burada olacak
    }

    public void ShowNotification(string message)
    {
        Debug.Log($"[UIService] Notification: {message}");
        // Notification UI gösterme logic'i burada olacak
    }

    public void ShowDialog(string title, string message)
    {
        _dialogVisible = true;
        Debug.Log($"[UIService] Dialog shown - Title: {title}, Message: {message}");
        // Dialog UI gösterme logic'i burada olacak
    }

    public void HideDialog()
    {
        _dialogVisible = false;
        Debug.Log("[UIService] Dialog hidden");
        // Dialog UI gizleme logic'i burada olacak
    }

    public void UpdateScore(int score)
    {
        _currentScore = score;
        Debug.Log($"[UIService] Score updated: {score}");
        // Score UI güncelleme logic'i burada olacak
    }

    public void ShowGameOverScreen()
    {
        Debug.Log($"[UIService] Game Over screen shown. Final Score: {_currentScore}");
        // Game Over UI gösterme logic'i burada olacak
    }

    public void ShowMainMenu()
    {
        Debug.Log("[UIService] Main menu shown");
        // Main menu UI gösterme logic'i burada olacak
    }

    public void Initialize()
    {
        Debug.Log("[UIService] UI Service initialized");
        _currentHealth = 100f;
        _currentScore = 0;
        _dialogVisible = false;
    }

    public void Cleanup()
    {
        Debug.Log("[UIService] UI Service cleanup completed");
        HideDialog();
    }
}