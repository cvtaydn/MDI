using UnityEngine;
using MDI.Core;
using MDI.Containers;
using MDI.Attributes;
using MDI.Configuration;
using MDI.Helpers;

namespace MDI.Examples
{
    /// <summary>
    /// MDI+ temel kullanım örnekleri
    /// </summary>
    public class BasicUsageExample : MonoBehaviour
    {
        [Header("Service References")]
        [Inject] private ILogger logger;
        [Inject] private IPlayerService playerService;
        [Inject] private IAudioService audioService;
        
        private void Start()
        {
            // Örnek 1: Basit Container Kullanımı
            BasicContainerExample();
            
            // Örnek 2: Fluent API ile Configuration
            FluentConfigurationExample();
            
            // Örnek 3: Helper Metodlar
            HelperMethodsExample();
            
            // Örnek 4: Auto-Registration
            AutoRegistrationExample();
            
            // Örnek 5: Validation ve Monitoring
            ValidationExample();
        }
        
        /// <summary>
        /// Basit container kullanımı
        /// </summary>
        private void BasicContainerExample()
        {
            Debug.Log("=== Basic Container Example ===");
            
            // Container oluştur
            using var container = new MDIContainer();
            
            // Service'leri register et
            container
                .RegisterSingleton<ILogger, ConsoleLogger>()
                .RegisterTransient<IPlayerService, PlayerService>()
                .RegisterSingleton<IAudioService, AudioService>();
            
            // Service'leri resolve et
            var logger = container.Resolve<ILogger>();
            var playerService = container.Resolve<IPlayerService>();
            
            logger.Log("Services resolved successfully!");
            playerService.Initialize();
        }
        
        /// <summary>
        /// Fluent API ile configuration
        /// </summary>
        private void FluentConfigurationExample()
        {
            Debug.Log("=== Fluent Configuration Example ===");
            
            // Configuration ile setup
            MDIConfiguration.ConfigureGlobalServices(config =>
            {
                config.Configure<ILogger, ConsoleLogger>()
                      .AsSingleton()
                      .WithName("MainLogger")
                      .InDebugOnly()
                      .OnRegistered(() => Debug.Log("Logger registered!"));
                      
                config.Configure<IPlayerService, PlayerService>()
                      .AsTransient()
                      .WithPriority(1)
                      .When(() => Application.isPlaying);
                      
                config.Configure<IAudioService, AudioService>()
                      .AsSingleton()
                      .OnPlatform(RuntimePlatform.WindowsPlayer)
                      .AndThen(audio => audio.Initialize());
            });
            
            Debug.Log("Global services configured!");
        }
        
        /// <summary>
        /// Helper metodlar örneği
        /// </summary>
        private void HelperMethodsExample()
        {
            Debug.Log("=== Helper Methods Example ===");
            
            // Güvenli service kullanımı
            MDI.UseService<ILogger>(logger => 
            {
                logger.Log("Using service safely!");
            }, error => 
            {
                Debug.LogError($"Service error: {error.Message}");
            });
            
            // Service varsa kullan
            bool found = MDI.IfServiceExists<IPlayerService>(player => 
            {
                player.SetHealth(100);
                Debug.Log("Player health set!");
            });
            
            if (!found)
            {
                Debug.Log("Player service not found");
            }
            
            // Function ile kullanım
            var playerName = MDI.UseService<IPlayerService, string>(
                player => player.GetPlayerName(), 
                "Unknown Player"
            );
            
            Debug.Log($"Player name: {playerName}");
            
            // Birden fazla service resolve et
            var services = MDI.ResolveMultiple(
                typeof(ILogger), 
                typeof(IPlayerService)
            );
            
            Debug.Log($"Resolved {services.Length} services");
        }
        
        /// <summary>
        /// Auto-registration örneği
        /// </summary>
        private void AutoRegistrationExample()
        {
            Debug.Log("=== Auto-Registration Example ===");
            
            var container = new MDIContainer();
            
            // Assembly'deki tüm service'leri otomatik register et
            int registeredCount = container.AutoRegisterServices();
            Debug.Log($"Auto-registered {registeredCount} services");
            
            // Namespace'den register et
            container.RegisterFromNamespace("MDI.Examples.Services");
            
            // Batch registration
            container.RegisterBatch(
                (typeof(ILogger), typeof(ConsoleLogger), ServiceLifetime.Singleton),
                (typeof(IPlayerService), typeof(PlayerService), ServiceLifetime.Transient)
            );
            
            // Conditional registration
            container.RegisterIf<IAudioService, AudioService>(
                () => SystemInfo.supportsAudio
            );
            
            // Platform-specific registration
            container.RegisterForPlatform<IInputService, MobileInputService>(
                RuntimePlatform.Android
            );
        }
        
        /// <summary>
        /// Validation ve monitoring örneği
        /// </summary>
        private void ValidationExample()
        {
            Debug.Log("=== Validation Example ===");
            
            // Health check
            if (MDI.IsHealthy())
            {
                Debug.Log("✅ All services are healthy!");
            }
            else
            {
                Debug.LogWarning("⚠️ Some services have issues");
            }
            
            // Performance report
            var report = MDI.GetPerformanceReport();
            Debug.Log($"Performance Report:\n{report}");
            
            // Registered services
            var serviceTypes = MDI.GetRegisteredServiceTypes();
            Debug.Log($"Registered {serviceTypes.Length} service types");
            
            // Service existence check
            bool hasLogger = MDI.IsRegistered<ILogger>();
            Debug.Log($"Logger registered: {hasLogger}");
        }
    }
    
    // Example service interfaces
    public interface ILogger
    {
        void Log(string message);
    }
    
    public interface IPlayerService
    {
        void Initialize();
        void SetHealth(int health);
        string GetPlayerName();
    }
    
    public interface IAudioService
    {
        void Initialize();
        void PlaySound(string soundName);
    }
    
    public interface IInputService
    {
        Vector2 GetInput();
    }
    
    // Example service implementations
    [MDIAutoRegister(ServiceLifetime.Singleton)]
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.Log($"[Logger] {message}");
        }
    }
    
    [MDIAutoRegister(ServiceLifetime.Transient)]
    public class PlayerService : IPlayerService
    {
        private int health = 100;
        private string playerName = "Player1";
        
        public void Initialize()
        {
            Debug.Log("Player service initialized");
        }
        
        public void SetHealth(int health)
        {
            this.health = health;
            Debug.Log($"Player health set to {health}");
        }
        
        public string GetPlayerName()
        {
            return playerName;
        }
    }
    
    [MDIAutoRegister(ServiceLifetime.Singleton)]
    public class AudioService : IAudioService
    {
        public void Initialize()
        {
            Debug.Log("Audio service initialized");
        }
        
        public void PlaySound(string soundName)
        {
            Debug.Log($"Playing sound: {soundName}");
        }
    }
    
    public class MobileInputService : IInputService
    {
        public Vector2 GetInput()
        {
            // Mobile input implementation
            return Vector2.zero;
        }
    }
}