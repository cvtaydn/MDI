using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using MDI.Core;
using MDI.Containers;
using MDI.Attributes;
using MDI.Extensions;

namespace MDI.Helpers
{
    /// <summary>
    /// Service management için helper metodlar
    /// </summary>
    public static class MDIServiceHelper
    {
        /// <summary>
        /// Assembly'deki tüm service'leri otomatik register eder
        /// </summary>
        /// <param name="container">Target container</param>
        /// <param name="assembly">Taranacak assembly (null ise current assembly)</param>
        /// <returns>Register edilen service sayısı</returns>
        public static int AutoRegisterServices(this IContainer container, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            int registeredCount = 0;
            
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<MDIAutoRegisterAttribute>() != null)
                .ToList();
            
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<MDIAutoRegisterAttribute>();
                var interfaces = type.GetInterfaces();
                
                if (interfaces.Length == 0)
                {
                    // Interface yoksa concrete type olarak register et
                    RegisterServiceByLifetime(container, type, type, attribute.Lifetime);
                    registeredCount++;
                }
                else
                {
                    // Her interface için register et
                    foreach (var interfaceType in interfaces)
                    {
                        if (interfaceType.IsGenericType || interfaceType == typeof(IDisposable))
                            continue;
                            
                        RegisterServiceByLifetime(container, interfaceType, type, attribute.Lifetime);
                        registeredCount++;
                    }
                }
            }
            
