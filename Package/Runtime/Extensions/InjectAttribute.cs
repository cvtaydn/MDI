using System;
using UnityEngine;

namespace MDI.Extensions
{
    /// <summary>
    /// Unity component'lerine dependency injection yapmak için kullanılan attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class InjectAttribute : PropertyAttribute
    {
        /// <summary>
        /// Service tipi (opsiyonel, field/property tipinden otomatik çıkarılır)
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Service adı (opsiyonel, named service için)
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Injection'ın opsiyonel olup olmadığı (varsayılan: false)
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Service ID'si (opsiyonel, named service için)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public InjectAttribute()
        {
        }

        /// <summary>
        /// Service tipi ile constructor
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        public InjectAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }

        /// <summary>
        /// Service adı ile constructor
        /// </summary>
        /// <param name="serviceName">Service adı</param>
        public InjectAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }

        /// <summary>
        /// Service tipi ve adı ile constructor
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <param name="serviceName">Service adı</param>
        public InjectAttribute(Type serviceType, string serviceName)
        {
            ServiceType = serviceType;
            ServiceName = serviceName;
        }
    }
}
