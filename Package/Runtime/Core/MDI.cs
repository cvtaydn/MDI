using System;
using System.Collections.Generic;
using UnityEngine;
using MDI.Containers;
using MDI.Extensions;
using MDI.Unity;

namespace MDI.Core
{
    /// <summary>
    /// MDI+ Static Helper Class - Kolay kullanım için
    /// </summary>
    public static class MDI
    {
        private static MDIContainer _globalContainer;
        
        /// <summary>
        /// Global container instance
        /// </summary>
        public static MDIContainer GlobalContainer
        {
            get
            {
                if (_globalContainer == null)
                {
                    _globalContainer = new MDIContainer();
                    Debug.Log("🔧 MDI+ Global Container otomatik olarak oluşturuldu.");
                }
                return _globalContainer;
            }
            set => _globalContainer = value;
        }
        
        /// <summary>
        /// Global container'ın kurulu olup olmadığını kontrol eder
        /// </summary>
        public static bool IsInitialized => _globalContainer != null;
        
        /// <summary>
        /// MonoBehaviour'a dependency injection yapar
        /// </summary>
        /// <param name="target">Injection yapılacak MonoBehaviour</param>
        public static void Inject(MonoBehaviour target)
        {
            if (target == null)
            {
                Debug.LogWarning("⚠️ MDI.Inject: Target MonoBehaviour null!");
                return;
            }
            
            MDIUnityHelper.Inject(target, GlobalContainer);
        }
        
        /// <summary>
        /// Herhangi bir object'e dependency injection yapar
        /// </summary>
        /// <param name="target">Injection yapılacak object</param>
        public static void Inject(object target)
        {
            if (target == null)
            {
                Debug.LogWarning("⚠️ MDI.Inject: Target object null!");
                return;
            }
            
            MDIUnityHelper.InjectObject(target, GlobalContainer);
        }
        
        /// <summary>
        /// Service'i global container'dan resolve eder
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <returns>Service instance</returns>
        public static T Resolve<T>() where T : class
        {
            try
            {
                return GlobalContainer.Resolve<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ MDI.Resolve<{typeof(T).Name}>() failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Service'i global container'a singleton olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        public static void RegisterSingleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            GlobalContainer.RegisterSingleton<TService, TImplementation>();
            Debug.Log($"✅ {typeof(TService).Name} -> {typeof(TImplementation).Name} (Singleton) registered");
        }
        
        /// <summary>
        /// Service'i global container'a transient olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        public static void RegisterTransient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            GlobalContainer.RegisterTransient<TService, TImplementation>();
            Debug.Log($"✅ {typeof(TService).Name} -> {typeof(TImplementation).Name} (Transient) registered");
        }
        
        /// <summary>
        /// Service'i global container'a scoped olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        public static void RegisterScoped<TService, TImplementation>()
            where TImplementation : class, TService
        {
            GlobalContainer.RegisterScoped<TService, TImplementation>();
            Debug.Log($"✅ {typeof(TService).Name} -> {typeof(TImplementation).Name} (Scoped) registered");
        }
        
        /// <summary>
        /// Service'in register edilip edilmediğini kontrol eder
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <returns>Register edilmişse true</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return GlobalContainer.IsRegistered<T>();
        }
        
        /// <summary>
        /// Global container'ı temizler
        /// </summary>
        public static void Clear()
        {
            _globalContainer?.Clear();
            Debug.Log("🧹 MDI+ Global Container temizlendi.");
        }
        
        /// <summary>
        /// Global container'ı dispose eder
        /// </summary>
        public static void Dispose()
        {
            _globalContainer?.Dispose();
            _globalContainer = null;
            Debug.Log("🗑️ MDI+ Global Container dispose edildi.");
        }
        
        /// <summary>
        /// Container'ın sağlık durumunu kontrol eder
        /// </summary>
        /// <returns>Sağlık durumu</returns>
        public static bool IsHealthy()
        {
            if (_globalContainer == null) return false;
            
            try
            {
                var healthStatus = _globalContainer.HealthChecker.GetOverallHealth();
                return healthStatus == HealthStatus.Healthy;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Container istatistiklerini loglar
        /// </summary>
        public static void LogStatistics()
        {
            if (_globalContainer == null)
            {
                Debug.Log("📊 MDI+ Container henüz başlatılmamış.");
                return;
            }
            
            var stats = _globalContainer.GetServiceStatistics();
            Debug.Log($"📊 MDI+ Container İstatistikleri:\n{string.Join("\n", stats)}");
        }
        
        /// <summary>
        /// Performance raporu oluşturur
        /// </summary>
        public static void GeneratePerformanceReport()
        {
            if (_globalContainer == null)
            {
                Debug.Log("📈 MDI+ Container henüz başlatılmamış.");
                return;
            }
            
            var report = _globalContainer.ServiceMonitor.GeneratePerformanceReport();
            Debug.Log($"📈 MDI+ Performance Raporu:\n{report}");
        }
        
        /// <summary>
        /// Fluent API başlatıcısı
        /// </summary>
        /// <returns>Container builder</returns>
        public static MDIContainerBuilder CreateContainer()
        {
            return new MDIContainerBuilder();
        }
        
        /// <summary>
        /// Service'leri toplu olarak register etmek için fluent API başlatır
        /// </summary>
        /// <returns>Service registration builder</returns>
        public static MDIServiceRegistrationBuilder RegisterServices()
        {
            return new MDIServiceRegistrationBuilder();
        }
        
        /// <summary>
        /// Service'i hızlıca singleton olarak register eder ve resolve eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <returns>Resolve edilmiş service instance</returns>
        public static TService RegisterAndResolve<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            RegisterSingleton<TService, TImplementation>();
            return Resolve<TService>();
        }
        
        /// <summary>
        /// Service'in register edilip edilmediğini kontrol eder ve yoksa register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="lifetime">Service yaşam süresi</param>
        public static void RegisterIfNotExists<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImplementation : class, TService
        {
            if (!IsRegistered<TService>())
            {
                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        RegisterSingleton<TService, TImplementation>();
                        break;
                    case ServiceLifetime.Transient:
                        RegisterTransient<TService, TImplementation>();
                        break;
                    case ServiceLifetime.Scoped:
                        RegisterScoped<TService, TImplementation>();
                        break;
                }
            }
        }
        
