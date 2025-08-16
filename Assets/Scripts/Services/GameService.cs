using UnityEngine;

/// <summary>
/// Game service implementation for managing game state and logic
/// </summary>
public class GameService : IGameService
{
    private string _playerName;
    private int _playerHealth;
    private int _playerScore;
    private bool _gameStarted;

    public void InitializePlayer(string playerName, int startingHealth)
    {
        _playerName = playerName;
        _playerHealth = startingHealth;
        _playerScore = 0;
        Debug.Log($"[GameService] Player {_playerName} initialized with {startingHealth} health");
    }

    public void StartGame()
    {
        _gameStarted = true;
        Debug.Log($"[GameService] Game started for player {_playerName}");
    }

    public void DamagePlayer(int damage)
    {
        _playerHealth = Mathf.Max(0, _playerHealth - damage);
        Debug.Log($"[GameService] Player took {damage} damage. Health: {_playerHealth}");
        
        if (_playerHealth <= 0)
        {
            Debug.Log($"[GameService] Player {_playerName} has died!");
        }
    }

    public void HealPlayer(int healAmount)
    {
        _playerHealth += healAmount;
        Debug.Log($"[GameService] Player healed for {healAmount}. Health: {_playerHealth}");
    }

    public void AddScore(int points)
    {
        _playerScore += points;
        Debug.Log($"[GameService] Player scored {points} points. Total score: {_playerScore}");
    }

    public int GetPlayerHealth()
    {
        return _playerHealth;
    }

    public int GetPlayerScore()
    {
        return _playerScore;
    }

    public string GetPlayerName()
    {
        return _playerName;
    }

    public void Initialize()
    {
        Debug.Log("[GameService] Game Service initialized");
    }

    public void Cleanup()
    {
        Debug.Log("[GameService] Game Service cleanup completed");
        _gameStarted = false;
    }
}