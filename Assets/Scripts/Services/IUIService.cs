using UnityEngine;

/// <summary>
/// UI service interface for managing user interface
/// </summary>
public interface IUIService
{
    void UpdateHealthBar(float health);
    void ShowNotification(string message);
    void ShowDialog(string title, string message);
    void HideDialog();
    void UpdateScore(int score);
    void ShowGameOverScreen();
    void ShowMainMenu();
    void Initialize();
    void Cleanup();
}