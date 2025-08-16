using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace MDI.Core
{
    /// <summary>
    /// Service performans metrikleri
    /// </summary>
    [Serializable]
    public class ServiceMetrics
    {
        /// <summary>
        /// Service tipi
        /// </summary>
        public Type ServiceType { get; set; }
        
        /// <summary>
        /// Service adı
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// İlk resolve zamanı
        /// </summary>
        public DateTime FirstResolveTime { get; set; }
        
        /// <summary>
        /// Son resolve zamanı
        /// </summary>
        public DateTime LastResolveTime { get; set; }
        
        /// <summary>
        /// Toplam resolve sayısı
        /// </summary>
        public int TotalResolveCount { get; set; }
        
        /// <summary>
        /// Ortalama resolve süresi (milliseconds)
        /// </summary>
        public double AverageResolveTime { get; set; }
        
        /// <summary>
        /// En uzun resolve süresi (milliseconds)
        /// </summary>
        public double MaxResolveTime { get; set; }
        
        /// <summary>
        /// En kısa resolve süresi (milliseconds)
        /// </summary>
        public double MinResolveTime { get; set; } = double.MaxValue;
        
        /// <summary>
        /// Toplam resolve süresi (milliseconds)
        /// </summary>
        public double TotalResolveTime { get; set; }
        
        /// <summary>
        /// Memory kullanımı (bytes)
        /// </summary>
        public long MemoryUsage { get; set; }
        
        /// <summary>
        /// Tahmini memory kullanımı (bytes)
        /// </summary>
        public long EstimatedMemoryUsage { get; set; }
        
        /// <summary>
        /// Service durumu
        /// </summary>
        public ServiceStatus Status { get; set; }
        
        /// <summary>
        /// Hata sayısı
        /// </summary>
        public int ErrorCount { get; set; }
        
        /// <summary>
        /// Son hata mesajı
        /// </summary>
        public string LastError { get; set; }
        
        /// <summary>
        /// Son hata zamanı
        /// </summary>
        public DateTime? LastErrorTime { get; set; }
    }
    
    /// <summary>
    /// Service monitoring ve performance tracking
    /// </summary>
    public class ServiceMonitor
    {
        private readonly Dictionary<Type, ServiceMetrics> _metrics = new Dictionary<Type, ServiceMetrics>();
        private readonly Dictionary<Type, Stopwatch> _activeResolves = new Dictionary<Type, Stopwatch>();
        private readonly List<string> _logs = new List<string>();
        private readonly int _maxLogCount = 1000;
        
        /// <summary>
        /// Tüm service metrikleri
        /// </summary>
        public IReadOnlyDictionary<Type, ServiceMetrics> Metrics => _metrics;
        
        /// <summary>
        /// Log kayıtları
        /// </summary>
        public IReadOnlyList<string> Logs => _logs;
        
        /// <summary>
        /// Toplam resolve sayısını döndürür
        /// </summary>
        public int TotalResolveCount => _metrics.Values.Sum(m => m.TotalResolveCount);
        
        /// <summary>
        /// Ortalama resolve süresini döndürür
        /// </summary>
        public double AverageResolveTime => _metrics.Values.Any() ? _metrics.Values.Average(m => m.AverageResolveTime) : 0;
        
        /// <summary>
        /// Toplam bellek kullanımını döndürür
        /// </summary>
        public long MemoryUsage => _metrics.Values.Sum(m => m.MemoryUsage);
        
        /// <summary>
        /// Log kayıtlarını temizler
        /// </summary>
        public void ClearLogs()
        {
            _logs.Clear();
            LogEvent("Logs cleared");
        }
        
        /// <summary>
        /// Service resolve başlangıcını kaydeder
        /// </summary>
        public void StartResolve(Type serviceType, string serviceName = null)
        {
            // Metrics'i initialize et
            if (!_metrics.ContainsKey(serviceType))
            {
                _metrics[serviceType] = new ServiceMetrics
                {
                    ServiceType = serviceType,
                    ServiceName = serviceName ?? serviceType.Name,
                    FirstResolveTime = DateTime.Now,
                    Status = ServiceStatus.Initializing
                };
            }
            
            // Stopwatch başlat
            if (!_activeResolves.ContainsKey(serviceType))
            {
                _activeResolves[serviceType] = new Stopwatch();
            }
            
            _activeResolves[serviceType].Restart();
            
            LogEvent($"[RESOLVE START] {serviceName ?? serviceType.Name}");
        }
        
        /// <summary>
        /// Service resolve bitişini kaydeder
        /// </summary>
        public void EndResolve(Type serviceType, bool success = true, string errorMessage = null)
        {
            if (!_activeResolves.TryGetValue(serviceType, out var stopwatch))
                return;
                
            stopwatch.Stop();
            var resolveTime = stopwatch.Elapsed.TotalMilliseconds;
            
            if (_metrics.TryGetValue(serviceType, out var metrics))
            {
                metrics.LastResolveTime = DateTime.Now;
                metrics.TotalResolveCount++;
                metrics.TotalResolveTime += resolveTime;
                metrics.AverageResolveTime = metrics.TotalResolveTime / metrics.TotalResolveCount;
                
                if (resolveTime > metrics.MaxResolveTime)
                    metrics.MaxResolveTime = resolveTime;
                    
                if (resolveTime < metrics.MinResolveTime)
                    metrics.MinResolveTime = resolveTime;
                
                if (success)
                {
                    metrics.Status = ServiceStatus.Initialized;
                    LogEvent($"[RESOLVE SUCCESS] {metrics.ServiceName} - {resolveTime:F2}ms");
                }
                else
                {
                    metrics.Status = ServiceStatus.Error;
                    metrics.ErrorCount++;
                    metrics.LastError = errorMessage;
                    metrics.LastErrorTime = DateTime.Now;
                    LogEvent($"[RESOLVE ERROR] {metrics.ServiceName} - {errorMessage}");
                }
                
                // Memory kullanımını tahmin et (basit hesaplama)
                EstimateMemoryUsage(metrics);
            }
            
            _activeResolves.Remove(serviceType);
        }
        
        /// <summary>
        /// Service access'ini kaydeder
        /// </summary>
        public void RecordAccess(Type serviceType)
        {
            if (_metrics.TryGetValue(serviceType, out var metrics))
            {
                metrics.LastResolveTime = DateTime.Now;
                LogEvent($"[ACCESS] {metrics.ServiceName}");
            }
        }
        
        /// <summary>
        /// Service durumunu günceller
        /// </summary>
        public void UpdateStatus(Type serviceType, ServiceStatus status)
        {
            if (_metrics.TryGetValue(serviceType, out var metrics))
            {
                var oldStatus = metrics.Status;
                metrics.Status = status;
                
                if (oldStatus != status)
                {
                    LogEvent($"[STATUS CHANGE] {metrics.ServiceName}: {oldStatus} -> {status}");
                }
            }
        }
        
        /// <summary>
        /// Memory kullanımını tahmin eder
        /// </summary>
        private void EstimateMemoryUsage(ServiceMetrics metrics)
        {
            // Basit memory estimation - gerçek uygulamada daha sofistike olabilir
            var baseSize = 64; // Base object overhead
            var typeSize = metrics.ServiceType.Name.Length * 2; // String memory
            var instanceSize = 128; // Estimated instance size
            
            metrics.MemoryUsage = baseSize + typeSize + instanceSize;
        }
        
        /// <summary>
        /// Event log'u ekler
        /// </summary>
        private void LogEvent(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";
            
            _logs.Add(logEntry);
            
            // Log sayısını sınırla
            if (_logs.Count > _maxLogCount)
            {
                _logs.RemoveAt(0);
            }
            
            // Unity console'a da yazdır (debug mode'da)
            if (Application.isEditor)
            {
                UnityEngine.Debug.Log($"[MDI+ Monitor] {logEntry}");
            }
        }
        
        /// <summary>
        /// Performance raporu oluşturur
        /// </summary>
        public string GeneratePerformanceReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== MDI+ Service Performance Report ===");
            report.AppendLine($"Generated at: {DateTime.Now}");
            report.AppendLine($"Total Services: {_metrics.Count}");
            report.AppendLine();
            
            // Genel istatistikler
            var totalResolves = _metrics.Values.Sum(m => m.TotalResolveCount);
            var avgResolveTime = _metrics.Values.Where(m => m.TotalResolveCount > 0).Average(m => m.AverageResolveTime);
            var totalErrors = _metrics.Values.Sum(m => m.ErrorCount);
            
            report.AppendLine("=== General Statistics ===");
            report.AppendLine($"Total Resolves: {totalResolves}");
            report.AppendLine($"Average Resolve Time: {avgResolveTime:F2}ms");
            report.AppendLine($"Total Errors: {totalErrors}");
            report.AppendLine($"Error Rate: {(totalErrors * 100.0 / Math.Max(totalResolves, 1)):F2}%");
            report.AppendLine();
            
            // En çok kullanılan servisler
            report.AppendLine("=== Most Used Services ===");
            var mostUsed = _metrics.Values
                .OrderByDescending(m => m.TotalResolveCount)
                .Take(5);
                
            foreach (var metric in mostUsed)
            {
                report.AppendLine($"{metric.ServiceName}: {metric.TotalResolveCount} resolves, {metric.AverageResolveTime:F2}ms avg");
            }
            report.AppendLine();
            
            // En yavaş servisler
            report.AppendLine("=== Slowest Services ===");
            var slowest = _metrics.Values
                .Where(m => m.TotalResolveCount > 0)
                .OrderByDescending(m => m.AverageResolveTime)
                .Take(5);
                
            foreach (var metric in slowest)
            {
                report.AppendLine($"{metric.ServiceName}: {metric.AverageResolveTime:F2}ms avg (max: {metric.MaxResolveTime:F2}ms)");
            }
            report.AppendLine();
            
            // Hatalı servisler
            var errorServices = _metrics.Values.Where(m => m.ErrorCount > 0).ToList();
            if (errorServices.Any())
            {
                report.AppendLine("=== Services with Errors ===");
                foreach (var metric in errorServices)
                {
                    report.AppendLine($"{metric.ServiceName}: {metric.ErrorCount} errors, Last: {metric.LastError}");
                }
                report.AppendLine();
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// Belirli bir service için detaylı rapor
        /// </summary>
        public string GetServiceReport(Type serviceType)
        {
            if (!_metrics.TryGetValue(serviceType, out var metric))
                return $"No metrics found for service: {serviceType.Name}";
                
            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== Service Report: {metric.ServiceName} ===");
            report.AppendLine($"Type: {metric.ServiceType.FullName}");
            report.AppendLine($"Status: {metric.Status}");
            report.AppendLine($"First Resolve: {metric.FirstResolveTime}");
            report.AppendLine($"Last Resolve: {metric.LastResolveTime}");
            report.AppendLine($"Total Resolves: {metric.TotalResolveCount}");
            report.AppendLine($"Average Time: {metric.AverageResolveTime:F2}ms");
            report.AppendLine($"Min Time: {metric.MinResolveTime:F2}ms");
            report.AppendLine($"Max Time: {metric.MaxResolveTime:F2}ms");
            report.AppendLine($"Memory Usage: {metric.MemoryUsage} bytes");
            report.AppendLine($"Error Count: {metric.ErrorCount}");
            
            if (!string.IsNullOrEmpty(metric.LastError))
            {
                report.AppendLine($"Last Error: {metric.LastError}");
                report.AppendLine($"Last Error Time: {metric.LastErrorTime}");
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// Monitoring verilerini temizler
        /// </summary>
        public void Clear()
        {
            _metrics.Clear();
            _activeResolves.Clear();
            _logs.Clear();
            LogEvent("Monitor data cleared");
        }
        
        /// <summary>
        /// JSON formatında metrics export eder
        /// </summary>
        public string ExportMetricsAsJson()
        {
            try
            {
                return JsonUtility.ToJson(new { metrics = _metrics.Values.ToArray() }, true);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Failed to export metrics: {ex.Message}\"}}";
            }
        }
    }
}