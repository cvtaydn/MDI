using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MDI.Core;
using MDI.Containers;

namespace MDI.Editor.Window
{
    /// <summary>
    /// MDI+ Service Monitor Editor Window
    /// </summary>
    public class MDIMonitorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Services", "Dependencies", "Performance", "Logs" };
        
        private MDIContainer _currentContainer;
        private bool _autoRefresh = true;
        private float _refreshInterval = 1.0f;
        private double _lastRefreshTime;
        
        private string _searchFilter = "";
        private ServiceStatus _statusFilter = ServiceStatus.Initialized;
        private bool _showAllStatuses = true;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _serviceBoxStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _successStyle;
        private GUIStyle _warningStyle;
        
        [MenuItem("MDI+/Service Monitor", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<MDIMonitorWindow>("MDI+ Monitor");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeStyles();
            FindCurrentContainer();
        }
        
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            _serviceBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 2, 2)
            };
            
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red }
            };
            
            _successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green }
            };
            
            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.yellow }
            };
        }
        
        private void FindCurrentContainer()
        {
            // Runtime'da aktif container'ı bul
            // Bu basit bir implementasyon - gerçek uygulamada daha sofistike olabilir
            _currentContainer = FindObjectOfType<MonoBehaviour>()
                ?.GetComponent<MonoBehaviour>()
                ?.GetType()
                ?.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.FirstOrDefault(f => f.FieldType == typeof(MDIContainer))
                ?.GetValue(FindObjectOfType<MonoBehaviour>()) as MDIContainer;
        }
        
        private void OnGUI()
        {
            if (_headerStyle == null)
                InitializeStyles();
                
            DrawHeader();
            DrawToolbar();
            
            if (_currentContainer == null)
            {
                DrawNoContainerMessage();
                return;
            }
            
            // Auto refresh
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_selectedTab)
            {
                case 0:
                    DrawServicesTab();
                    break;
                case 1:
                    DrawDependenciesTab();
                    break;
                case 2:
                    DrawPerformanceTab();
                    break;
                case 3:
                    DrawLogsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MDI+ Service Monitor", _headerStyle);
            EditorGUILayout.Space();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Tab selection
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            // Auto refresh toggle
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            
            // Refresh interval
            if (_autoRefresh)
            {
                GUILayout.Label("Interval:", EditorStyles.miniLabel);
                _refreshInterval = EditorGUILayout.FloatField(_refreshInterval, EditorStyles.toolbarTextField, GUILayout.Width(50));
                _refreshInterval = Mathf.Clamp(_refreshInterval, 0.1f, 10f);
            }
            
            // Manual refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                FindCurrentContainer();
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNoContainerMessage()
        {
            EditorGUILayout.Space(50);
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("No MDI+ Container Found", _headerStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Make sure you have an active MDI+ container in your scene.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Search for Container", GUILayout.Height(30)))
            {
                FindCurrentContainer();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawServicesTab()
        {
            DrawServiceFilters();
            
            var dependencyGraph = _currentContainer.DependencyGraph;
            var serviceMonitor = _currentContainer.ServiceMonitor;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Registered Services ({dependencyGraph.Nodes.Count})", EditorStyles.boldLabel);
            
            foreach (var kvp in dependencyGraph.Nodes)
            {
                var serviceType = kvp.Key;
                var node = kvp.Value;
                
                // Filter by search
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !node.Name.ToLower().Contains(_searchFilter.ToLower()) &&
                    !serviceType.Name.ToLower().Contains(_searchFilter.ToLower()))
                    continue;
                    
                // Filter by status
                if (!_showAllStatuses && node.Status != _statusFilter)
                    continue;
                
                DrawServiceNode(serviceType, node, serviceMonitor);
            }
        }
        
        private void DrawServiceFilters()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Search filter
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            
            GUILayout.Space(20);
            
            // Status filter
            _showAllStatuses = EditorGUILayout.Toggle("All Statuses", _showAllStatuses);
            
            if (!_showAllStatuses)
            {
                EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
                _statusFilter = (ServiceStatus)EditorGUILayout.EnumPopup(_statusFilter);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawServiceNode(Type serviceType, DependencyNode node, ServiceMonitor monitor)
        {
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            
            // Service header
            EditorGUILayout.BeginHorizontal();
            
            var statusIcon = GetStatusIcon(node.Status);
            var statusColor = GetStatusColor(node.Status);
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
            GUI.color = originalColor;
            
            EditorGUILayout.LabelField(node.Name, EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField(node.Status.ToString(), GetStatusStyle(node.Status));
            
            EditorGUILayout.EndHorizontal();
            
            // Service details
            EditorGUILayout.LabelField($"Type: {serviceType.Name}", EditorStyles.miniLabel);
            
            if (node.InitializationTime.HasValue)
            {
                EditorGUILayout.LabelField($"Initialized: {node.InitializationTime.Value:HH:mm:ss}", EditorStyles.miniLabel);
            }
            
            if (node.LastAccessTime.HasValue)
            {
                EditorGUILayout.LabelField($"Last Access: {node.LastAccessTime.Value:HH:mm:ss}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.LabelField($"Resolve Count: {node.ResolveCount}", EditorStyles.miniLabel);
            
            // Performance metrics
            if (monitor.Metrics.TryGetValue(serviceType, out var metrics))
            {
                if (metrics.TotalResolveCount > 0)
                {
                    EditorGUILayout.LabelField($"Avg Resolve Time: {metrics.AverageResolveTime:F2}ms", EditorStyles.miniLabel);
                    
                    if (metrics.ErrorCount > 0)
                    {
                        EditorGUILayout.LabelField($"Errors: {metrics.ErrorCount}", _errorStyle);
                    }
                }
            }
            
            // Dependencies
            if (node.Dependencies.Count > 0)
            {
                EditorGUILayout.LabelField($"Dependencies ({node.Dependencies.Count}):", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                foreach (var dep in node.Dependencies)
                {
                    EditorGUILayout.LabelField($"• {dep.Name}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
            
            // Dependents
            if (node.Dependents.Count > 0)
            {
                EditorGUILayout.LabelField($"Used by ({node.Dependents.Count}):", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                foreach (var dependent in node.Dependents)
                {
                    EditorGUILayout.LabelField($"• {dependent.Name}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDependenciesTab()
        {
            var dependencyGraph = _currentContainer.DependencyGraph;
            
            EditorGUILayout.LabelField("Dependency Analysis", EditorStyles.boldLabel);
            
            // Circular dependencies
            if (dependencyGraph.CircularDependencies.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("⚠️ Circular Dependencies Detected!", _errorStyle);
                
                foreach (var cycle in dependencyGraph.CircularDependencies)
                {
                    EditorGUILayout.LabelField($"• {cycle}", _errorStyle);
                }
            }
            else
            {
                EditorGUILayout.LabelField("✅ No circular dependencies detected", _successStyle);
            }
            
            EditorGUILayout.Space();
            
            // Dependency tree for each service
            EditorGUILayout.LabelField("Dependency Trees", EditorStyles.boldLabel);
            
            foreach (var kvp in dependencyGraph.Nodes)
            {
                if (kvp.Value.Dependencies.Count == 0) continue;
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(kvp.Value.Name, EditorStyles.boldLabel);
                
                var tree = dependencyGraph.GetDependencyTree(kvp.Key, 5);
                EditorGUILayout.TextArea(tree, EditorStyles.textArea, GUILayout.Height(100));
            }
        }
        
        private void DrawPerformanceTab()
        {
            var serviceMonitor = _currentContainer.ServiceMonitor;
            
            EditorGUILayout.LabelField("Performance Report", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Full Report"))
            {
                var report = serviceMonitor.GeneratePerformanceReport();
                Debug.Log(report);
                EditorUtility.DisplayDialog("Performance Report", "Report generated and logged to console.", "OK");
            }
            
            EditorGUILayout.Space();
            
            // Quick stats
            var stats = _currentContainer.GetServiceStatistics();
            
            EditorGUILayout.LabelField("Quick Statistics", EditorStyles.boldLabel);
            
            foreach (var stat in stats)
            {
                EditorGUILayout.LabelField($"{stat.Key}: {stat.Value}");
            }
            
            EditorGUILayout.Space();
            
            // Export options
            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Export Metrics as JSON"))
            {
                var json = serviceMonitor.ExportMetricsAsJson();
                var path = EditorUtility.SaveFilePanel("Save Metrics", "", "mdi_metrics.json", "json");
                
                if (!string.IsNullOrEmpty(path))
                {
                    System.IO.File.WriteAllText(path, json);
                    EditorUtility.DisplayDialog("Export Complete", $"Metrics exported to {path}", "OK");
                }
            }
        }
        
        private void DrawLogsTab()
        {
            var serviceMonitor = _currentContainer.ServiceMonitor;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Service Logs ({serviceMonitor.Logs.Count})", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear Logs"))
            {
                serviceMonitor.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Log entries
            foreach (var log in serviceMonitor.Logs.Reverse().Take(100))
            {
                var style = EditorStyles.label;
                
                if (log.Contains("ERROR"))
                    style = _errorStyle;
                else if (log.Contains("SUCCESS"))
                    style = _successStyle;
                else if (log.Contains("START") || log.Contains("STATUS"))
                    style = _warningStyle;
                
                EditorGUILayout.LabelField(log, style);
            }
        }
        
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
        
        private Color GetStatusColor(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.NotInitialized => Color.gray,
                ServiceStatus.Initializing => Color.yellow,
                ServiceStatus.Initialized => Color.green,
                ServiceStatus.Error => Color.red,
                ServiceStatus.Disposed => Color.black,
                _ => Color.white
            };
        }
        
        private GUIStyle GetStatusStyle(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Error => _errorStyle,
                ServiceStatus.Initialized => _successStyle,
                ServiceStatus.Initializing => _warningStyle,
                _ => EditorStyles.label
            };
        }
    }
}