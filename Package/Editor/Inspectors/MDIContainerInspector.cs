using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MDI.Core;
using MDI.Containers;

namespace MDI.Editor.Inspectors
{
    /// <summary>
    /// MDIContainer için özel inspector
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MDIContainerInspector : UnityEditor.Editor
    {
        private MDIContainer _container;
        private bool _showServices = true;
        private bool _showDependencies = false;
        private bool _showPerformance = false;
        private bool _showSettings = false;
        private string _searchFilter = "";
        private ServiceLifetime _lifetimeFilter = (ServiceLifetime)(-1); // All
        
        private static GUIStyle _headerStyle;
        private static GUIStyle _serviceBoxStyle;
        private static GUIStyle _dependencyStyle;
        private static GUIStyle _performanceStyle;
        private static bool _stylesInitialized = false;
        
        private Vector2 _servicesScrollPos;
        private Vector2 _dependenciesScrollPos;
        
        public override void OnInspectorGUI()
        {
            // Önce normal inspector'ı çiz
            DrawDefaultInspector();
            
            // MDI Container'ı kontrol et
            _container = GetMDIContainer();
            if (_container == null) return;
            
            InitializeStyles();
            
            EditorGUILayout.Space(10);
            DrawMDISection();
        }
        
        private MDIContainer GetMDIContainer()
        {
            var targetObject = target as MonoBehaviour;
            if (targetObject == null) return null;
            
            // Bootstrapper kontrolü
            if (targetObject.GetType().Name.Contains("Bootstrap"))
            {
                var containerField = targetObject.GetType()
                    .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.FieldType == typeof(MDIContainer));
                
                if (containerField != null)
                {
                    return containerField.GetValue(targetObject) as MDIContainer;
                }
            }
            
            // Global container kontrolü
            if (Application.isPlaying)
            {
                try
                {
                    var mdiType = System.Type.GetType("MDI.Core.MDI, Assembly-CSharp");
                    if (mdiType != null)
                    {
                        var globalContainerProperty = mdiType.GetProperty("GlobalContainer");
                        if (globalContainerProperty != null)
                        {
                            return globalContainerProperty.GetValue(null) as MDIContainer;
                        }
                    }
                }
                catch { }
            }
            
            return null;
        }
        
