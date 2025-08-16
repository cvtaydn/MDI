using System;
using UnityEngine;

namespace MDI.Attributes
{
    /// <summary>
    /// MDI+ Service attribute - Inspector'da özel görünüm için
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public class MDIServiceAttribute : PropertyAttribute
    {
        /// <summary>
        /// Service tipi
        /// </summary>
        public Type ServiceType { get; }
        
        /// <summary>
        /// Otomatik resolve edilsin mi?
        /// </summary>
        public bool AutoResolve { get; }
        
        /// <summary>
        /// Service adı (named service için)
        /// </summary>
        public string ServiceName { get; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public MDIServiceAttribute()
        {
            ServiceType = null;
            AutoResolve = false;
            ServiceName = null;
        }
        
        /// <summary>
        /// Service tipi ile constructor
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <param name="autoResolve">Otomatik resolve edilsin mi?</param>
        public MDIServiceAttribute(Type serviceType, bool autoResolve = false)
        {
            ServiceType = serviceType;
            AutoResolve = autoResolve;
            ServiceName = null;
        }
        
        /// <summary>
        /// Service adı ile constructor
        /// </summary>
        /// <param name="serviceName">Service adı</param>
        /// <param name="autoResolve">Otomatik resolve edilsin mi?</param>
        public MDIServiceAttribute(string serviceName, bool autoResolve = false)
        {
            ServiceType = null;
            AutoResolve = autoResolve;
            ServiceName = serviceName;
        }
        
        /// <summary>
        /// Service tipi ve adı ile constructor
        /// </summary>
        /// <param name="serviceType">Service tipi</param>
        /// <param name="serviceName">Service adı</param>
        /// <param name="autoResolve">Otomatik resolve edilsin mi?</param>
        public MDIServiceAttribute(Type serviceType, string serviceName, bool autoResolve = false)
        {
            ServiceType = serviceType;
            AutoResolve = autoResolve;
            ServiceName = serviceName;
        }
    }
}