using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using MDI.Core;
using MDI.Attributes;
using MDI.Containers;

namespace MDI.Bootstrap
{
    /// <summary>
    /// Otomatik service discovery ve registration sistemi
    /// </summary>
    public static class MDIServiceDiscovery
    {
        /// <summary>
        /// Assembly'deki tüm MDIAutoRegister attribute'lu sınıfları bulur ve register eder
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="assemblies">Taranacak assembly'ler (null ise tüm loaded assembly'ler)</param>
        public static void DiscoverAndRegisterServices(MDIContainerBuilder builder, Assembly[] assemblies = null)
        {
            if (builder == null)
            {
                Debug.LogError("❌ MDIServiceDiscovery: Builder null!");
                return;
            }
            
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            var discoveredServices = new List<ServiceRegistrationInfo>();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var services = DiscoverServicesInAssembly(assembly);
                    discoveredServices.AddRange(services);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Assembly tarama hatası ({assembly.GetName().Name}): {ex.Message}");
                }
            }
            
            // Önceliğe göre sırala ve register et
            var sortedServices = discoveredServices
                .Where(s => ShouldRegisterService(s))
                .OrderBy(s => s.Priority)
                .ToList();
            
            foreach (var serviceInfo in sortedServices)
            {
                try
                {
                    RegisterService(builder, serviceInfo);
                    Debug.Log($"✅ Auto-registered: {serviceInfo.ServiceType.Name} -> {serviceInfo.ImplementationType.Name} ({serviceInfo.Lifetime})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Auto-registration failed: {serviceInfo.ServiceType.Name} -> {serviceInfo.ImplementationType.Name}\nError: {ex.Message}");
                }
            }
            
            Debug.Log($"🔍 Service Discovery tamamlandı. {sortedServices.Count} service auto-registered.");
        }
        
        /// <summary>
        /// Belirli bir assembly'deki service'leri bulur
        /// </summary>
        /// <param name="assembly">Taranacak assembly</param>
        /// <returns>Bulunan service'ler</returns>
        private static List<ServiceRegistrationInfo> DiscoverServicesInAssembly(Assembly assembly)
        {
            var services = new List<ServiceRegistrationInfo>();
            
            try
            {
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    if (type.IsClass && !type.IsAbstract)
                    {
                        var autoRegisterAttr = type.GetCustomAttribute<MDIAutoRegisterAttribute>();
                        if (autoRegisterAttr != null)
                        {
                            var serviceInfo = CreateServiceRegistrationInfo(type, autoRegisterAttr);
                            if (serviceInfo != null)
                            {
                                services.Add(serviceInfo);
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Debug.LogWarning($"⚠️ Type loading hatası ({assembly.GetName().Name}): {ex.Message}");
                
                // Yüklenebilen type'ları al
                var loadedTypes = ex.Types.Where(t => t != null);
                foreach (var type in loadedTypes)
                {
                    if (type.IsClass && !type.IsAbstract)
                    {
                        var autoRegisterAttr = type.GetCustomAttribute<MDIAutoRegisterAttribute>();
                        if (autoRegisterAttr != null)
                        {
                            var serviceInfo = CreateServiceRegistrationInfo(type, autoRegisterAttr);
                            if (serviceInfo != null)
                            {
                                services.Add(serviceInfo);
                            }
                        }
                    }
                }
            }
            
            return services;
        }
        
        /// <summary>
        /// Service registration info oluşturur
        /// </summary>
        /// <param name="implementationType">Implementation type</param>
        /// <param name="attribute">Auto register attribute</param>
        /// <returns>Service registration info</returns>
        private static ServiceRegistrationInfo CreateServiceRegistrationInfo(Type implementationType, MDIAutoRegisterAttribute attribute)
        {
            try
            {
                var serviceType = attribute.ServiceType;
                
                // Service type belirtilmemişse, interface'leri bul
                if (serviceType == null)
                {
                    var interfaces = implementationType.GetInterfaces()
                        .Where(i => i != typeof(IDisposable) && !i.IsGenericType)
                        .ToArray();
                    
                    if (interfaces.Length == 1)
                    {
                        serviceType = interfaces[0];
                    }
                    else if (interfaces.Length > 1)
                    {
                        // İsim benzerliğine göre en uygun interface'i seç
                        serviceType = interfaces.FirstOrDefault(i => 
                            i.Name.EndsWith(implementationType.Name.Replace("Service", "").Replace("Manager", ""))) 
                            ?? interfaces[0];
                    }
                    else
                    {
                        // Interface yoksa, kendisini service type olarak kullan
                        serviceType = implementationType;
                    }
                }
                
                // Service type'ın implementation type'a assign edilebilir olduğunu kontrol et
                if (!serviceType.IsAssignableFrom(implementationType))
                {
                    Debug.LogError($"❌ {implementationType.Name} is not assignable to {serviceType.Name}");
                    return null;
                }
                
                return new ServiceRegistrationInfo
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    Lifetime = attribute.Lifetime,
                    Priority = attribute.Priority,
                    Name = attribute.Name ?? implementationType.Name,
                    Description = attribute.Description,
                    Attribute = attribute
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Service registration info oluşturma hatası ({implementationType.Name}): {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Service'in register edilip edilmeyeceğini kontrol eder
        /// </summary>
        /// <param name="serviceInfo">Service info</param>
        /// <returns>Register edilecekse true</returns>
        private static bool ShouldRegisterService(ServiceRegistrationInfo serviceInfo)
        {
            // Conditional attribute kontrolü
            var conditionalAttr = serviceInfo.ImplementationType.GetCustomAttribute<MDIConditionalAttribute>();
            if (conditionalAttr != null)
            {
                // Platform kontrolü
                if (conditionalAttr.Platforms != null && conditionalAttr.Platforms.Length > 0)
                {
                    if (!conditionalAttr.Platforms.Contains(Application.platform))
                    {
                        return false;
                    }
                }
                
                // Debug build kontrolü
                if (conditionalAttr.IsDebugBuild.HasValue)
                {
                    if (conditionalAttr.IsDebugBuild.Value != Debug.isDebugBuild)
                    {
                        return false;
                    }
                }
                
                // Editor kontrolü
                if (conditionalAttr.IsEditor.HasValue)
                {
                    if (conditionalAttr.IsEditor.Value != Application.isEditor)
                    {
                        return false;
                    }
                }
                
                // Custom condition method kontrolü
                if (!string.IsNullOrEmpty(conditionalAttr.ConditionMethod))
                {
                    try
                    {
                        var method = serviceInfo.ImplementationType.GetMethod(conditionalAttr.ConditionMethod, 
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        
                        if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                        {
                            var result = (bool)method.Invoke(null, null);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ Invalid condition method: {conditionalAttr.ConditionMethod} in {serviceInfo.ImplementationType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Condition method execution failed: {conditionalAttr.ConditionMethod} in {serviceInfo.ImplementationType.Name}\nError: {ex.Message}");
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Service'i container'a register eder
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="serviceInfo">Service info</param>
        private static void RegisterService(MDIContainerBuilder builder, ServiceRegistrationInfo serviceInfo)
        {
            // Reflection ile generic method çağır
            var builderType = typeof(MDIContainerBuilder);
            string methodName = serviceInfo.Lifetime switch
            {
                ServiceLifetime.Singleton => "AddSingleton",
                ServiceLifetime.Transient => "AddTransient",
                ServiceLifetime.Scoped => "AddScoped",
                _ => "AddTransient"
            };
            
            var method = builderType.GetMethod(methodName)?.MakeGenericMethod(serviceInfo.ServiceType, serviceInfo.ImplementationType);
            if (method != null)
            {
                method.Invoke(builder, null);
            }
            else
            {
                throw new InvalidOperationException($"Method not found: {methodName}<{serviceInfo.ServiceType.Name}, {serviceInfo.ImplementationType.Name}>");
            }
        }
        
        /// <summary>
        /// Assembly'deki tüm MDIAutoBootstrap attribute'lu MonoBehaviour'ları bulur
        /// </summary>
        /// <param name="assemblies">Taranacak assembly'ler</param>
        /// <returns>Bulunan bootstrap sınıfları</returns>
        public static List<Type> DiscoverBootstrapClasses(Assembly[] assemblies = null)
        {
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            var bootstrapClasses = new List<Type>();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    
                    foreach (var type in types)
                    {
                        if (type.IsSubclassOf(typeof(MonoBehaviour)) && 
                            type.GetCustomAttribute<MDIAutoBootstrapAttribute>() != null)
                        {
                            bootstrapClasses.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Bootstrap class discovery hatası ({assembly.GetName().Name}): {ex.Message}");
                }
            }
            
            // Önceliğe göre sırala
            return bootstrapClasses
                .OrderBy(t => t.GetCustomAttribute<MDIAutoBootstrapAttribute>()?.Priority ?? 0)
                .ToList();
        }
    }
    
    /// <summary>
    /// Service registration bilgileri
    /// </summary>
    internal class ServiceRegistrationInfo
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MDIAutoRegisterAttribute Attribute { get; set; }
    }
}