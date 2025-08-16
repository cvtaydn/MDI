using UnityEngine;
using MDI.Containers;
using MDI.Extensions;
using MDI.Core;

namespace MDI.Examples.Basic
{
    /// <summary>
    /// Unity'de service registration örneği
    /// </summary>
    public class UnityServiceRegistration : MonoBehaviour
    {
        [Header("Service Registration")]
        [SerializeField] private bool registerOnAwake = true;
        [SerializeField] private bool useGlobalContainer = true;

        private IContainer _localContainer;

        private void Awake()
        {
            if (registerOnAwake)
            {
                RegisterServices();
            }
        }

        /// <summary>
        /// Tüm service'leri register eder
        /// </summary>
        [ContextMenu("Register Services")]
        public void RegisterServices()
        {
            if (useGlobalContainer)
            {
                RegisterServicesToGlobalContainer();
            }
            else
            {
                RegisterServicesToLocalContainer();
            }
        }

        /// <summary>
        /// Global container'a service'leri register eder
        /// </summary>
        private void RegisterServicesToGlobalContainer()
        {
            Debug.Log("Registering services to global container...");

            var container = MDIUnityHelper.GlobalContainer;

            // Logger service - Singleton
            container.RegisterSingleton<ILogger, ConsoleLogger>();
            Debug.Log("✓ Logger service registered as Singleton");

            // Game service - Singleton
            container.RegisterSingleton<IGameService, UnityGameService>();
            Debug.Log("✓ Game service registered as Singleton");

            // Audio service - Singleton
            container.RegisterSingleton<IAudioService, UnityAudioService>();
            Debug.Log("✓ Audio service registered as Singleton");

            // Email service - Transient (her seferinde yeni instance)
            container.RegisterTransient<IEmailService, EmailService>();
            Debug.Log("✓ Email service registered as Transient");

            // Notification service - Default (Transient)
            container.Register<INotificationService, NotificationService>();
            Debug.Log("✓ Notification service registered as Default");

            Debug.Log("All services registered to global container successfully!");
        }

        /// <summary>
        /// Local container'a service'leri register eder
        /// </summary>
        private void RegisterServicesToLocalContainer()
        {
            Debug.Log("Creating and registering services to local container...");

            _localContainer = new MDIContainer();

            // Logger service - Singleton
            _localContainer.RegisterSingleton<ILogger, ConsoleLogger>();
            Debug.Log("✓ Logger service registered as Singleton");

            // Game service - Singleton
            _localContainer.RegisterSingleton<IGameService, UnityGameService>();
            Debug.Log("✓ Game service registered as Singleton");

            // Audio service - Singleton
            _localContainer.RegisterSingleton<IAudioService, UnityAudioService>();
            Debug.Log("✓ Audio service registered as Singleton");

            // Email service - Transient
            _localContainer.RegisterTransient<IEmailService, EmailService>();
            Debug.Log("✓ Email service registered as Transient");

            // Notification service - Default
            _localContainer.Register<INotificationService, NotificationService>();
            Debug.Log("✓ Notification service registered as Default");

            Debug.Log("All services registered to local container successfully!");
        }

        /// <summary>
        /// Local container'ı kullanarak injection yapar
        /// </summary>
        /// <param name="component">Injection yapılacak component</param>
        public void InjectWithLocalContainer(MonoBehaviour component)
        {
            if (_localContainer == null)
            {
                Debug.LogError("Local container not initialized! Register services first.");
                return;
            }

            MDIUnityHelper.Inject(component, _localContainer);
            Debug.Log($"Component {component.name} injected with local container");
        }

        /// <summary>
        /// Service'leri test eder
        /// </summary>
        [ContextMenu("Test Services")]
        public void TestServices()
        {
            var container = useGlobalContainer ? MDIUnityHelper.GlobalContainer : _localContainer;

            if (container == null)
            {
                Debug.LogError("Container not available! Register services first.");
                return;
            }

            try
            {
                // Logger test
                var logger = container.Resolve<ILogger>();
                logger.Log("Logger service test successful!");

                // Game service test
                var gameService = container.Resolve<IGameService>();
                gameService.InitializePlayer("TestPlayer", 100);
                Debug.Log("Game service test successful!");

                // Audio service test
                var audioService = container.Resolve<IAudioService>();
                audioService.PlaySound("TestSound");
                Debug.Log("Audio service test successful!");

                // Email service test
                var emailService = container.Resolve<IEmailService>();
                emailService.SendEmail("test@example.com", "Test", "Test email");
                Debug.Log("Email service test successful!");

                // Notification service test
                var notificationService = container.Resolve<INotificationService>();
                notificationService.SendNotification("Test notification");
                Debug.Log("Notification service test successful!");

                Debug.Log("All service tests passed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Service test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Container'ı temizler
        /// </summary>
        [ContextMenu("Clear Container")]
        public void ClearContainer()
        {
            if (useGlobalContainer)
            {
                MDIUnityHelper.ClearGlobalContainer();
                Debug.Log("Global container cleared");
            }
            else if (_localContainer != null)
            {
                _localContainer.Clear();
                Debug.Log("Local container cleared");
            }
        }

        /// <summary>
        /// Container'ı dispose eder
        /// </summary>
        [ContextMenu("Dispose Container")]
        public void DisposeContainer()
        {
            if (useGlobalContainer)
            {
                MDIUnityHelper.DisposeGlobalContainer();
                Debug.Log("Global container disposed");
            }
            else if (_localContainer != null)
            {
                _localContainer.Dispose();
                _localContainer = null;
                Debug.Log("Local container disposed");
            }
        }

        private void OnDestroy()
        {
            // Local container'ı dispose et
            if (_localContainer != null)
            {
                _localContainer.Dispose();
                _localContainer = null;
            }
        }

        // Inspector'da container bilgilerini göster
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                var container = useGlobalContainer ? MDIUnityHelper.GlobalContainer : _localContainer;
                if (container != null)
                {
                    // Container durumunu güncelle
                }
            }
        }
    }
}