        private void DrawMDISection()
        {
            EditorGUILayout.BeginVertical(_headerStyle);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🔧 MDI+ Container", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("🔍 Open Monitor", GUILayout.Width(100)))
            {
                EditorApplication.ExecuteMenuItem("MDI+/🔍 Service Monitor");
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Container durumu
            DrawContainerStatus();
            
            EditorGUILayout.Space(5);
            
            // Tabs
            EditorGUILayout.BeginHorizontal();
            _showServices = GUILayout.Toggle(_showServices, "📦 Services", "Button");
            _showDependencies = GUILayout.Toggle(_showDependencies, "🔗 Dependencies", "Button");
            _showPerformance = GUILayout.Toggle(_showPerformance, "📊 Performance", "Button");
            _showSettings = GUILayout.Toggle(_showSettings, "⚙️ Settings", "Button");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Content
            if (_showServices)
                DrawServicesTab();
            else if (_showDependencies)
                DrawDependenciesTab();
            else if (_showPerformance)
                DrawPerformanceTab();
            else if (_showSettings)
                DrawSettingsTab();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawContainerStatus()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Durum göstergesi
            var statusColor = _container.IsHealthy() ? Color.green : Color.red;
            var statusText = _container.IsHealthy() ? "✅ Healthy" : "❌ Unhealthy";
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusText, EditorStyles.miniLabel);
            GUI.color = oldColor;
            
            GUILayout.FlexibleSpace();
            
            // İstatistikler
            var serviceCount = _container.ServiceDescriptors?.Count ?? 0;
            GUILayout.Label($"Services: {serviceCount}", EditorStyles.miniLabel);
            
            if (_container.ServiceMonitor != null)
            {
                var resolveCount = _container.ServiceMonitor.TotalResolveCount;
                GUILayout.Label($"Resolves: {resolveCount}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawServicesTab()
        {
            if (_container.ServiceDescriptors == null || !_container.ServiceDescriptors.Any())
            {
                EditorGUILayout.HelpBox("No services registered.", MessageType.Info);
                return;
            }
            
            // Filtreler
            EditorGUILayout.BeginHorizontal();
            _searchFilter = EditorGUILayout.TextField("🔍 Search:", _searchFilter);
            _lifetimeFilter = (ServiceLifetime)EditorGUILayout.EnumPopup("Lifetime:", _lifetimeFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Services listesi
            _servicesScrollPos = EditorGUILayout.BeginScrollView(_servicesScrollPos, GUILayout.Height(200));
            
            var filteredServices = FilterServices();
            
            foreach (var kvp in filteredServices)
            {
                DrawServiceItem(kvp.Key, kvp.Value);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawServiceItem(Type serviceType, ServiceDescriptor descriptor)
        {
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            // Service adı
            GUILayout.Label($"🔧 {serviceType.Name}", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            // Lifetime badge
            var lifetimeColor = GetLifetimeColor(descriptor.Lifetime);
            var oldColor = GUI.color;
            GUI.color = lifetimeColor;
            GUILayout.Label(descriptor.Lifetime.ToString(), EditorStyles.miniLabel);
            GUI.color = oldColor;
            
            // Resolve butonu
            if (Application.isPlaying && GUILayout.Button("Resolve", GUILayout.Width(60)))
            {
                try
                {
                    var instance = _container.Resolve(serviceType);
                    Debug.Log($"[MDI+] Resolved {serviceType.Name}: {instance}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MDI+] Failed to resolve {serviceType.Name}: {ex.Message}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Implementation type
            if (descriptor.ImplementationType != serviceType)
            {
                EditorGUILayout.LabelField($"Implementation: {descriptor.ImplementationType.Name}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        private void DrawDependenciesTab()
        {
            if (_container.DependencyGraph == null)
            {
                EditorGUILayout.HelpBox("Dependency graph not available.", MessageType.Info);
                return;
            }
            
            _dependenciesScrollPos = EditorGUILayout.BeginScrollView(_dependenciesScrollPos, GUILayout.Height(200));
            
            foreach (var kvp in _container.ServiceDescriptors)
            {
                var dependencies = _container.DependencyGraph.GetDependencies(kvp.Key);
                if (dependencies.Any())
                {
                    EditorGUILayout.BeginVertical(_dependencyStyle);
                    
                    EditorGUILayout.LabelField($"🔧 {kvp.Key.Name}", EditorStyles.boldLabel);
                    
                    foreach (var dep in dependencies)
                    {
                        EditorGUILayout.LabelField($"  └─ {dep.Name}", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawPerformanceTab()
        {
            if (_container.ServiceMonitor == null)
            {
                EditorGUILayout.HelpBox("Service monitoring not enabled.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(_performanceStyle);
            
            var monitor = _container.ServiceMonitor;
            
            EditorGUILayout.LabelField("📊 Performance Metrics", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"Total Resolves: {monitor.TotalResolveCount}");
            EditorGUILayout.LabelField($"Average Resolve Time: {monitor.AverageResolveTime:F2}ms");
            EditorGUILayout.LabelField($"Memory Usage: {FormatBytes(monitor.MemoryUsage)}");
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("📋 Generate Report"))
            {
                var report = monitor.GeneratePerformanceReport();
                Debug.Log($"[MDI+] Performance Report:\n{report}");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("⚙️ Container Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"Monitoring Enabled: {(_container.ServiceMonitor != null ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"Health Checking Enabled: {(_container.HealthChecker != null ? "Yes" : "No")}");
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("🧹 Clear Container"))
            {
                if (EditorUtility.DisplayDialog("Clear Container", "Are you sure you want to clear all services?", "Yes", "No"))
                {
                    _container.Clear();
                    Debug.Log("[MDI+] Container cleared.");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private Dictionary<Type, ServiceDescriptor> FilterServices()
        {
            var services = _container.ServiceDescriptors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                services = services.Where(kvp => 
                    kvp.Key.Name.ToLower().Contains(_searchFilter.ToLower()) ||
                    kvp.Value.ImplementationType.Name.ToLower().Contains(_searchFilter.ToLower())
                ).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            
            if ((int)_lifetimeFilter >= 0)
            {
                services = services.Where(kvp => kvp.Value.Lifetime == _lifetimeFilter)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            
            return services;
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _headerStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };
            
            _serviceBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(0, 0, 1, 1)
            };
            
            _dependencyStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(0, 0, 1, 1)
            };
            
            _performanceStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
            
            _stylesInitialized = true;
        }
        
        private Color GetLifetimeColor(ServiceLifetime lifetime)
        {
            return lifetime switch
            {
                ServiceLifetime.Singleton => new Color(0.2f, 0.8f, 0.2f), // Green
                ServiceLifetime.Scoped => new Color(1f, 0.8f, 0.2f), // Yellow
                ServiceLifetime.Transient => new Color(0.3f, 0.7f, 1f), // Blue
                ServiceLifetime.Lazy => Color.magenta,
                _ => Color.gray
            };
        }
        
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}