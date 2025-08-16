using System;
using UnityEngine;
using MDI.Core;

namespace MDI.Attributes
{
    /// <summary>
    /// MonoBehaviour'ın otomatik olarak MDI+ ile bootstrap edilmesini sağlar
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class MDIAutoBootstrapAttribute : Attribute
    {
        /// <summary>
        /// Bootstrap önceliği (düşük sayı = önce bootstrap edilir)
        /// </summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// Scene injection yapılsın mı?
        /// </summary>
        public bool InjectScene { get; set; } = true;
        
        /// <summary>
        /// Monitoring etkinleştirilsin mi?
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;
        
        /// <summary>
        /// Health check etkinleştirilsin mi?
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;
        
        /// <summary>
        /// Logging etkinleştirilsin mi?
        /// </summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>
        /// DontDestroyOnLoad uygulanacak mı?
        /// </summary>
        public bool DontDestroyOnLoad { get; set; } = true;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public MDIAutoBootstrapAttribute()
        {
        }
        
        /// <summary>
        /// Constructor with priority
        /// </summary>
        /// <param name="priority">Bootstrap önceliği</param>
        public MDIAutoBootstrapAttribute(int priority)
        {
            Priority = priority;
        }
    }
    
    /// <summary>
    /// Service'in otomatik olarak register edilmesini sağlar
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MDIAutoRegisterAttribute : Attribute
    {
        /// <summary>
        /// Service interface tipi
        /// </summary>
        public Type ServiceType { get; set; }
        
        /// <summary>
        /// Service yaşam süresi
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
        
        /// <summary>
        /// Service önceliği
        /// </summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// Service adı
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Service açıklaması
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceType">Service interface tipi</param>
        public MDIAutoRegisterAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }
        
        /// <summary>
        /// Constructor with lifetime
        /// </summary>
        /// <param name="serviceType">Service interface tipi</param>
        /// <param name="lifetime">Service yaşam süresi</param>
        public MDIAutoRegisterAttribute(Type serviceType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
        }
    }
    
    /// <summary>
    /// Service'in lazy loading ile yüklenmesini sağlar
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MDILazyLoadAttribute : Attribute
    {
        /// <summary>
        /// Lazy loading delay (milliseconds)
        /// </summary>
        public int DelayMs { get; set; } = 0;
        
        /// <summary>
        /// Condition method name (static bool method)
        /// </summary>
        public string ConditionMethod { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public MDILazyLoadAttribute()
        {
        }
        
        /// <summary>
        /// Constructor with delay
        /// </summary>
        /// <param name="delayMs">Delay in milliseconds</param>
        public MDILazyLoadAttribute(int delayMs)
        {
            DelayMs = delayMs;
        }
    }
    
    /// <summary>
    /// Service'in conditional olarak register edilmesini sağlar
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MDIConditionalAttribute : Attribute
    {
        /// <summary>
        /// Condition method name (static bool method)
        /// </summary>
        public string ConditionMethod { get; set; }
        
        /// <summary>
        /// Platform condition
        /// </summary>
        public RuntimePlatform[] Platforms { get; set; }
        
        /// <summary>
        /// Build condition (Debug/Release)
        /// </summary>
        public bool? IsDebugBuild { get; set; }
        
        /// <summary>
        /// Editor condition
        /// </summary>
        public bool? IsEditor { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conditionMethod">Condition method name</param>
        public MDIConditionalAttribute(string conditionMethod)
        {
            ConditionMethod = conditionMethod;
        }
        
        /// <summary>
        /// Constructor for platform condition
        /// </summary>
        /// <param name="platforms">Target platforms</param>
        public MDIConditionalAttribute(params RuntimePlatform[] platforms)
        {
            Platforms = platforms;
        }
        
        /// <summary>
        /// Constructor for debug condition
        /// </summary>
        /// <param name="isDebugBuild">Debug build condition</param>
        public MDIConditionalAttribute(bool isDebugBuild)
        {
            IsDebugBuild = isDebugBuild;
        }
    }
}