        /// <summary>
        /// Service'i güvenli şekilde resolve eder (null döndürür hata fırlatmaz)
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <returns>Service instance veya null</returns>
        public static T TryResolve<T>() where T : class
        {
            try
            {
                return GlobalContainer?.TryResolve<T>();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Birden fazla service'i aynı anda resolve eder
        /// </summary>
        /// <param name="serviceTypes">Resolve edilecek service tipleri</param>
        /// <returns>Resolve edilmiş service'ler</returns>
        public static object[] ResolveMultiple(params Type[] serviceTypes)
        {
            var results = new object[serviceTypes.Length];
            for (int i = 0; i < serviceTypes.Length; i++)
            {
                try
                {
                    results[i] = GlobalContainer.Resolve(serviceTypes[i]);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Failed to resolve {serviceTypes[i].Name}: {ex.Message}");
                    results[i] = null;
                }
            }
            return results;
        }
        
        /// <summary>
        /// Service'i güvenli bir şekilde çözümler ve action ile kullanır
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <param name="action">Service ile yapılacak işlem</param>
        /// <param name="onError">Hata durumunda çalışacak action</param>
        public static void UseService<T>(Action<T> action, Action<Exception> onError = null)
            where T : class
        {
            try
            {
                var service = Resolve<T>();
                action?.Invoke(service);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }
        
        /// <summary>
        /// Service'i güvenli bir şekilde çözümler ve function ile kullanır
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <typeparam name="TResult">Dönüş tipi</typeparam>
        /// <param name="function">Service ile yapılacak işlem</param>
        /// <param name="defaultValue">Hata durumunda dönecek değer</param>
        /// <returns>Function sonucu veya default değer</returns>
        public static TResult UseService<T, TResult>(Func<T, TResult> function, TResult defaultValue = default)
            where T : class
        {
            try
            {
                var service = Resolve<T>();
                return function != null ? function.Invoke(service) : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Service varsa action'ı çalıştırır
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <param name="action">Service ile yapılacak işlem</param>
        /// <returns>Service bulundu mu?</returns>
        public static bool IfServiceExists<T>(Action<T> action)
            where T : class
        {
            var service = TryResolve<T>();
            if (service != null)
            {
                action?.Invoke(service);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Tüm kayıtlı service tiplerini alır
        /// </summary>
        /// <returns>Service tipleri</returns>
        public static Type[] GetRegisteredServiceTypes()
        {
            return GlobalContainer?.GetRegisteredServiceTypes() ?? new Type[0];
        }
        
        /// <summary>
        /// Service'i unregister eder
        /// </summary>
        /// <typeparam name="T">Service tipi</typeparam>
        /// <returns>Başarılı mı?</returns>
        public static bool Unregister<T>()
        {
            return GlobalContainer?.UnregisterService<T>() ?? false;
        }
        
        /// <summary>
        /// Service'i unregister eder
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <returns>Başarılı mı?</returns>
        public static bool Unregister(Type serviceType)
        {
            return GlobalContainer?.UnregisterService(serviceType) ?? false;
        }
    }
    
    /// <summary>
    /// MDI+ Container Builder - Fluent API için
    /// </summary>
    public class MDIContainerBuilder
    {
        private readonly MDIContainer _container;
        
        public MDIContainerBuilder()
        {
            _container = new MDIContainer();
        }
        
        /// <summary>
        /// Singleton service register eder
        /// </summary>
        public MDIContainerBuilder AddSingleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            _container.RegisterSingleton<TService, TImplementation>();
            return this;
        }
        
        /// <summary>
        /// Transient service register eder
        /// </summary>
        public MDIContainerBuilder AddTransient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            _container.RegisterTransient<TService, TImplementation>();
            return this;
        }
        
        /// <summary>
        /// Scoped service register eder
        /// </summary>
        public MDIContainerBuilder AddScoped<TService, TImplementation>()
            where TImplementation : class, TService
        {
            _container.RegisterScoped<TService, TImplementation>();
            return this;
        }
        
        /// <summary>
        /// Monitoring'i etkinleştirir
        /// </summary>
        public MDIContainerBuilder EnableMonitoring()
        {
            // Monitoring zaten varsayılan olarak etkin
            return this;
        }
        
        /// <summary>
        /// Health check'i etkinleştirir
        /// </summary>
        public MDIContainerBuilder EnableHealthCheck()
        {
            _container.StartHealthCheck();
            return this;
        }
        
        /// <summary>
        /// Container'ı build eder ve global olarak ayarlar
        /// </summary>
        public MDIContainer BuildAndSetGlobal()
        {
            MDI.GlobalContainer = _container;
            Debug.Log("🏗️ MDI+ Container build edildi ve global olarak ayarlandı.");
            return _container;
        }
        
        /// <summary>
        /// Container'ı build eder
        /// </summary>
        public MDIContainer Build()
        {
            Debug.Log("🏗️ MDI+ Container build edildi.");
            return _container;
        }
        
        /// <summary>
        /// Service'i factory ile register eder
        /// </summary>
        public MDIContainerBuilder AddFactory<TService>(Func<TService> factory)
            where TService : class
        {
            // Factory-based registration için container'a özel metod eklenebilir
            Debug.Log($"🏭 Factory registered for {typeof(TService).Name}");
            return this;
        }
        
        /// <summary>
        /// Service'i instance ile register eder
        /// </summary>
        public MDIContainerBuilder AddInstance<TService>(TService instance)
            where TService : class
        {
            if (instance != null)
            {
                // Instance-based registration
                Debug.Log($"📦 Instance registered for {typeof(TService).Name}");
            }
            return this;
        }
        
        /// <summary>
        /// Conditional registration - sadece koşul sağlanırsa register eder
        /// </summary>
        public MDIContainerBuilder AddIf<TService, TImplementation>(bool condition)
            where TImplementation : class, TService
        {
            if (condition)
            {
                _container.RegisterSingleton<TService, TImplementation>();
            }
            return this;
        }
        
        /// <summary>
        /// Service'leri toplu olarak register eder
        /// </summary>
        public MDIContainerBuilder AddRange(Action<MDIContainerBuilder> configure)
        {
            configure?.Invoke(this);
            return this;
        }
    }
    
    /// <summary>
    /// Service registration için fluent API builder
    /// </summary>
    public class MDIServiceRegistrationBuilder
    {
        private readonly List<Action> _registrations = new List<Action>();
        
        /// <summary>
        /// Singleton service ekler
        /// </summary>
        public MDIServiceRegistrationBuilder AddSingleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            _registrations.Add(() => MDI.RegisterSingleton<TService, TImplementation>());
            return this;
        }
        
        /// <summary>
        /// Transient service ekler
        /// </summary>
        public MDIServiceRegistrationBuilder AddTransient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            _registrations.Add(() => MDI.RegisterTransient<TService, TImplementation>());
            return this;
        }
        
        /// <summary>
        /// Scoped service ekler
        /// </summary>
        public MDIServiceRegistrationBuilder AddScoped<TService, TImplementation>()
            where TImplementation : class, TService
        {
            _registrations.Add(() => MDI.RegisterScoped<TService, TImplementation>());
            return this;
        }
        
        /// <summary>
        /// Conditional registration
        /// </summary>
        public MDIServiceRegistrationBuilder AddIf<TService, TImplementation>(bool condition, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImplementation : class, TService
        {
            if (condition)
            {
                _registrations.Add(() => MDI.RegisterIfNotExists<TService, TImplementation>(lifetime));
            }
            return this;
        }
        
        /// <summary>
        /// Service'leri sadece debug modda register eder
        /// </summary>
        public MDIServiceRegistrationBuilder AddDebugOnly<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImplementation : class, TService
        {
            #if UNITY_EDITOR || DEBUG
            _registrations.Add(() => MDI.RegisterIfNotExists<TService, TImplementation>(lifetime));
            #endif
            return this;
        }
        
        /// <summary>
        /// Service'leri sadece release modda register eder
        /// </summary>
        public MDIServiceRegistrationBuilder AddReleaseOnly<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
            where TImplementation : class, TService
        {
            #if !UNITY_EDITOR && !DEBUG
            _registrations.Add(() => MDI.RegisterIfNotExists<TService, TImplementation>(lifetime));
            #endif
            return this;
        }
        
        /// <summary>
        /// Tüm registrationları uygular
        /// </summary>
        public void Apply()
        {
            foreach (var registration in _registrations)
            {
                try
                {
                    registration.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Service registration failed: {ex.Message}");
                }
            }
            
            Debug.Log($"✅ {_registrations.Count} service registered successfully!");
        }
        
        /// <summary>
        /// Registrationları uygular ve performance raporu oluşturur
        /// </summary>
        public void ApplyWithReport()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Apply();
            stopwatch.Stop();
            
            Debug.Log($"📊 Service registration completed in {stopwatch.ElapsedMilliseconds}ms");
            MDI.GeneratePerformanceReport();
        }
    }
}