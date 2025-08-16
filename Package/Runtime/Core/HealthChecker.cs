using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using MDI.Containers;

namespace MDI.Core
{
    /// <summary>
    /// Service health status
    /// </summary>
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Warning,
        Critical,
        Unhealthy
    }

    /// <summary>
    /// Health check result for a service
    /// </summary>
    public class HealthCheckResult
    {
        public Type ServiceType { get; set; }
        public string ServiceName { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime CheckTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public Exception Exception { get; set; }

        public bool IsHealthy => Status == HealthStatus.Healthy;
        public bool IsUnhealthy => Status == HealthStatus.Unhealthy || Status == HealthStatus.Critical;
    }

    /// <summary>
    /// Interface for services that can perform health checks
    /// </summary>
    public interface IHealthCheckable
    {
        Task<HealthCheckResult> CheckHealthAsync();
    }

    /// <summary>
    /// Health check configuration
    /// </summary>
    public class HealthCheckConfig
    {
        public bool EnableAutoCheck { get; set; } = true;
        public float CheckInterval { get; set; } = 30f; // seconds
        public int MaxRetries { get; set; } = 3;
        public float RetryDelay { get; set; } = 1f; // seconds
        public float TimeoutSeconds { get; set; } = 10f;
        public bool LogResults { get; set; } = true;
        public bool AlertOnFailure { get; set; } = true;
    }

    /// <summary>
    /// Service health checker - monitors service health and detects issues
    /// </summary>
    public class HealthChecker
    {
        private readonly MDIContainer _container;
        private readonly HealthCheckConfig _config;
        private readonly Dictionary<Type, HealthCheckResult> _lastResults;
        private readonly Dictionary<Type, DateTime> _lastCheckTimes;
        private readonly Dictionary<Type, int> _retryCounters;
        private readonly List<string> _healthLogs;
        
        private bool _isRunning;
        private float _lastAutoCheckTime;

        public IReadOnlyDictionary<Type, HealthCheckResult> LastResults => _lastResults;
        public IReadOnlyList<string> HealthLogs => _healthLogs;
        public HealthCheckConfig Config => _config;
        public bool IsRunning => _isRunning;

        public event Action<HealthCheckResult> OnHealthCheckCompleted;
        public event Action<Type, HealthCheckResult> OnServiceHealthChanged;
        public event Action<Type, Exception> OnHealthCheckFailed;

        public HealthChecker(MDIContainer container, HealthCheckConfig config = null)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _config = config ?? new HealthCheckConfig();
            _lastResults = new Dictionary<Type, HealthCheckResult>();
            _lastCheckTimes = new Dictionary<Type, DateTime>();
            _retryCounters = new Dictionary<Type, int>();
            _healthLogs = new List<string>();
        }

        /// <summary>
        /// Start automatic health checking
        /// </summary>
        public void StartAutoCheck()
        {
            if (_config.EnableAutoCheck)
            {
                _isRunning = true;
                _lastAutoCheckTime = Time.time;
                Log("Health checker started with auto-check enabled");
            }
        }

        /// <summary>
        /// Stop automatic health checking
        /// </summary>
        public void StopAutoCheck()
        {
            _isRunning = false;
            Log("Health checker stopped");
        }

        /// <summary>
        /// Update method - call this regularly to perform auto health checks
        /// </summary>
        public void Update()
        {
            if (!_isRunning || !_config.EnableAutoCheck)
                return;

            if (Time.time - _lastAutoCheckTime >= _config.CheckInterval)
            {
                _lastAutoCheckTime = Time.time;
                _ = CheckAllServicesHealthAsync();
            }
        }

        /// <summary>
        /// Check health of all registered services
        /// </summary>
        public async Task<Dictionary<Type, HealthCheckResult>> CheckAllServicesHealthAsync()
        {
            var results = new Dictionary<Type, HealthCheckResult>();
            var dependencyGraph = _container.DependencyGraph;

            Log("Starting health check for all services");

            foreach (var kvp in dependencyGraph.Nodes)
            {
                var serviceType = kvp.Key;
                var result = await CheckServiceHealthAsync(serviceType);
                results[serviceType] = result;
            }

            Log($"Health check completed for {results.Count} services");
            return results;
        }

        /// <summary>
        /// Check health of a specific service
        /// </summary>
        public async Task<HealthCheckResult> CheckServiceHealthAsync(Type serviceType)
        {
            var startTime = DateTime.UtcNow;
            var result = new HealthCheckResult
            {
                ServiceType = serviceType,
                ServiceName = serviceType.Name,
                CheckTime = startTime,
                Status = HealthStatus.Unknown
            };

            try
            {
                // Basic checks first
                await PerformBasicHealthChecksAsync(serviceType, result);

                // If service implements IHealthCheckable, use its custom health check
                if (await PerformCustomHealthCheckAsync(serviceType, result))
                {
                    // Custom health check was performed
                }
                else
                {
                    // Perform default health checks
                    await PerformDefaultHealthChecksAsync(serviceType, result);
                }

                // Reset retry counter on success
                _retryCounters[serviceType] = 0;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Critical;
                result.Message = $"Health check failed: {ex.Message}";
                result.Exception = ex;

                HandleHealthCheckFailure(serviceType, result, ex);
            }
            finally
            {
                result.Duration = DateTime.UtcNow - startTime;
                _lastResults[serviceType] = result;
                _lastCheckTimes[serviceType] = DateTime.UtcNow;

                OnHealthCheckCompleted?.Invoke(result);
                CheckForHealthStatusChange(serviceType, result);

                if (_config.LogResults)
                {
                    LogHealthCheckResult(result);
                }
            }

            return result;
        }

        /// <summary>
        /// Perform basic health checks (registration, dependencies, etc.)
        /// </summary>
        private async Task PerformBasicHealthChecksAsync(Type serviceType, HealthCheckResult result)
        {
            await Task.Yield(); // Make it async

            var dependencyGraph = _container.DependencyGraph;
            
            // Check if service is registered
            if (!dependencyGraph.Nodes.ContainsKey(serviceType))
            {
                result.Status = HealthStatus.Critical;
                result.Message = "Service is not registered";
                return;
            }

            var node = dependencyGraph.Nodes[serviceType];
            result.Data["ServiceStatus"] = node.Status.ToString();
            result.Data["ResolveCount"] = node.ResolveCount;
            result.Data["LastAccessTime"] = node.LastAccessTime?.ToString() ?? "Never";

            // Check service status
            switch (node.Status)
            {
                case ServiceStatus.Error:
                    result.Status = HealthStatus.Critical;
                    result.Message = "Service is in error state";
                    return;
                    
                case ServiceStatus.Disposed:
                    result.Status = HealthStatus.Unhealthy;
                    result.Message = "Service has been disposed";
                    return;
                    
                case ServiceStatus.NotInitialized:
                    result.Status = HealthStatus.Warning;
                    result.Message = "Service is not yet initialized";
                    break;
                    
                case ServiceStatus.Initializing:
                    result.Status = HealthStatus.Warning;
                    result.Message = "Service is currently initializing";
                    break;
            }

            // Check dependencies
            var circularDeps = dependencyGraph.CircularDependencies
                .Where(cycle => cycle.Contains(serviceType.Name))
                .ToList();
                
            if (circularDeps.Any())
            {
                result.Status = HealthStatus.Critical;
                result.Message = $"Circular dependency detected: {string.Join(", ", circularDeps)}";
                result.Data["CircularDependencies"] = circularDeps;
                return;
            }

            // Check if dependencies are healthy
            foreach (var dependency in node.Dependencies)
            {
                if (dependency.Status == ServiceStatus.Error)
                {
                    result.Status = HealthStatus.Warning;
                    result.Message = $"Dependency '{dependency.Name}' is in error state";
                    break;
                }
            }
        }

        /// <summary>
        /// Perform custom health check if service implements IHealthCheckable
        /// </summary>
        private async Task<bool> PerformCustomHealthCheckAsync(Type serviceType, HealthCheckResult result)
        {
            try
            {
                // Try to resolve the service
                var service = _container.Resolve(serviceType);
                
                if (service is IHealthCheckable healthCheckable)
                {
                    var customResult = await healthCheckable.CheckHealthAsync();
                    
                    // Merge custom result with our result
                    result.Status = customResult.Status;
                    result.Message = customResult.Message;
                    
                    foreach (var kvp in customResult.Data)
                    {
                        result.Data[kvp.Key] = kvp.Value;
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Critical;
                result.Message = $"Failed to resolve service for health check: {ex.Message}";
                result.Exception = ex;
            }
            
            return false;
        }

        /// <summary>
        /// Perform default health checks
        /// </summary>
        private async Task PerformDefaultHealthChecksAsync(Type serviceType, HealthCheckResult result)
        {
            await Task.Yield(); // Make it async

            try
            {
                // Try to resolve the service
                var service = _container.Resolve(serviceType);
                
                if (service == null)
                {
                    result.Status = HealthStatus.Critical;
                    result.Message = "Service resolved to null";
                    return;
                }

                // Check if service is a MonoBehaviour and if it's destroyed
                if (service is MonoBehaviour monoBehaviour)
                {
                    if (monoBehaviour == null) // Unity null check
                    {
                        result.Status = HealthStatus.Critical;
                        result.Message = "MonoBehaviour service has been destroyed";
                        return;
                    }
                    
                    if (!monoBehaviour.gameObject.activeInHierarchy)
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = "MonoBehaviour service's GameObject is inactive";
                    }
                    
                    result.Data["GameObject"] = monoBehaviour.gameObject.name;
                    result.Data["Active"] = monoBehaviour.gameObject.activeInHierarchy;
                }

                // Check performance metrics
                var serviceMonitor = _container.ServiceMonitor;
                if (serviceMonitor.Metrics.TryGetValue(serviceType, out var metrics))
                {
                    result.Data["TotalResolveCount"] = metrics.TotalResolveCount;
                    result.Data["AverageResolveTime"] = metrics.AverageResolveTime;
                    result.Data["MaxResolveTime"] = metrics.MaxResolveTime;
                    result.Data["ErrorCount"] = metrics.ErrorCount;
                    result.Data["EstimatedMemoryUsage"] = metrics.EstimatedMemoryUsage;

                    // Check for performance issues
                    if (metrics.AverageResolveTime > 100) // 100ms threshold
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = $"Service has slow resolve time: {metrics.AverageResolveTime:F2}ms";
                    }
                    
                    if (metrics.ErrorCount > 0)
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = $"Service has {metrics.ErrorCount} recorded errors";
                    }
                }

                // If no issues found, mark as healthy
                if (result.Status == HealthStatus.Unknown)
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = "Service is healthy";
                }
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Critical;
                result.Message = $"Failed to resolve service: {ex.Message}";
                result.Exception = ex;
            }
        }

        /// <summary>
        /// Handle health check failure with retry logic
        /// </summary>
        private void HandleHealthCheckFailure(Type serviceType, HealthCheckResult result, Exception ex)
        {
            if (!_retryCounters.ContainsKey(serviceType))
                _retryCounters[serviceType] = 0;

            _retryCounters[serviceType]++;

            if (_retryCounters[serviceType] <= _config.MaxRetries)
            {
                Log($"Health check failed for {serviceType.Name}, retry {_retryCounters[serviceType]}/{_config.MaxRetries}: {ex.Message}");
                
                // Schedule retry
                _ = Task.Delay(TimeSpan.FromSeconds(_config.RetryDelay))
                    .ContinueWith(_ => CheckServiceHealthAsync(serviceType));
            }
            else
            {
                Log($"Health check failed for {serviceType.Name} after {_config.MaxRetries} retries: {ex.Message}", LogType.Error);
                
                if (_config.AlertOnFailure)
                {
                    OnHealthCheckFailed?.Invoke(serviceType, ex);
                }
            }
        }

        /// <summary>
        /// Check if health status has changed and fire event
        /// </summary>
        private void CheckForHealthStatusChange(Type serviceType, HealthCheckResult newResult)
        {
            if (_lastResults.TryGetValue(serviceType, out var lastResult))
            {
                if (lastResult.Status != newResult.Status)
                {
                    Log($"Health status changed for {serviceType.Name}: {lastResult.Status} -> {newResult.Status}");
                    OnServiceHealthChanged?.Invoke(serviceType, newResult);
                }
            }
        }

        /// <summary>
        /// Get overall system health
        /// </summary>
        public HealthStatus GetOverallHealth()
        {
            if (_lastResults.Count == 0)
                return HealthStatus.Unknown;

            var statuses = _lastResults.Values.Select(r => r.Status).ToList();

            if (statuses.Any(s => s == HealthStatus.Critical))
                return HealthStatus.Critical;
                
            if (statuses.Any(s => s == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;
                
            if (statuses.Any(s => s == HealthStatus.Warning))
                return HealthStatus.Warning;
                
            if (statuses.All(s => s == HealthStatus.Healthy))
                return HealthStatus.Healthy;

            return HealthStatus.Unknown;
        }

        /// <summary>
        /// Get health summary report
        /// </summary>
        public string GenerateHealthReport()
        {
            var report = new System.Text.StringBuilder();
            var overallHealth = GetOverallHealth();
            
            report.AppendLine("=== MDI+ Health Report ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine($"Overall Health: {overallHealth}");
            report.AppendLine($"Services Checked: {_lastResults.Count}");
            report.AppendLine();

            // Group by status
            var groupedResults = _lastResults.Values.GroupBy(r => r.Status);
            
            foreach (var group in groupedResults.OrderBy(g => g.Key))
            {
                report.AppendLine($"{group.Key} ({group.Count()}):");
                
                foreach (var result in group.OrderBy(r => r.ServiceName))
                {
                    report.AppendLine($"  • {result.ServiceName}: {result.Message}");
                    
                    if (result.Exception != null)
                    {
                        report.AppendLine($"    Error: {result.Exception.Message}");
                    }
                }
                
                report.AppendLine();
            }

            return report.ToString();
        }

        /// <summary>
        /// Clear all health check data
        /// </summary>
        public void Clear()
        {
            _lastResults.Clear();
            _lastCheckTimes.Clear();
            _retryCounters.Clear();
            _healthLogs.Clear();
            Log("Health checker data cleared");
        }

        /// <summary>
        /// Log health check result
        /// </summary>
        private void LogHealthCheckResult(HealthCheckResult result)
        {
            var statusIcon = result.Status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Warning => "⚠️",
                HealthStatus.Critical => "🔴",
                HealthStatus.Unhealthy => "❌",
                _ => "❓"
            };

            Log($"{statusIcon} {result.ServiceName}: {result.Status} - {result.Message} ({result.Duration.TotalMilliseconds:F1}ms)");
        }

        /// <summary>
        /// Log message
        /// </summary>
        private void Log(string message, LogType logType = LogType.Log)
        {
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] HEALTH: {message}";
            
            _healthLogs.Add(logEntry);
            
            // Keep only last 1000 logs
            if (_healthLogs.Count > 1000)
            {
                _healthLogs.RemoveAt(0);
            }

            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(logEntry);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(logEntry);
                    break;
                default:
                    Debug.Log(logEntry);
                    break;
            }
        }
    }
}