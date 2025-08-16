using System;

namespace MDI.Core
{
    /// <summary>
    /// Service'in nasıl oluşturulacağını ve yaşam süresini tanımlar
    /// </summary>
    public class ServiceDescriptor
    {
        /// <summary>
        /// Service interface tipi
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Service implementation tipi
        /// </summary>
        public Type ImplementationType { get; }

        /// <summary>
        /// Service'in yaşam süresi
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// Service'in başlatılma önceliği (yüksek sayı = yüksek öncelik)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Service'in başlatılma sırası (düşük sayı = önce başlatılır)
        /// </summary>
        public int ExecutionOrder { get; set; } = 0;

        /// <summary>
        /// Service'in adı (debugging ve tracking için)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Service'in açıklaması
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Factory function (eğer custom creation logic varsa)
        /// </summary>
        public Func<object> Factory { get; }

        /// <summary>
        /// Service instance'ı (singleton için)
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            Lifetime = lifetime;
        }

        /// <summary>
        /// Factory-based constructor
        /// </summary>
        public ServiceDescriptor(Type serviceType, Func<object> factory, ServiceLifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Lifetime = lifetime;
        }

        /// <summary>
        /// Instance-based constructor
        /// </summary>
        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Lifetime = ServiceLifetime.Singleton;
        }
    }
}
