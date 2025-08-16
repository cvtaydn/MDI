using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using MDI.Core;
using MDI.Attributes;
using MDI.Bootstrap;
using MDI.Containers;
using MDI.Extensions;

namespace MDI.Editor.Validation
{
    /// <summary>
    /// Real-time dependency validation system for MDI+
    /// </summary>
    public class MDIDependencyValidator
    {
        private static MDIDependencyValidator _instance;
        public static MDIDependencyValidator Instance => _instance ??= new MDIDependencyValidator();

        private readonly Dictionary<Type, List<Type>> _dependencyGraph = new Dictionary<Type, List<Type>>();
        private readonly Dictionary<Type, ValidationResult> _validationCache = new Dictionary<Type, ValidationResult>();
        private readonly HashSet<Type> _registeredServices = new HashSet<Type>();

        public event Action<ValidationResult> OnValidationCompleted;

        /// <summary>
        /// Validates all dependencies in the current container
        /// </summary>
        public ValidationResult ValidateAll()
        {
            var result = new ValidationResult();

            try
            {
                // Clear cache
                _validationCache.Clear();
                _dependencyGraph.Clear();
                _registeredServices.Clear();

                // Find current container
                var container = FindCurrentContainer();
                if (container == null)
                {
                    result.AddError("No active MDI container found");
                    return result;
                }

                // Build dependency graph
                BuildDependencyGraph(container);

                // Validate circular dependencies
                ValidateCircularDependencies(result);

                // Validate missing dependencies
                ValidateMissingDependencies(result);

                // Validate service lifetimes
                ValidateServiceLifetimes(result);

                // Validate injection attributes
                ValidateInjectionAttributes(result);

                OnValidationCompleted?.Invoke(result);
            }
            catch (Exception ex)
            {
                result.AddError($"Validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates a specific service type
        /// </summary>
        public ValidationResult ValidateService(Type serviceType)
        {
            if (_validationCache.TryGetValue(serviceType, out var cachedResult))
            {
                return cachedResult;
            }

            var result = new ValidationResult();

            try
            {
                var container = FindCurrentContainer();
                if (container == null)
                {
                    result.AddError("No active MDI container found");
                    return result;
                }

                // Check if service is registered
                if (!IsServiceRegistered(container, serviceType))
                {
                    result.AddError($"Service {serviceType.Name} is not registered");
                }

                // Validate dependencies
                var dependencies = GetServiceDependencies(serviceType);
                foreach (var dependency in dependencies)
                {
                    if (!IsServiceRegistered(container, dependency))
                    {
                        result.AddError($"Dependency {dependency.Name} for service {serviceType.Name} is not registered");
                    }
                }

                // Check for circular dependencies
                var visited = new HashSet<Type>();
                var recursionStack = new HashSet<Type>();

                if (HasCircularDependency(serviceType, visited, recursionStack))
                {
                    result.AddError($"Circular dependency detected for service {serviceType.Name}");
                }

                _validationCache[serviceType] = result;
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to validate service {serviceType.Name}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates MonoBehaviour injection points
        /// </summary>
        public ValidationResult ValidateMonoBehaviour(MonoBehaviour monoBehaviour)
        {
            var result = new ValidationResult();

            try
            {
                var container = FindCurrentContainer();
                if (container == null)
                {
                    result.AddWarning("No active MDI container found for MonoBehaviour validation");
                    return result;
                }

                var type = monoBehaviour.GetType();
                var injectableFields = GetInjectableFields(type);

                foreach (var field in injectableFields)
                {
                    var fieldType = field.FieldType;
                    var isRegistered = IsServiceRegistered(container, fieldType);
                    
                    Debug.Log($"[MDI+ Validator] Checking field '{field.Name}' of type {fieldType.Name} in {type.Name}: {(isRegistered ? "REGISTERED" : "NOT REGISTERED")}");

                    if (!isRegistered)
                    {
                        result.AddError($"Injectable field '{field.Name}' of type {fieldType.Name} in {type.Name} is not registered");
                    }
                }

                var injectableProperties = GetInjectableProperties(type);

                foreach (var property in injectableProperties)
                {
                    var propertyType = property.PropertyType;

                    if (!IsServiceRegistered(container, propertyType))
                    {
                        result.AddError($"Injectable property '{property.Name}' of type {propertyType.Name} in {type.Name} is not registered");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to validate MonoBehaviour {monoBehaviour.GetType().Name}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Real-time validation during play mode
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeRealTimeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Start real-time validation
                EditorApplication.update += Instance.PerformRealTimeValidation;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                // Stop real-time validation
                EditorApplication.update -= Instance.PerformRealTimeValidation;
            }
        }

        private void PerformRealTimeValidation()
        {
            // Throttle validation to avoid performance issues
            if (Time.realtimeSinceStartup % 2f < 0.1f) // Every 2 seconds
            {
                var result = ValidateAll();

                if (result.HasErrors)
                {
                    foreach (var error in result.Errors)
                    {
                        Debug.LogError($"[MDI+ Validation] {error}");
                    }
                }

                if (result.HasWarnings)
                {
                    foreach (var warning in result.Warnings)
                    {
                        Debug.LogWarning($"[MDI+ Validation] {warning}");
                    }
                }
            }
        }

        private void BuildDependencyGraph(MDIContainer container)
        {
            // Get all registered services using reflection
            var containerType = container.GetType();
            var servicesField = containerType.GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance);

            if (servicesField?.GetValue(container) is Dictionary<Type, object> services)
            {
                foreach (var serviceType in services.Keys)
                {
                    _registeredServices.Add(serviceType);
                    var dependencies = GetServiceDependencies(serviceType);
                    _dependencyGraph[serviceType] = dependencies;
                }
            }
        }

        private void ValidateCircularDependencies(ValidationResult result)
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var serviceType in _registeredServices)
            {
                if (!visited.Contains(serviceType))
                {
                    if (HasCircularDependency(serviceType, visited, recursionStack))
                    {
                        result.AddError($"Circular dependency detected involving service {serviceType.Name}");
                    }
                }
            }
        }

        private void ValidateMissingDependencies(ValidationResult result)
        {
            foreach (var kvp in _dependencyGraph)
            {
                var serviceType = kvp.Key;
                var dependencies = kvp.Value;

                foreach (var dependency in dependencies)
                {
                    if (!_registeredServices.Contains(dependency))
                    {
                        result.AddError($"Service {serviceType.Name} depends on {dependency.Name} which is not registered");
                    }
                }
            }
        }

        private void ValidateServiceLifetimes(ValidationResult result)
        {
            // Check for potential lifetime mismatches
            foreach (var serviceType in _registeredServices)
            {
                var dependencies = GetServiceDependencies(serviceType);

                foreach (var dependency in dependencies)
                {
                    // Add lifetime validation logic here
                    // For example: Singleton depending on Transient might be problematic
                }
            }
        }

        private void ValidateInjectionAttributes(ValidationResult result)
        {
            // Find all MonoBehaviours with [Inject] attributes
            var monoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

            foreach (var mb in monoBehaviours)
            {
                var mbResult = ValidateMonoBehaviour(mb);
                result.Merge(mbResult);
            }
        }

        private bool HasCircularDependency(Type serviceType, HashSet<Type> visited, HashSet<Type> recursionStack)
        {
            visited.Add(serviceType);
            recursionStack.Add(serviceType);

            if (_dependencyGraph.TryGetValue(serviceType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        if (HasCircularDependency(dependency, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(serviceType);
            return false;
        }

        private List<Type> GetServiceDependencies(Type serviceType)
        {
            var dependencies = new List<Type>();

            // Get constructor dependencies
            var constructors = serviceType.GetConstructors();
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (constructor != null)
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    dependencies.Add(parameter.ParameterType);
                }
            }

            // Get field dependencies
            var injectableFields = GetInjectableFields(serviceType);
            dependencies.AddRange(injectableFields.Select(f => f.FieldType));

            // Get property dependencies
            var injectableProperties = GetInjectableProperties(serviceType);
            dependencies.AddRange(injectableProperties.Select(p => p.PropertyType));

            return dependencies.Distinct().ToList();
        }

        private FieldInfo[] GetInjectableFields(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<InjectAttribute>() != null)
                .ToArray();
        }

        private PropertyInfo[] GetInjectableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null)
                .ToArray();
        }

        private bool IsServiceRegistered(MDIContainer container, Type serviceType)
        {
            try
            {
                Debug.Log($"[MDI+ Validator] Checking if {serviceType.Name} is registered in container");
                
                // Use the Type-based IsRegistered method directly to avoid ambiguous match
                var result = container.IsRegistered(serviceType);
                Debug.Log($"[MDI+ Validator] IsRegistered result for {serviceType.Name}: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MDI+ Validator] Exception in IsServiceRegistered for {serviceType.Name}: {ex.Message}");
                
                // Fallback: try to resolve using Type-based method
                try
                {
                    Debug.Log($"[MDI+ Validator] Trying fallback resolve for {serviceType.Name}");
                    var resolvedService = container.Resolve(serviceType);
                    Debug.Log($"[MDI+ Validator] Fallback resolve successful for {serviceType.Name}");
                    return resolvedService != null;
                }
                catch (Exception fallbackEx)
                {
                    Debug.Log($"[MDI+ Validator] Fallback resolve failed for {serviceType.Name}: {fallbackEx.Message}");
                    return false;
                }
            }
        }

        private MDIContainer FindCurrentContainer()
        {
            // Try to get global container first (most reliable)
            var globalContainerProperty = typeof(MDI.Core.MDI).GetProperty("GlobalContainer", BindingFlags.Public | BindingFlags.Static);
            if (globalContainerProperty?.GetValue(null) is MDIContainer globalContainer)
            {
                Debug.Log($"[MDI+ Validator] Global container found with {globalContainer.GetRegisteredServiceTypes().Length} services (HashCode: {globalContainer.GetHashCode()})");
                var registeredTypes = globalContainer.GetRegisteredServiceTypes();
                Debug.Log($"[MDI+ Validator] Registered service types: {string.Join(", ", registeredTypes.Select(t => t.Name))}");
                return globalContainer;
            }
            else
            {
                Debug.Log("[MDI+ Validator] Global container not found or empty");
            }

            // Try to find MDIBootstrapper in scene
            var mdiBootstrapper = UnityEngine.Object.FindObjectOfType<MDIBootstrapper>();
            if (mdiBootstrapper != null)
            {
                var containerField = mdiBootstrapper.GetType().GetField("_container", BindingFlags.NonPublic | BindingFlags.Instance);
                if (containerField?.GetValue(mdiBootstrapper) is MDIContainer container)
                {
                    return container;
                }
            }

            // Try to find any bootstrapper (including GameBootstrapper) in scene
            var allBootstrappers = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()
                .Where(mb => mb.GetType().Name.Contains("Bootstrap"));
            
            foreach (var bootstrapper in allBootstrappers)
            {
                // Look for container field
                var containerField = bootstrapper.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(f => f.FieldType == typeof(MDIContainer));
                
                if (containerField?.GetValue(bootstrapper) is MDIContainer container)
                {
                    return container;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Infos { get; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool HasInfos => Infos.Count > 0;
        public bool IsValid => !HasErrors;

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        public void AddInfo(string message) => Infos.Add(message);

        public void Merge(ValidationResult other)
        {
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            Infos.AddRange(other.Infos);
        }

        public override string ToString()
        {
            var result = new System.Text.StringBuilder();

            if (HasErrors)
            {
                result.AppendLine("Errors:");
                foreach (var error in Errors)
                {
                    result.AppendLine($"  - {error}");
                }
            }

            if (HasWarnings)
            {
                result.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                {
                    result.AppendLine($"  - {warning}");
                }
            }

            if (HasInfos)
            {
                result.AppendLine("Info:");
                foreach (var info in Infos)
                {
                    result.AppendLine($"  - {info}");
                }
            }

            return result.ToString();
        }
    }
}