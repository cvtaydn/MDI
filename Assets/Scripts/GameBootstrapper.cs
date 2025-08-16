using UnityEngine;
using MDI.Core;
using MDI.Containers;

/// <summary>
/// Game için MDI+ service registration'larını yöneten bootstrapper
/// </summary>
[DefaultExecutionOrder(-1000)] // Diğer script'lerden önce çalışsın
public class GameBootstrapper : MonoBehaviour
{
    [Header("🔧 MDI+ Bootstrap Ayarları")]
    [SerializeField] private bool autoInjectScene = true;
    [SerializeField] private bool enableMonitoring = true;
    [SerializeField] private bool enableHealthCheck = true;
    [SerializeField] private bool enableLogging = true;
    
    private void Awake()
    {
        // Service'leri register et ve container'ı oluştur
        RegisterGameServices();
        
        Debug.Log("🎮 Game Bootstrapper: Tüm servisler kaydedildi ve bootstrap tamamlandı!");
    }
    
    private void RegisterGameServices()
    {
        // Type-safe registration kullanarak service'leri kaydet
        var builder = new MDI.Core.MDIContainerBuilder();
        
        // Service'leri register et
        builder.AddSingleton<ILogger, ConsoleLogger>();
        builder.AddSingleton<IGameService, GameService>();
        builder.AddSingleton<IAudioService, AudioService>();
        builder.AddSingleton<IUIService, UIService>();
        
        // Container'ı build et ve global olarak ayarla
        var container = builder.Build();
        MDI.Core.MDI.GlobalContainer = container;
        
        // Debug: Container'da servislerin kaydedilip kaydedilmediğini kontrol et
        Debug.Log($"[GameBootstrapper] Container built with {container.GetRegisteredServiceTypes().Length} services (HashCode: {container.GetHashCode()})");
        Debug.Log($"[GameBootstrapper] ILogger registered: {container.IsRegistered<ILogger>()}");
        Debug.Log($"[GameBootstrapper] IGameService registered: {container.IsRegistered<IGameService>()}");
        Debug.Log($"[GameBootstrapper] IAudioService registered: {container.IsRegistered<IAudioService>()}");
        Debug.Log($"[GameBootstrapper] IUIService registered: {container.IsRegistered<IUIService>()}");
        
        if (enableLogging)
        {
            Debug.Log("✅ Game Services registered:");
            Debug.Log("   - ILogger -> ConsoleLogger (Singleton)");
            Debug.Log("   - IGameService -> GameService (Singleton)");
            Debug.Log("   - IAudioService -> AudioService (Singleton)");
            Debug.Log("   - IUIService -> UIService (Singleton)");
        }
    }
}