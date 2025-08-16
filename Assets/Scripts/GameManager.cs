using UnityEngine;
using MDI.Extensions;

/// <summary>
/// Ana game manager sınıfı - MDI+ ile service injection kullanır
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("🎮 Injected Services")]
    [Inject] private ILogger logger;
    [Inject] private IGameService gameService;
    [Inject] private IAudioService audioService;
    [Inject] private IUIService uiService;
    
    [Header("🎯 Game Settings")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private bool enableGameplaySimulation = true;
    
    private void Awake()
    {
        // MDI+ injection'ı gerçekleştir
        MDI.Core.MDI.Inject(this);
        
        // Injection'ın başarılı olup olmadığını kontrol et
        ValidateInjection();
    }
    
    private void Start()
    {
        // Servisleri initialize et
        InitializeServices();
        
        // Game'i başlat
        StartGame();
        
        // Gameplay simulation'ı başlat
        if (enableGameplaySimulation)
        {
            StartCoroutine(SimulateGameplay());
        }
    }
    
    private void ValidateInjection()
    {
        bool allServicesInjected = true;
        
        if (logger == null)
        {
            Debug.LogError("❌ Logger service not injected!");
            allServicesInjected = false;
        }
        
        if (gameService == null)
        {
            Debug.LogError("❌ Game service not injected!");
            allServicesInjected = false;
        }
        
        if (audioService == null)
        {
            Debug.LogError("❌ Audio service not injected!");
            allServicesInjected = false;
        }
        
        if (uiService == null)
        {
            Debug.LogError("❌ UI service not injected!");
            allServicesInjected = false;
        }
        
        if (allServicesInjected)
        {
            Debug.Log("✅ All services injected successfully!");
        }
        else
        {
            Debug.LogError("❌ Some services failed to inject. Check GameBootstrapper configuration.");
        }
    }
    
    private void InitializeServices()
    {
        logger?.Initialize();
        gameService?.Initialize();
        audioService?.Initialize();
        uiService?.Initialize();
        
        logger?.Log("🎮 Game Manager: All services initialized");
    }
    
    private void StartGame()
    {
        logger?.Log($"🚀 Starting game for player: {playerName}");
        
        // Player'ı initialize et
        gameService?.InitializePlayer(playerName, startingHealth);
        
        // Game'i başlat
        gameService?.StartGame();
        
        // UI'ı güncelle
        uiService?.UpdateHealthBar(startingHealth);
        uiService?.UpdateScore(0);
        
        // Background music'i başlat
        audioService?.PlayBackgroundMusic("main_theme");
        
        logger?.Log("✅ Game started successfully!");
    }
    
    private System.Collections.IEnumerator SimulateGameplay()
    {
        yield return new WaitForSeconds(2f);
        
        while (true)
        {
            // Rastgele gameplay olayları simüle et
            int eventType = Random.Range(0, 4);
            
            switch (eventType)
            {
                case 0: // Damage
                    int damage = Random.Range(5, 15);
                    gameService?.DamagePlayer(damage);
                    audioService?.PlaySound("damage");
                    uiService?.UpdateHealthBar(gameService?.GetPlayerHealth() ?? 0);
                    logger?.LogWarning($"Player took {damage} damage!");
                    break;
                    
                case 1: // Heal
                    int heal = Random.Range(10, 20);
                    gameService?.HealPlayer(heal);
                    audioService?.PlaySound("heal");
                    uiService?.UpdateHealthBar(gameService?.GetPlayerHealth() ?? 0);
                    logger?.Log($"Player healed for {heal} points!");
                    break;
                    
                case 2: // Score
                    int points = Random.Range(50, 200);
                    gameService?.AddScore(points);
                    audioService?.PlaySound("score");
                    uiService?.UpdateScore(gameService?.GetPlayerScore() ?? 0);
                    logger?.Log($"Player gained {points} points!");
                    break;
                    
                case 3: // Notification
                    string[] messages = { "New quest available!", "Achievement unlocked!", "Bonus item found!" };
                    string message = messages[Random.Range(0, messages.Length)];
                    uiService?.ShowNotification(message);
                    audioService?.PlaySound("notification");
                    logger?.Log($"Notification: {message}");
                    break;
            }
            
            yield return new WaitForSeconds(Random.Range(3f, 6f));
        }
    }
    
    private void OnDestroy()
    {
        // Servisleri cleanup et
        logger?.Cleanup();
        gameService?.Cleanup();
        audioService?.Cleanup();
        uiService?.Cleanup();
        
        logger?.Log("🧹 Game Manager: Cleanup completed");
    }
}