using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MDI.Core;
using MDI.Containers;
using static MDI.Core.MDI;

namespace MDI.Extensions
{
    /// <summary>
    /// MDI+ için extension metodları
    /// Fluent API'yi daha kullanıcı dostu hale getirir
    /// </summary>
    public static class MDIExtensions
    {
        /// <summary>
        /// MonoBehaviour'a service'leri otomatik inject eder
        /// </summary>
        /// <param name="monoBehaviour">Target MonoBehaviour</param>
        /// <param name="container">Container (null ise global container kullanılır)</param>
        public static T InjectServices<T>(this T monoBehaviour, IContainer container = null) where T : MonoBehaviour
        {
            var targetContainer = container ?? GlobalContainer;
            if (targetContainer != null)
            {
                Inject(monoBehaviour);
            }
            return monoBehaviour;
        }
        
        /// <summary>
        /// Service'i resolve eder ve action ile kullanır
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <param name="container">Container</param>
        /// <param name="action">Service ile yapılacak işlem</param>
        public static IContainer UseService<TService>(this IContainer container, Action<TService> action) 
            where TService : class
        {
            try
            {
                var service = container.TryResolve<TService>();
                if (service != null)
                {
                    action?.Invoke(service);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Service {typeof(TService).Name} could not be resolved");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error using service {typeof(TService).Name}: {ex.Message}");
            }
            
            return container;
        }
        
        /// <summary>
        /// Service'i resolve eder ve func ile kullanır
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <typeparam name="TResult">Sonuç tipi</typeparam>
        /// <param name="container">Container</param>
        /// <param name="func">Service ile yapılacak işlem</param>
        /// <returns>İşlem sonucu</returns>
        public static TResult UseService<TService, TResult>(this IContainer container, Func<TService, TResult> func) 
            where TService : class
        {
            try
            {
                var service = container.TryResolve<TService>();
                if (service != null)
                {
                    return func != null ? func.Invoke(service) : default(TResult);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Service {typeof(TService).Name} could not be resolved");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error using service {typeof(TService).Name}: {ex.Message}");
            }
            
            return default(TResult);
        }
        
        /// <summary>
        /// Service'in mevcut olup olmadığını kontrol eder ve varsa action çalıştırır
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <param name="container">Container</param>
        /// <param name="action">Service varsa çalıştırılacak action</param>
        /// <param name="fallback">Service yoksa çalıştırılacak action</param>
        public static IContainer IfServiceExists<TService>(this IContainer container, Action<TService> action, Action fallback = null) 
            where TService : class
        {
            if (container.IsRegistered<TService>())
            {
                var service = container.TryResolve<TService>();
                if (service != null)
                {
                    action?.Invoke(service);
                }
            }
            else
            {
                fallback?.Invoke();
            }
            
            return container;
        }
        
        /// <summary>
        /// Birden fazla service'i sırayla resolve eder ve action ile kullanır
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="serviceTypes">Service tipleri</param>
        /// <param name="action">Her service için çalıştırılacak action</param>
        public static IContainer UseServices(this IContainer container, Type[] serviceTypes, Action<object> action)
        {
            foreach (var serviceType in serviceTypes)
            {
                try
                {
                    var service = container.TryResolve(serviceType);
                    if (service != null)
                    {
                        action?.Invoke(service);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error using service {serviceType.Name}: {ex.Message}");
                }
            }
            
            return container;
        }
        
        /// <summary>
        /// Service'leri batch olarak register eder
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="registrations">Registration action'ları</param>
        public static IContainer RegisterBatch(this IContainer container, params Action<IContainer>[] registrations)
        {
            foreach (var registration in registrations)
            {
                try
                {
                    registration?.Invoke(container);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Batch registration failed: {ex.Message}");
                }
            }
            
            return container;
        }
        
        /// <summary>
        /// Service'leri conditional olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Container</param>
        /// <param name="condition">Register koşulu</param>
        /// <param name="lifetime">Service yaşam süresi</param>
        public static IContainer RegisterIf<TService, TImplementation>(this IContainer container, bool condition, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            if (condition)
            {
                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        container.RegisterSingleton<TService, TImplementation>();
                        break;
                    case ServiceLifetime.Transient:
                        container.RegisterTransient<TService, TImplementation>();
                        break;
                    case ServiceLifetime.Scoped:
                        // Scoped için RegisterScoped metodu olduğunu varsayıyoruz
                        container.Register<TService, TImplementation>();
                        break;
                }
            }
            
            return container;
        }
        
        /// <summary>
        /// Service'leri debug modda register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Container</param>
        /// <param name="lifetime">Service yaşam süresi</param>
        public static IContainer RegisterDebugOnly<TService, TImplementation>(this IContainer container, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            #if UNITY_EDITOR || DEBUG
            return container.RegisterIf<TService, TImplementation>(true, lifetime);
            #else
            return container;
            #endif
        }
        
        /// <summary>
        /// Service'leri release modda register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Container</param>
        /// <param name="lifetime">Service yaşam süresi</param>
        public static IContainer RegisterReleaseOnly<TService, TImplementation>(this IContainer container, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            #if !UNITY_EDITOR && !DEBUG
            return container.RegisterIf<TService, TImplementation>(true, lifetime);
            #else
            return container;
            #endif
        }
        
        /// <summary>
        /// Container'ın sağlık durumunu kontrol eder ve rapor yazdırır
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="logToConsole">Console'a log yazılsın mı</param>
        /// <returns>Sağlık durumu</returns>
        public static bool CheckHealth(this IContainer container, bool logToConsole = true)
        {
            if (container is MDIContainer mdiContainer)
            {
                try
                {
                    var health = mdiContainer.GetOverallHealth();
                    var isHealthy = health == HealthStatus.Healthy;
                    
                    if (logToConsole)
                    {
                        var icon = isHealthy ? "✅" : "❌";
                        var report = mdiContainer.GenerateHealthReport();
                        Debug.Log($"{icon} MDI+ Health Check: {health}\n{report}");
                    }
                    
                    return isHealthy;
                }
                catch (Exception ex)
                {
                    if (logToConsole)
                    {
                        Debug.LogError($"❌ Health check failed: {ex.Message}");
                    }
                    return false;
                }
            }
            
            return true; // Diğer container'lar için varsayılan olarak healthy
        }
        
        /// <summary>
        /// Container'ın performans raporunu oluşturur
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="logToConsole">Console'a log yazılsın mı</param>
        /// <returns>Performans raporu</returns>
        public static string GeneratePerformanceReport(this IContainer container, bool logToConsole = true)
        {
            if (container is MDIContainer mdiContainer)
            {
                try
                {
                    var report = mdiContainer.GeneratePerformanceReport();
                    
                    if (logToConsole)
                    {
                        Debug.Log($"📊 MDI+ Performance Report:\n{report}");
                    }
                    
                    return report;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"❌ Performance report generation failed: {ex.Message}";
                    if (logToConsole)
                    {
                        Debug.LogError(errorMsg);
                    }
                    return errorMsg;
                }
            }
            
            return "Performance reporting not available for this container type.";
        }
    }
}