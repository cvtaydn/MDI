using UnityEngine;

/// <summary>
/// Game service interface for managing game state and logic
/// </summary>
public interface IGameService
{
    void InitializePlayer(string playerName, int startingHealth);
    void StartGame();
    void DamagePlayer(int damage);
    void HealPlayer(int healAmount);
    void AddScore(int points);
    int GetPlayerHealth();
    int GetPlayerScore();
    string GetPlayerName();
    void Initialize();
    void Cleanup();
}