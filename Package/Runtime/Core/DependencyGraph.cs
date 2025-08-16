using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MDI.Core
{
    /// <summary>
    /// Service dependency node'u
    /// </summary>
    [Serializable]
    public class DependencyNode
    {
        /// <summary>
        /// Service tipi
        /// </summary>
        public Type ServiceType { get; set; }
        
        /// <summary>
        /// Service adı
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Bu service'in bağımlı olduğu service'ler
        /// </summary>
        public List<Type> Dependencies { get; set; } = new List<Type>();
        
        /// <summary>
        /// Bu service'e bağımlı olan service'ler
        /// </summary>
        public List<Type> Dependents { get; set; } = new List<Type>();
        
        /// <summary>
        /// Service'in durumu
        /// </summary>
        public ServiceStatus Status { get; set; } = ServiceStatus.NotInitialized;
        
        /// <summary>
        /// Service'in başlatılma zamanı
        /// </summary>
        public DateTime? InitializationTime { get; set; }
        
        /// <summary>
        /// Service'in son kullanılma zamanı
        /// </summary>
        public DateTime? LastAccessTime { get; set; }
        
        /// <summary>
        /// Service'in kaç kez resolve edildiği
        /// </summary>
        public int ResolveCount { get; set; } = 0;
    }
    
    /// <summary>
    /// Service durumu
    /// </summary>
    public enum ServiceStatus
    {
        NotInitialized,
        Initializing,
        Initialized,
        Error,
        Disposed
    }
    
    /// <summary>
    /// Dependency graph yönetimi
    /// </summary>
    public class DependencyGraph
    {
        private readonly Dictionary<Type, DependencyNode> _nodes = new Dictionary<Type, DependencyNode>();
        private readonly List<string> _circularDependencies = new List<string>();
        
        /// <summary>
        /// Tüm node'lar
        /// </summary>
        public IReadOnlyDictionary<Type, DependencyNode> Nodes => _nodes;
        
        /// <summary>
        /// Tespit edilen circular dependency'ler
        /// </summary>
        public IReadOnlyList<string> CircularDependencies => _circularDependencies;
        
        /// <summary>
        /// Service node'u ekler
        /// </summary>
        public void AddService(Type serviceType, string name = null)
        {
            if (!_nodes.ContainsKey(serviceType))
            {
                _nodes[serviceType] = new DependencyNode
                {
                    ServiceType = serviceType,
                    Name = name ?? serviceType.Name
                };
            }
        }
        
        /// <summary>
        /// Belirtilen service type için node'u getirir
        /// </summary>
        public DependencyNode GetNode(Type serviceType)
        {
            return _nodes.TryGetValue(serviceType, out var node) ? node : null;
        }
        
        /// <summary>
        /// Dependency ilişkisi ekler
        /// </summary>
        public void AddDependency(Type serviceType, Type dependencyType)
        {
            // Service'leri ekle
            AddService(serviceType);
            AddService(dependencyType);
            
            var serviceNode = _nodes[serviceType];
            var dependencyNode = _nodes[dependencyType];
            
            // Dependency ilişkisini ekle
            if (!serviceNode.Dependencies.Contains(dependencyType))
            {
                serviceNode.Dependencies.Add(dependencyType);
            }
            
            if (!dependencyNode.Dependents.Contains(serviceType))
            {
                dependencyNode.Dependents.Add(serviceType);
            }
        }
        
        /// <summary>
        /// Service durumunu günceller
        /// </summary>
        public void UpdateServiceStatus(Type serviceType, ServiceStatus status)
        {
            if (_nodes.TryGetValue(serviceType, out var node))
            {
                node.Status = status;
                
                if (status == ServiceStatus.Initialized)
                {
                    node.InitializationTime = DateTime.Now;
                }
            }
        }
        
        /// <summary>
        /// Service'in resolve edildiğini kaydeder
        /// </summary>
        public void RecordServiceAccess(Type serviceType)
        {
            if (_nodes.TryGetValue(serviceType, out var node))
            {
                node.ResolveCount++;
                node.LastAccessTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Circular dependency kontrolü yapar
        /// </summary>
        public bool DetectCircularDependencies()
        {
            _circularDependencies.Clear();
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();
            
            foreach (var serviceType in _nodes.Keys)
            {
                if (!visited.Contains(serviceType))
                {
                    if (HasCircularDependency(serviceType, visited, recursionStack, new List<Type>()))
                    {
                        return true;
                    }
                }
            }
            
            return _circularDependencies.Count > 0;
        }
        
        /// <summary>
        /// Recursive circular dependency kontrolü
        /// </summary>
        private bool HasCircularDependency(Type serviceType, HashSet<Type> visited, HashSet<Type> recursionStack, List<Type> path)
        {
            visited.Add(serviceType);
            recursionStack.Add(serviceType);
            path.Add(serviceType);
            
            if (_nodes.TryGetValue(serviceType, out var node))
            {
                foreach (var dependency in node.Dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        if (HasCircularDependency(dependency, visited, recursionStack, new List<Type>(path)))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        // Circular dependency bulundu
                        var cycle = new List<Type>(path);
                        cycle.Add(dependency);
                        var cycleString = string.Join(" -> ", cycle.Select(t => t.Name));
                        _circularDependencies.Add(cycleString);
                        
                        Debug.LogError($"[MDI+] Circular dependency detected: {cycleString}");
                        return true;
                    }
                }
            }
            
            recursionStack.Remove(serviceType);
            return false;
        }
        
        /// <summary>
        /// Dependency tree'yi string olarak döndürür
        /// </summary>
        public string GetDependencyTree(Type rootType, int maxDepth = 10)
        {
            if (!_nodes.ContainsKey(rootType))
                return $"Service {rootType.Name} not found in dependency graph.";
            
            var result = new System.Text.StringBuilder();
            var visited = new HashSet<Type>();
            
            BuildDependencyTree(rootType, result, visited, 0, maxDepth);
            
            return result.ToString();
        }
        
        /// <summary>
        /// Recursive dependency tree builder
        /// </summary>
        private void BuildDependencyTree(Type serviceType, System.Text.StringBuilder result, HashSet<Type> visited, int depth, int maxDepth)
        {
            if (depth > maxDepth || visited.Contains(serviceType))
            {
                result.AppendLine($"{new string(' ', depth * 2)}{serviceType.Name} (circular or max depth)");
                return;
            }
            
            visited.Add(serviceType);
            
            var node = _nodes[serviceType];
            var indent = new string(' ', depth * 2);
            var statusIcon = GetStatusIcon(node.Status);
            
            result.AppendLine($"{indent}{statusIcon} {node.Name} ({node.Status})");
            
            foreach (var dependency in node.Dependencies)
            {
                BuildDependencyTree(dependency, result, new HashSet<Type>(visited), depth + 1, maxDepth);
            }
        }
        
        /// <summary>
        /// Service durumu için icon döndürür
        /// </summary>
        private string GetStatusIcon(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.NotInitialized => "⚪",
                ServiceStatus.Initializing => "🟡",
                ServiceStatus.Initialized => "🟢",
                ServiceStatus.Error => "🔴",
                ServiceStatus.Disposed => "⚫",
                _ => "❓"
            };
        }
        
        /// <summary>
        /// Graph'ı temizler
        /// </summary>
        public void Clear()
        {
            _nodes.Clear();
            _circularDependencies.Clear();
        }
        
        /// <summary>
        /// Service istatistiklerini döndürür
        /// </summary>
        public Dictionary<string, object> GetStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalServices"] = _nodes.Count,
                ["InitializedServices"] = _nodes.Values.Count(n => n.Status == ServiceStatus.Initialized),
                ["ErrorServices"] = _nodes.Values.Count(n => n.Status == ServiceStatus.Error),
                ["CircularDependencies"] = _circularDependencies.Count,
                ["TotalResolves"] = _nodes.Values.Sum(n => n.ResolveCount),
                ["MostUsedService"] = _nodes.Values.OrderByDescending(n => n.ResolveCount).FirstOrDefault()?.Name ?? "None"
            };
            
            return stats;
        }
    }
}