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
        /// Service'in container'da register edilip edilmediğini kontrol eder
        /// </summary>
        /// <typeparam name="TService">Service tipi</typeparam>
        /// <returns>True eğer register edilmişse</returns>
        bool IsRegistered<TService>() where TService : class;

        /// <summary>
        /// Tüm registered service'leri temizler
        /// </summary>
        void Clear();
    }
}
