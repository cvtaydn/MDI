using System;
using System.Collections.Generic;
using UnityEngine;
using MDI.Core;
using MDI.Containers;
using MDI.Unity;
using static MDI.Core.MDI;

namespace MDI.Bootstrap
{
    /// <summary>
    /// MDI+ otomatik bootstrap sistemi
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Diğer script'lerden önce çalışsın
    public class MDIBootstrapper : MonoBehaviour
    {
        [Header("🔧 MDI+ Bootstrap Ayarları")]
        [SerializeField] private bool autoInjectScene = true;
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private bool enableHealthCheck = true;
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool dontDestroyOnLoad = true;
        
        [Header("📦 Service Configurations")]
        [SerializeField] private List<ServiceConfiguration> serviceConfigurations = new List<ServiceConfiguration>();
        
        [Header("🎯 Events")]
        public UnityEngine.Events.UnityEvent OnBootstrapCompleted;
        public UnityEngine.Events.UnityEvent OnBootstrapFailed;
        
        private static MDIBootstrapper _instance;
        private bool _isBootstrapped = false;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static MDIBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MDIBootstrapper>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[MDI+ Bootstrapper]");
                        _instance = go.AddComponent<MDIBootstrapper>();
                        Debug.Log("🚀 MDI+ Bootstrapper otomatik olarak oluşturuldu.");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Bootstrap tamamlandı mı?
        /// </summary>
        public bool IsBootstrapped => _isBootstrapped;
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                Bootstrap();
            }
            else if (_instance != this)
            {
                Debug.LogWarning("⚠️ Birden fazla MDIBootstrapper bulundu. Fazladan olan siliniyor.");
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Bootstrap işlemini başlatır
        /// </summary>
        public void Bootstrap()
        {
            if (_isBootstrapped)
            {
                Debug.LogWarning("⚠️ MDI+ zaten bootstrap edilmiş.");
                return;
            }
            
            try
            {
                if (enableLogging)
                {
                    Debug.Log("🚀 MDI+ Bootstrap başlatılıyor...");
                }
                
                // Container oluştur
                var builder = CreateContainer();
                
                // Monitoring ve health check ayarları
                if (enableMonitoring)
                {
                    builder.EnableMonitoring();
                }
                
                if (enableHealthCheck)
                {
                    builder.EnableHealthCheck();
                }
                
                // Service'leri register et
                RegisterServices(builder);
                
                // Container'ı build et ve global olarak ayarla
                var container = builder.BuildAndSetGlobal();
                
                // Scene injection
                if (autoInjectScene)
                {
                    MDIUnityHelper.InjectScene(container);
                }
                
                _isBootstrapped = true;
                
                if (enableLogging)
                {
                    Debug.Log("✅ MDI+ Bootstrap tamamlandı!");
                    LogStatistics();
                }
                
                OnBootstrapCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ MDI+ Bootstrap failed: {ex.Message}\n{ex.StackTrace}");
                OnBootstrapFailed?.Invoke();
            }
        }
        
        /// <summary>
        /// Service'leri register eder
        /// </summary>
        /// <param name="builder">Container builder</param>
        private void RegisterServices(MDIContainerBuilder builder)
        {
            foreach (var config in serviceConfigurations)
            {
                if (config.IsValid())
                {
                    try
                    {
                        RegisterService(builder, config);
                        
                        if (enableLogging)
                        {
                            Debug.Log($"✅ Service registered: {config.ServiceTypeName} -> {config.ImplementationTypeName} ({config.Lifetime})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Service registration failed: {config.ServiceTypeName} -> {config.ImplementationTypeName}\nError: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Invalid service configuration: {config.ServiceTypeName}");
                }
            }
        }
        
        /// <summary>
        /// Tek bir service'i register eder
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="config">Service configuration</param>
        private void RegisterService(MDIContainerBuilder builder, ServiceConfiguration config)
        {
            var serviceType = Type.GetType(config.ServiceTypeName);
            var implementationType = Type.GetType(config.ImplementationTypeName);
            
            if (serviceType == null)
            {
                throw new InvalidOperationException($"Service type not found: {config.ServiceTypeName}");
            }
            
            if (implementationType == null)
            {
                throw new InvalidOperationException($"Implementation type not found: {config.ImplementationTypeName}");
            }
            
            // Reflection ile generic method çağır
            var builderType = typeof(MDIContainerBuilder);
            string methodName = config.Lifetime switch
            {
                ServiceLifetime.Singleton => "AddSingleton",
                ServiceLifetime.Transient => "AddTransient",
                ServiceLifetime.Scoped => "AddScoped",
                _ => "AddTransient"
            };
            
            var method = builderType.GetMethod(methodName)?.MakeGenericMethod(serviceType, implementationType);
            method?.Invoke(builder, null);
        }
        
        /// <summary>
        /// Service configuration ekler
        /// </summary>
        /// <param name="serviceTypeName">Service type name</param>
        /// <param name="implementationTypeName">Implementation type name</param>
        /// <param name="lifetime">Service lifetime</param>
        public void AddServiceConfiguration(string serviceTypeName, string implementationTypeName, ServiceLifetime lifetime)
        {
            var config = new ServiceConfiguration
            {
                ServiceTypeName = serviceTypeName,
                ImplementationTypeName = implementationTypeName,
                Lifetime = lifetime
            };
            
            serviceConfigurations.Add(config);
        }
        
        /// <summary>
        /// Service configuration'ları temizler
        /// </summary>
        public void ClearServiceConfigurations()
        {
            serviceConfigurations.Clear();
        }
        
        /// <summary>
        /// Bootstrap'ı yeniden başlatır
        /// </summary>
        public void Restart()
        {
            if (_isBootstrapped)
            {
                Dispose();
                _isBootstrapped = false;
            }
            
            Bootstrap();
        }
        
        /// <summary>
        /// Application quit olduğunda cleanup yapar
        /// </summary>
        private void OnApplicationQuit()
        {
            if (_isBootstrapped)
            {
                Dispose();
                _isBootstrapped = false;
                
                if (enableLogging)
                {
                    Debug.Log("🧹 MDI+ Cleanup tamamlandı.");
                }
            }
        }
        
        /// <summary>
        /// Editor'da validation için
        /// </summary>
        private void OnValidate()
        {
            // Service configuration'ları validate et
            for (int i = serviceConfigurations.Count - 1; i >= 0; i--)
            {
                if (!serviceConfigurations[i].IsValid())
                {
                    Debug.LogWarning($"⚠️ Invalid service configuration at index {i}");
                }
            }
        }
    }
    
    /// <summary>
    /// Service configuration sınıfı
    /// </summary>
    [System.Serializable]
    public class ServiceConfiguration
    {
        [Header("🔧 Service Definition")]
        public string ServiceTypeName;
        public string ImplementationTypeName;
        public ServiceLifetime Lifetime = ServiceLifetime.Transient;
        
        [Header("📝 Description")]
        [TextArea(2, 4)]
        public string Description;
        
        /// <summary>
        /// Configuration'ın geçerli olup olmadığını kontrol eder
        /// </summary>
        /// <returns>Geçerliyse true</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ServiceTypeName) && 
                   !string.IsNullOrEmpty(ImplementationTypeName);
        }
        
        /// <summary>
        /// Type'ların mevcut olup olmadığını kontrol eder
        /// </summary>
        /// <returns>Type'lar mevcutsa true</returns>
        public bool AreTypesValid()
        {
            if (!IsValid()) return false;
            
            var serviceType = Type.GetType(ServiceTypeName);
            var implementationType = Type.GetType(ImplementationTypeName);
            
            return serviceType != null && implementationType != null;
        }
    }
}