using System;
using System.Collections.Generic;

namespace MDI.Core
{
    /// <summary>
    /// Temel dependency injection container interface'i
    /// SOLID prensiplerine uygun olarak tasarlanmış
    /// </summary>
    public interface IContainer : IDisposable
    {
        /// <summary>
        /// Service'i container'a register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <returns>Container instance (fluent API için)</returns>
        IContainer Register<TService, TImplementation>() 
            where TImplementation : class, TService;

        /// <summary>
        /// Service'i singleton olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <returns>Container instance (fluent API için)</returns>
        IContainer RegisterSingleton<TService, TImplementation>() 
            where TImplementation : class, TService;

        /// <summary>
        /// Service'i transient olarak register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <returns>Container instance (fluent API için)</returns>
        IContainer RegisterTransient<TService, TImplementation>() 
            where TImplementation : class, TService;
            
        /// <summary>
        /// Service'i güvenli şekilde resolve eder (null döndürür hata fırlatmaz)
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <returns>Service instance veya null</returns>
        TService TryResolve<TService>() where TService : class;
        
        /// <summary>
        /// Service'i güvenli şekilde type ile resolve eder
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <returns>Service instance veya null</returns>
        object TryResolve(Type serviceType);
        
        /// <summary>
        /// Service'in register edilip edilmediğini kontrol eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <returns>Register edilmişse true</returns>
        bool IsRegistered<TService>() where TService : class;
        
        /// <summary>
        /// Service'in register edilip edilmediğini type ile kontrol eder
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <returns>Register edilmişse true</returns>
        bool IsRegistered(Type serviceType);
        
        /// <summary>
        /// Birden fazla service'i aynı anda resolve eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <returns>Service instance'ları</returns>
        TService[] ResolveAll<TService>() where TService : class;
        
        /// <summary>
        /// Service'i unregister eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <returns>Başarılıysa true</returns>
        bool UnregisterService<TService>();
        
        /// <summary>
        /// Service'i type ile unregister eder
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <returns>Başarılıysa true</returns>
        bool UnregisterService(Type serviceType);
        
        /// <summary>
        /// Tüm servisleri temizler
        /// </summary>
        void Clear();

        /// <summary>
        /// Service'i priority ve execution order ile register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="priority">Service önceliği</param>
        /// <param name="executionOrder">Başlatılma sırası</param>
        /// <param name="name">Service adı</param>
        /// <returns>Container instance (fluent API için)</returns>
        IContainer RegisterWithOrder<TService, TImplementation>(int priority = 0, int executionOrder = 0, string name = null) 
            where TImplementation : class, TService;

        /// <summary>
        /// Service'i singleton olarak priority ve execution order ile register eder
        /// </summary>
        /// <typeparam name="TService">Service interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        /// <param name="priority">Service önceliği</param>
        /// <param name="executionOrder">Başlatılma sırası</param>
        /// <param name="name">Service adı</param>
        /// <returns>Container instance (fluent API için)</returns>
        IContainer RegisterSingletonWithOrder<TService, TImplementation>(int priority = 0, int executionOrder = 0, string name = null) 
            where TImplementation : class, TService;

        /// <summary>
        /// Tüm servisleri execution order'a göre sıralı şekilde başlatır
        /// </summary>
        void InitializeServicesInOrder();

        /// <summary>
        /// Service'i resolve eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <returns>Service instance'ı</returns>
        TService Resolve<TService>() where TService : class;

        /// <summary>
        /// Service'i type ile resolve eder
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <returns>Service instance'ı</returns>
        object Resolve(Type serviceType);

        /// <summary>
        /// Service'i factory ile register eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <param name="factory">Factory function</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Container instance (fluent API için)</returns>
        IContainer Register<TService>(Func<TService> factory, ServiceLifetime lifetime) where TService : class;

    }
}
