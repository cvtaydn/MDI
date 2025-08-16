using UnityEngine;
using MDI.Core;
using MDI.Containers;
using MDI.Extensions;
using System.Collections;
using MDI.Examples.Basic;

namespace MDI.Examples
{
    /// <summary>
    /// MDI Monitor'ün yakalayabileceği örnek bir GameManager script'i
    /// Bu script çeşitli servisleri register eder ve kullanır
    /// </summary>
    public class ExampleGameManager : MonoBehaviour
    {
        [Header("MDI Container")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private bool _enableHealthCheck = true;
        
        private MDIContainer _container;
        
        // Inject edilecek servisler
        [Inject] private IPlayerService _playerService;
        [Inject] private IAudioService _audioService;
        [Inject] private IUIService _uiService;
        [Inject] private IGameDataService _gameDataService;
        
        private void Awake()
        {
            SetupContainer();
            RegisterServices();
            InjectDependencies();
        }
        
        private void Start()
        {
            StartCoroutine(SimulateGameplay());
        }
        
        private void SetupContainer()
        {
            _container = new MDIContainer();
            
            // Monitoring ve Health Check otomatik olarak etkin
            // MDIContainer constructor'da ServiceMonitor ve HealthChecker oluşturuluyor
        }
        
        private void RegisterServices()
        {
            // Singleton servisler - Monitor'da tek instance olarak görünecek
            _container.RegisterSingletonWithOrder<IPlayerService, PlayerService>(100, 0, "Player Manager");
            _container.RegisterSingletonWithOrder<IAudioService, AudioService>(90, 0, "Audio Manager");
            _container.RegisterSingletonWithOrder<IUIService, UIService>(80, 0, "UI Manager");
            
            // Transient servis - Her resolve'da yeni instance
            _container.RegisterWithOrder<IGameDataService, GameDataService>(70, 0, "Game Data Provider");
            
            // Transient servisler (Scoped ve Lazy yerine)
            _container.RegisterWithOrder<IInventoryService, InventoryService>(60, 0, "Inventory System");
            _container.RegisterWithOrder<IAnalyticsService, AnalyticsService>(50, 0, "Analytics Tracker");
        }
        
        private void InjectDependencies()
        {
            // Bu GameManager'a inject et
            MDIUnityHelper.Inject(this, _container);
            
            // Sahnedeki diğer MonoBehaviour'lara da inject et
            var injectableObjects = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in injectableObjects)
            {
                if (obj != this && obj.GetType().GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                {
                    MDIUnityHelper.Inject(obj, _container);
                }
            }
        }
        
        private IEnumerator SimulateGameplay()
        {
            yield return new WaitForSeconds(1f);
            
            // Servisleri kullan - Monitor'da activity görünecek
            while (true)
            {
                // Player service kullanımı
                if (_playerService != null)
                {
                    _playerService.UpdatePlayerPosition(Random.insideUnitSphere * 10f);
                    _playerService.AddExperience(Random.Range(10, 50));
                }
                
                // Audio service kullanımı
                if (_audioService != null)
                {
                    _audioService.PlaySound("step_" + Random.Range(1, 4));
                }
                
                // UI service kullanımı
                if (_uiService != null)
                {
                    _uiService.UpdateHealthBar(Random.Range(0.5f, 1f));
                    _uiService.ShowNotification("Score: " + Random.Range(100, 1000));
                }
                
                // Game data service - Her seferinde yeni instance
                var gameData = _container.Resolve<IGameDataService>();
                gameData?.SavePlayerData();
                
                // Inventory service kullanımı
                var inventory = _container.Resolve<IInventoryService>();
                inventory?.AddItem("Coin", Random.Range(1, 5));
                
                // Analytics - Lazy loading
                var analytics = _container.Resolve<IAnalyticsService>();
                analytics?.TrackEvent("gameplay_action", "player_move");
                
                yield return new WaitForSeconds(Random.Range(2f, 5f));
            }
        }
        
        private void OnDestroy()
        {
            _container?.Dispose();
        }
        
        // Monitor'da görünecek debug bilgileri
        private void OnGUI()
        {
            if (!_enableMonitoring) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("MDI Example - Monitor'ı aç:");
            GUILayout.Label("Window > MDI > Monitor Window");
            GUILayout.Space(10);
            
            if (GUILayout.Button("Trigger Error (Test)"))
            {
                // Hata simülasyonu - Monitor'da görünecek
                try
                {
                    var nonExistentService = _container.Resolve<INonExistentService>();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Simulated error for monitoring: {ex.Message}");
                }
            }
            
            if (GUILayout.Button("Manual Health Check"))
            {
                // Manuel health check
                var healthChecker = _container.HealthChecker;
                var overallHealth = healthChecker.GetOverallHealth();
                Debug.Log($"Health Check Result: {overallHealth}");
                
                // Async health check başlat
                _ = healthChecker.CheckAllServicesHealthAsync();
            }
            
            GUILayout.EndArea();
        }
    }
    
    // Örnek servis interface'leri ve implementasyonları
    public interface IPlayerService
    {
        void UpdatePlayerPosition(Vector3 position);
        void AddExperience(int amount);
    }
    
    // IAudioService interface UnityGameManager.cs'de tanımlı
    
    public interface IUIService
    {
        void UpdateHealthBar(float health);
        void ShowNotification(string message);
    }
    
    public interface IGameDataService
    {
        void SavePlayerData();
    }
    
    public interface IInventoryService
    {
        void AddItem(string itemName, int count);
    }
    
    public interface IAnalyticsService
    {
        void TrackEvent(string category, string action);
    }
    
    public interface INonExistentService
    {
        void DoSomething();
    }
    
    // Basit implementasyonlar
    public class PlayerService : IPlayerService
    {
        public void UpdatePlayerPosition(Vector3 position)
        {
            Debug.Log($"Player moved to: {position}");
        }
        
        public void AddExperience(int amount)
        {
            Debug.Log($"Player gained {amount} XP");
        }
    }
    
    public class AudioService : IAudioService
    {
        public void PlayBackgroundMusic(string musicName)
        {
            Debug.Log($"Playing background music: {musicName}");
        }
        
        public void PlaySound(string soundName)
        {
            Debug.Log($"Playing sound: {soundName}");
        }
        
        public void StopBackgroundMusic()
        {
            Debug.Log("Background music stopped");
        }
        
        public void SetVolume(float volume)
        {
            Debug.Log($"Volume set to: {volume:P0}");
        }
    }
    
    public class UIService : IUIService
    {
        public void UpdateHealthBar(float health)
        {
            Debug.Log($"Health updated: {health:P0}");
        }
        
        public void ShowNotification(string message)
        {
            Debug.Log($"Notification: {message}");
        }
    }
    
    public class GameDataService : IGameDataService
    {
        public void SavePlayerData()
        {
            Debug.Log("Player data saved");
        }
    }
    
    public class InventoryService : IInventoryService
    {
        public void AddItem(string itemName, int count)
        {
            Debug.Log($"Added {count}x {itemName} to inventory");
        }
    }
    
    public class AnalyticsService : IAnalyticsService
    {
        public void TrackEvent(string category, string action)
        {
            Debug.Log($"Analytics: {category}/{action}");
        }
    }
}