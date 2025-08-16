using System;
using System.Collections.Generic;
using UnityEngine;
using MDI.Core;
using MDI.Containers;
using static MDI.Core.MDI;

namespace MDI.Configuration
{
    /// <summary>
    /// Service configuration için fluent API
    /// </summary>
    public class MDIServiceConfiguration
    {
        private readonly List<ServiceRegistrationConfig> _configurations = new List<ServiceRegistrationConfig>();
        
        /// <summary>
        /// Yeni bir service configuration başlatır
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <returns>Service configuration builder</returns>
        public ServiceConfigurationBuilder<TService, TImplementation> Configure<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var config = new ServiceRegistrationConfig
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Singleton
            };
            
            _configurations.Add(config);
            return new ServiceConfigurationBuilder<TService, TImplementation>(config);
        }
        
        /// <summary>
        /// Tüm konfigürasyonları container'a uygular
        /// </summary>
        /// <param name="container">Target container</param>
        public void ApplyTo(IContainer container)
        {
            foreach (var config in _configurations)
            {
                try
                {
                    if (config.Condition != null && !config.Condition.Invoke())
                    {
                        continue; // Koşul sağlanmıyorsa atla
                    }
                    
                    RegisterService(container, config);
                    
                    if (config.PostRegistrationAction != null)
                    {
                        config.PostRegistrationAction.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Service registration failed for {config.ServiceType.Name}: {ex.Message}");
                }
            }
            
            Debug.Log($"✅ {_configurations.Count} service configurations applied successfully!");
        }
        
        /// <summary>
        /// Service'i container'a register eder
        /// </summary>
        private void RegisterService(IContainer container, ServiceRegistrationConfig config)
        {
            // Reflection ile generic method çağır
            var containerType = container.GetType();
            string methodName = config.Lifetime switch
            {
                ServiceLifetime.Singleton => "RegisterSingleton",
                ServiceLifetime.Transient => "RegisterTransient",
                ServiceLifetime.Scoped => "Register", // Scoped için varsayılan Register kullan
                _ => "Register"
            };
            
            var method = containerType.GetMethod(methodName)?.MakeGenericMethod(config.ServiceType, config.ImplementationType);
            if (method != null)
            {
                method.Invoke(container, null);
                Debug.Log($"🔧 {config.ServiceType.Name} -> {config.ImplementationType.Name} ({config.Lifetime}) registered");
            }
            else
            {
                throw new InvalidOperationException($"Method not found: {methodName}<{config.ServiceType.Name}, {config.ImplementationType.Name}>");
            }
        }
    }
    
    /// <summary>
    /// Service configuration builder
    /// </summary>
    /// <typeparam name="TService">Service interface tipi</typeparam>
    /// <typeparam name="TImplementation">Implementation tipi</typeparam>
    public class ServiceConfigurationBuilder<TService, TImplementation>
        where TService : class
        where TImplementation : class, TService
    {
        private readonly ServiceRegistrationConfig _config;
        
        internal ServiceConfigurationBuilder(ServiceRegistrationConfig config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Service'i singleton olarak ayarlar
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> AsSingleton()
        {
            _config.Lifetime = ServiceLifetime.Singleton;
            return this;
        }
        
        /// <summary>
        /// Service'i transient olarak ayarlar
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> AsTransient()
        {
            _config.Lifetime = ServiceLifetime.Transient;
            return this;
        }
        
        /// <summary>
        /// Service'i scoped olarak ayarlar
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> AsScoped()
        {
            _config.Lifetime = ServiceLifetime.Scoped;
            return this;
        }
        
        /// <summary>
        /// Service için bir isim belirler
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> WithName(string name)
        {
            _config.Name = name;
            return this;
        }
        
        /// <summary>
        /// Service için öncelik belirler
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> WithPriority(int priority)
        {
            _config.Priority = priority;
            return this;
        }
        
        /// <summary>
        /// Service için execution order belirler
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> WithExecutionOrder(int order)
        {
            _config.ExecutionOrder = order;
            return this;
        }
        
        /// <summary>
        /// Conditional registration - sadece koşul sağlanırsa register eder
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> When(Func<bool> condition)
        {
            _config.Condition = condition;
            return this;
        }
        
        /// <summary>
        /// Debug modda register eder
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> InDebugOnly()
        {
            #if UNITY_EDITOR || DEBUG
            _config.Condition = () => true;
            #else
            _config.Condition = () => false;
            #endif
            return this;
        }
        
        /// <summary>
        /// Release modda register eder
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> InReleaseOnly()
        {
            #if !UNITY_EDITOR && !DEBUG
            _config.Condition = () => true;
            #else
            _config.Condition = () => false;
            #endif
            return this;
        }
        
        /// <summary>
        /// Platform-specific registration
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> OnPlatform(RuntimePlatform platform)
        {
            _config.Condition = () => Application.platform == platform;
            return this;
        }
        
        /// <summary>
        /// Registration sonrası çalıştırılacak action
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> OnRegistered(Action action)
        {
            _config.PostRegistrationAction = action;
            return this;
        }
        
        /// <summary>
        /// Service'i hemen resolve eder ve action ile kullanır
        /// </summary>
        public ServiceConfigurationBuilder<TService, TImplementation> AndThen(Action<TService> action)
        {
            _config.PostRegistrationAction = () =>
            {
                try
                {
                    var service = Resolve<TService>();
                    action?.Invoke(service);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Post-registration action failed for {typeof(TService).Name}: {ex.Message}");
                }
            };
            return this;
        }
    }
    
    /// <summary>
    /// Service registration configuration
    /// </summary>
    internal class ServiceRegistrationConfig
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }
        public int ExecutionOrder { get; set; }
        public Func<bool> Condition { get; set; }
        public Action PostRegistrationAction { get; set; }
    }
    
    /// <summary>
    /// MDI+ Configuration helper
    /// </summary>
    public static class MDIConfiguration
    {
        /// <summary>
        /// Yeni bir service configuration başlatır
        /// </summary>
        /// <returns>Service configuration</returns>
        public static MDIServiceConfiguration CreateConfiguration()
        {
            return new MDIServiceConfiguration();
        }
        
        /// <summary>
        /// Hızlı configuration için fluent API
        /// </summary>
        /// <param name="configure">Configuration action</param>
        /// <returns>Configured container</returns>
        public static IContainer ConfigureServices(Action<MDIServiceConfiguration> configure)
        {
            var configuration = new MDIServiceConfiguration();
            configure?.Invoke(configuration);
            
            var container = new MDIContainer();
            configuration.ApplyTo(container);
            
            return container;
        }
        
        /// <summary>
        /// Global container'ı configure eder
        /// </summary>
        /// <param name="configure">Configuration action</param>
        public static void ConfigureGlobalServices(Action<MDIServiceConfiguration> configure)
        {
            var configuration = new MDIServiceConfiguration();
            configure?.Invoke(configuration);
            
            if (GlobalContainer == null)
            {
                GlobalContainer = new MDIContainer();
            }
            
            configuration.ApplyTo(GlobalContainer);
        }
    }
}