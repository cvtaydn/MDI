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
        private readonly DependencyGraph _dependencyGraph;
        private readonly ServiceMonitor _serviceMonitor;
        private readonly HealthChecker _healthChecker;
        private bool _disposed;

        /// <summary>
        /// Constructor
        /// </summary>
        public MDIContainer()
        {
            _services = new Dictionary<Type, ServiceDescriptor>();
            _singletons = new Dictionary<Type, object>();
            _scopedInstances = new Dictionary<Type, object>();
            _dependencyGraph = new DependencyGraph();
            _serviceMonitor = new ServiceMonitor();
            _healthChecker = new HealthChecker(this);
        }
        
        /// <summary>
        /// Dependency graph'a erişim
        /// </summary>
        public DependencyGraph DependencyGraph => _dependencyGraph;
        
        /// <summary>
        /// Service monitor'a erişim
        /// </summary>
        public ServiceMonitor ServiceMonitor => _serviceMonitor;
        
        /// <summary>
        /// Health checker'a erişim
        /// </summary>
        public HealthChecker HealthChecker => _healthChecker;

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
            
            // Dependency graph'a service'i ekle
            _dependencyGraph.AddService(serviceType, implementationType.Name);
            
            // Constructor dependency'lerini analiz et ve ekle
            AnalyzeAndAddDependencies(serviceType, implementationType);

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
        /// Service'i priority ve execution order ile register eder
        /// </summary>
        public IContainer RegisterWithOrder<TService, TImplementation>(int priority = 0, int executionOrder = 0, string name = null) 
            where TImplementation : class, TService
        {
            ThrowIfDisposed();
            
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);

            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient)
            {
                Priority = priority,
                ExecutionOrder = executionOrder,
                Name = name ?? implementationType.Name
            };
            
            _services[serviceType] = descriptor;
            
            // Dependency graph'a service'i ekle
            _dependencyGraph.AddService(serviceType, name ?? implementationType.Name);
            
            // Constructor dependency'lerini analiz et ve ekle
            AnalyzeAndAddDependencies(serviceType, implementationType);
            
            return this;
        }

        /// <summary>
        /// Service'i singleton olarak priority ve execution order ile register eder
        /// </summary>
        public IContainer RegisterSingletonWithOrder<TService, TImplementation>(int priority = 0, int executionOrder = 0, string name = null) 
            where TImplementation : class, TService
        {
            ThrowIfDisposed();
            
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);

            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton)
            {
                Priority = priority,
                ExecutionOrder = executionOrder,
                Name = name ?? implementationType.Name
            };
            
            _services[serviceType] = descriptor;
            return this;
        }

        /// <summary>
        /// Tüm servisleri execution order'a göre sıralı şekilde başlatır
        /// </summary>
        public void InitializeServicesInOrder()
        {
            ThrowIfDisposed();
            
            // Servisleri önce priority'ye göre, sonra execution order'a göre sırala
            var orderedServices = _services.Values
                .OrderByDescending(s => s.Priority)
                .ThenBy(s => s.ExecutionOrder)
                .ToList();

            foreach (var descriptor in orderedServices)
            {
                try
                {
                    // Service'i resolve ederek başlat
                    Resolve(descriptor.ServiceType);
                    
                    // Debug log
                    UnityEngine.Debug.Log($"[MDI+] Service initialized: {descriptor.Name ?? descriptor.ServiceType.Name} (Priority: {descriptor.Priority}, Order: {descriptor.ExecutionOrder})");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[MDI+] Failed to initialize service {descriptor.Name ?? descriptor.ServiceType.Name}: {ex.Message}");
                    throw;
                }
            }
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

            // Dependency graph'da service access'i kaydet
            _dependencyGraph.RecordServiceAccess(serviceType);
            
            // Service monitor'da resolve başlangıcını kaydet
            _serviceMonitor.StartResolve(serviceType, descriptor.Name);
            
            try
            {
                _dependencyGraph.UpdateServiceStatus(serviceType, ServiceStatus.Initializing);
                _serviceMonitor.UpdateStatus(serviceType, ServiceStatus.Initializing);
                
                var instance = CreateInstance(descriptor);
                
                _dependencyGraph.UpdateServiceStatus(serviceType, ServiceStatus.Initialized);
                _serviceMonitor.UpdateStatus(serviceType, ServiceStatus.Initialized);
                _serviceMonitor.EndResolve(serviceType, true);
                
                return instance;
            }
            catch (Exception ex)
            {
                _dependencyGraph.UpdateServiceStatus(serviceType, ServiceStatus.Error);
                _serviceMonitor.UpdateStatus(serviceType, ServiceStatus.Error);
                _serviceMonitor.EndResolve(serviceType, false, ex.Message);
                throw;
            }
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
            _dependencyGraph.Clear();
            _serviceMonitor.Clear();
            _healthChecker.Clear();
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
        /// Constructor dependency'lerini analiz eder ve dependency graph'a ekler
        /// </summary>
        private void AnalyzeAndAddDependencies(Type serviceType, Type implementationType)
        {
            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0) return;
            
            // En çok parametreli constructor'ı seç
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = constructor.GetParameters();
            
            foreach (var parameter in parameters)
            {
                var dependencyType = parameter.ParameterType;
                
                // Primitive type'ları ve string'i atla
                if (dependencyType.IsPrimitive || dependencyType == typeof(string))
                    continue;
                    
                // Dependency ilişkisini ekle
                _dependencyGraph.AddDependency(serviceType, dependencyType);
            }
        }
        
        /// <summary>
        /// Circular dependency kontrolü yapar
        /// </summary>
        public bool ValidateDependencies()
        {
            return !_dependencyGraph.DetectCircularDependencies();
        }
        
        /// <summary>
        /// Dependency tree'yi string olarak döndürür
        /// </summary>
        public string GetDependencyTree(Type serviceType)
        {
            return _dependencyGraph.GetDependencyTree(serviceType);
        }
        
        /// <summary>
        /// Service istatistiklerini döndürür
        /// </summary>
        public Dictionary<string, object> GetServiceStatistics()
        {
            return _dependencyGraph.GetStatistics();
        }
        
        /// <summary>
        /// Performance raporu oluşturur
        /// </summary>
        public string GeneratePerformanceReport()
        {
            return _serviceMonitor.GeneratePerformanceReport();
        }
        
        /// <summary>
        /// Belirli bir service için detaylı rapor
        /// </summary>
        public string GetServiceReport(Type serviceType)
        {
            return _serviceMonitor.GetServiceReport(serviceType);
        }
        
        /// <summary>
        /// Belirli bir service için detaylı rapor (generic)
        /// </summary>
        public string GetServiceReport<TService>()
        {
            return GetServiceReport(typeof(TService));
        }
        
        /// <summary>
        /// Service metrics'i JSON olarak export eder
        /// </summary>
        public string ExportMetricsAsJson()
        {
            return _serviceMonitor.ExportMetricsAsJson();
        }
        
        /// <summary>
        /// Health check sistemini başlatır
        /// </summary>
        public void StartHealthCheck()
        {
            ThrowIfDisposed();
            _healthChecker.StartAutoCheck();
        }
        
        /// <summary>
        /// Health check sistemini durdurur
        /// </summary>
        public void StopHealthCheck()
        {
            ThrowIfDisposed();
            _healthChecker.StopAutoCheck();
        }
        
        /// <summary>
        /// Tüm servislerin sağlık durumunu kontrol eder
        /// </summary>
        public async System.Threading.Tasks.Task<Dictionary<Type, HealthCheckResult>> CheckAllServicesHealthAsync()
        {
            ThrowIfDisposed();
            return await _healthChecker.CheckAllServicesHealthAsync();
        }
        
        /// <summary>
        /// Belirli bir servisin sağlık durumunu kontrol eder
        /// </summary>
        public async System.Threading.Tasks.Task<HealthCheckResult> CheckServiceHealthAsync<TService>()
        {
            ThrowIfDisposed();
            return await _healthChecker.CheckServiceHealthAsync(typeof(TService));
        }
        
        /// <summary>
        /// Genel sistem sağlık durumunu alır
        /// </summary>
        public HealthStatus GetOverallHealth()
        {
            ThrowIfDisposed();
            return _healthChecker.GetOverallHealth();
        }
        
        /// <summary>
        /// Sağlık raporu oluşturur
        /// </summary>
        public string GenerateHealthReport()
        {
            ThrowIfDisposed();
            return _healthChecker.GenerateHealthReport();
        }
        
        /// <summary>
        /// Container'ı günceller - health check ve monitoring için
        /// MonoBehaviour'da Update metodunda çağrılmalı
        /// </summary>
        public void Update()
        {
            ThrowIfDisposed();
            _healthChecker.Update();
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