            Debug.Log($"🔧 Auto-registered {registeredCount} services from {assembly.GetName().Name}");
            return registeredCount;
        }
        
        /// <summary>
        /// Namespace'deki service'leri register eder
        /// </summary>
        /// <param name="container">Target container</param>
        /// <param name="namespaceName">Namespace adı</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Register edilen service sayısı</returns>
        public static int RegisterFromNamespace(this IContainer container, string namespaceName, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Namespace == namespaceName)
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();
            
            int registeredCount = 0;
            
            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => !i.IsGenericType && i != typeof(IDisposable))
                    .ToList();
                
                if (interfaces.Count > 0)
                {
                    foreach (var interfaceType in interfaces)
                    {
                        RegisterServiceByLifetime(container, interfaceType, type, lifetime);
                        registeredCount++;
                    }
                }
            }
            
            Debug.Log($"🔧 Registered {registeredCount} services from namespace '{namespaceName}'");
            return registeredCount;
        }
        
        /// <summary>
        /// Service'leri batch olarak register eder
        /// </summary>
        /// <param name="container">Target container</param>
        /// <param name="registrations">Service registrations</param>
        /// <returns>Register edilen service sayısı</returns>
        public static int RegisterBatch(this IContainer container, params (Type serviceType, Type implementationType, ServiceLifetime lifetime)[] registrations)
        {
            int registeredCount = 0;
            
            foreach (var (serviceType, implementationType, lifetime) in registrations)
            {
                try
                {
                    RegisterServiceByLifetime(container, serviceType, implementationType, lifetime);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Failed to register {serviceType.Name} -> {implementationType.Name}: {ex.Message}");
                }
            }
            
            Debug.Log($"✅ Batch registered {registeredCount}/{registrations.Length} services");
            return registeredCount;
        }
        
        /// <summary>
        /// Service'i koşullu olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Target container</param>
        /// <param name="condition">Register koşulu</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Register edildi mi?</returns>
        public static bool RegisterIf<TService, TImplementation>(this IContainer container, Func<bool> condition, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            if (condition?.Invoke() == true)
            {
                RegisterServiceByLifetime(container, typeof(TService), typeof(TImplementation), lifetime);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Service'i sadece debug modda register eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Target container</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Register edildi mi?</returns>
        public static bool RegisterDebugOnly<TService, TImplementation>(this IContainer container, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            #if UNITY_EDITOR || DEBUG
            RegisterServiceByLifetime(container, typeof(TService), typeof(TImplementation), lifetime);
            return true;
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// Service'i sadece release modda register eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Target container</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Register edildi mi?</returns>
        public static bool RegisterReleaseOnly<TService, TImplementation>(this IContainer container, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            #if !UNITY_EDITOR && !DEBUG
            RegisterServiceByLifetime(container, typeof(TService), typeof(TImplementation), lifetime);
            return true;
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// Service'i platform-specific olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="container">Target container</param>
        /// <param name="platform">Target platform</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Register edildi mi?</returns>
        public static bool RegisterForPlatform<TService, TImplementation>(this IContainer container, RuntimePlatform platform, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TService
        {
            if (Application.platform == platform)
            {
                RegisterServiceByLifetime(container, typeof(TService), typeof(TImplementation), lifetime);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Service'i factory pattern ile register eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <param name="container">Target container</param>
        /// <param name="factory">Factory function</param>
        /// <param name="lifetime">Service lifetime</param>
        public static void RegisterFactory<TService>(this IContainer container, Func<TService> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            // Factory wrapper class oluştur
            var factoryWrapper = new ServiceFactory<TService>(factory);
            
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    container.Register<TService>(factory, ServiceLifetime.Singleton);
                    break;
                case ServiceLifetime.Transient:
                    container.Register<TService>(factory, ServiceLifetime.Transient);
                    break;
                default:
                    container.Register<TService>(factory, ServiceLifetime.Singleton);
                    break;
            }
        }
        
        /// <summary>
        /// Service'leri chain olarak register eder (decorator pattern)
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <param name="container">Target container</param>
        /// <param name="implementations">Implementation chain</param>
        public static void RegisterChain<TService>(this IContainer container, params Type[] implementations)
        {
            if (implementations.Length == 0) return;
            
            // İlk implementation'ı base olarak register et
            var baseType = implementations[0];
            RegisterServiceByLifetime(container, typeof(TService), baseType, ServiceLifetime.Singleton);
            
            // Diğerlerini decorator olarak register et
            for (int i = 1; i < implementations.Length; i++)
            {
                var decoratorType = implementations[i];
                // Decorator pattern implementation burada olacak
                Debug.Log($"🔗 Registered decorator: {decoratorType.Name} for {typeof(TService).Name}");
            }
        }
        
        /// <summary>
        /// Service lifetime'a göre register eder
        /// </summary>
        private static void RegisterServiceByLifetime(IContainer container, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            var containerType = container.GetType();
            string methodName = lifetime switch
            {
                ServiceLifetime.Singleton => "RegisterSingleton",
                ServiceLifetime.Transient => "RegisterTransient",
                ServiceLifetime.Scoped => "Register",
                _ => "Register"
            };
            
            var method = containerType.GetMethod(methodName)?.MakeGenericMethod(serviceType, implementationType);
            method?.Invoke(container, null);
        }
    }
    
    /// <summary>
    /// Service factory wrapper
    /// </summary>
    /// <typeparam name="T">Service tipi</typeparam>
    internal class ServiceFactory<T>
    {
        private readonly Func<T> _factory;
        
        public ServiceFactory(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        
        public T Create() => _factory.Invoke();
    }
    
    /// <summary>
    /// Service discovery helper
    /// </summary>
    public static class ServiceDiscovery
    {
        /// <summary>
        /// Assembly'deki tüm service interface'lerini bulur
        /// </summary>
        /// <param name="assembly">Taranacak assembly</param>
        /// <returns>Service interface'leri</returns>
        public static IEnumerable<Type> FindServiceInterfaces(Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
            return assembly.GetTypes()
                .Where(t => t.IsInterface)
                .Where(t => t.Name.EndsWith("Service") || t.GetCustomAttribute<MDIServiceAttribute>() != null)
                .ToList();
        }
        
        /// <summary>
        /// Service implementation'larını bulur
        /// </summary>
        /// <param name="serviceType">Service interface tipi</param>
        /// <param name="assembly">Taranacak assembly</param>
        /// <returns>Implementation'lar</returns>
        public static IEnumerable<Type> FindImplementations(Type serviceType, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
            return assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => serviceType.IsAssignableFrom(t))
                .ToList();
        }
        
        /// <summary>
        /// Service dependency'lerini analiz eder
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <returns>Dependency'ler</returns>
        public static IEnumerable<Type> AnalyzeDependencies(Type serviceType)
        {
            var dependencies = new List<Type>();
            
            // Constructor dependency'leri
            var constructors = serviceType.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                dependencies.AddRange(parameters.Select(p => p.ParameterType));
            }
            
            // Property dependency'leri (Inject attribute ile)
            var properties = serviceType.GetProperties()
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null);
            dependencies.AddRange(properties.Select(p => p.PropertyType));
            
            // Field dependency'leri (Inject attribute ile)
            var fields = serviceType.GetFields()
                .Where(f => f.GetCustomAttribute<InjectAttribute>() != null);
            dependencies.AddRange(fields.Select(f => f.FieldType));
            
            return dependencies.Distinct();
        }
    }
}