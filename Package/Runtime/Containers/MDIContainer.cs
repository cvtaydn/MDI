using System;
using System.Collections.Generic;
using System.Linq;
using MDI.Core;

namespace MDI.Containers
{
    /// <summary>
    /// MDI+ Dependency Injection Container
    /// SOLID prensiplerine uygun olarak tasarlanmış
    /// </summary>
    public class MDIContainer : IContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services;
        private readonly Dictionary<Type, object> _singletons;
        private readonly Dictionary<Type, object> _scopedInstances;
        private bool _disposed;

        /// <summary>
        /// Constructor
        /// </summary>
        public MDIContainer()
        {
            _services = new Dictionary<Type, ServiceDescriptor>();
            _singletons = new Dictionary<Type, object>();
            _scopedInstances = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Service'i container'a register eder (default: Transient)
        /// </summary>
        public IContainer Register<TService, TImplementation>() 
            where TImplementation : class, TService
        {
            return Register<TService, TImplementation>(ServiceLifetime.Transient);
        }

        /// <summary>
        /// Service'i belirtilen lifetime ile register eder
        /// </summary>
        public IContainer Register<TService, TImplementation>(ServiceLifetime lifetime) 
            where TImplementation : class, TService
        {
            ThrowIfDisposed();
            
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);

            // Interface constraint kontrolü
            if (!implementationType.IsAssignableFrom(implementationType))
            {
                throw new InvalidOperationException(
                    $"Type {implementationType.Name} does not implement {serviceType.Name}");
            }

            var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
            _services[serviceType] = descriptor;

            return this; // Fluent API için
        }

        /// <summary>
        /// Service'i singleton olarak register eder
        /// </summary>
        public IContainer RegisterSingleton<TService, TImplementation>() 
            where TImplementation : class, TService
        {
            return Register<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Service'i transient olarak register eder
        /// </summary>
        public IContainer RegisterTransient<TService, TImplementation>() 
            where TImplementation : class, TService
        {
            return Register<TService, TImplementation>(ServiceLifetime.Transient);
        }

        /// <summary>
        /// Service'i resolve eder
        /// </summary>
        public TService Resolve<TService>() where TService : class
        {
            ThrowIfDisposed();
            
            var serviceType = typeof(TService);
            return (TService)Resolve(serviceType);
        }

        /// <summary>
        /// Service'i type ile resolve eder
        /// </summary>
        public object Resolve(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException(
                    $"Service of type {serviceType.Name} is not registered");
            }

            return CreateInstance(descriptor);
        }

        /// <summary>
        /// Service instance'ını oluşturur
        /// </summary>
        private object CreateInstance(ServiceDescriptor descriptor)
        {
            // Singleton instance zaten varsa onu döndür
            if (descriptor.Lifetime == ServiceLifetime.Singleton && _singletons.TryGetValue(descriptor.ServiceType, out var singleton))
            {
                return singleton;
            }

            // Factory-based creation
            if (descriptor.Factory != null)
            {
                var instance = descriptor.Factory();
                StoreInstance(descriptor, instance);
                return instance;
            }

            // Instance-based creation
            if (descriptor.Instance != null)
            {
                StoreInstance(descriptor, descriptor.Instance);
                return descriptor.Instance;
            }

            // Constructor-based creation
            var newInstance = CreateInstanceFromConstructor(descriptor);
            StoreInstance(descriptor, newInstance);
            return newInstance;
        }

        /// <summary>
        /// Constructor'dan instance oluşturur
        /// </summary>
        private object CreateInstanceFromConstructor(ServiceDescriptor descriptor)
        {
            var implementationType = descriptor.ImplementationType;
            var constructors = implementationType.GetConstructors();

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {implementationType.Name} has no public constructor");
            }

            // En çok parametreli constructor'ı seç
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            // Constructor parametrelerini resolve et
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                parameterInstances[i] = Resolve(parameterType);
            }

            return Activator.CreateInstance(implementationType, parameterInstances);
        }

        /// <summary>
        /// Instance'ı uygun storage'a kaydeder
        /// </summary>
        private void StoreInstance(ServiceDescriptor descriptor, object instance)
        {
            switch (descriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    _singletons[descriptor.ServiceType] = instance;
                    break;
                case ServiceLifetime.Scoped:
                    _scopedInstances[descriptor.ServiceType] = instance;
                    break;
                case ServiceLifetime.Transient:
                    // Transient instance'lar kaydedilmez
                    break;
            }
        }

        /// <summary>
        /// Service'in register edilip edilmediğini kontrol eder
        /// </summary>
        public bool IsRegistered<TService>() where TService : class
        {
            return _services.ContainsKey(typeof(TService));
        }

        /// <summary>
        /// Tüm registered service'leri temizler
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            
            _services.Clear();
            _singletons.Clear();
            _scopedInstances.Clear();
        }

        /// <summary>
        /// Scope'u temizler (scoped instance'ları siler)
        /// </summary>
        public void ClearScope()
        {
            ThrowIfDisposed();
            _scopedInstances.Clear();
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Singleton'ları dispose et
                foreach (var singleton in _singletons.Values)
                {
                    if (singleton is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                // Scoped instance'ları dispose et
                foreach (var scoped in _scopedInstances.Values)
                {
                    if (scoped is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposed kontrolü
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MDIContainer));
            }
        }
    }
}
