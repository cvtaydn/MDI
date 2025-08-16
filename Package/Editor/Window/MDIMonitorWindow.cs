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
    /// MDI+ Service Monitor Editor Window - Enhanced Version
    /// </summary>
    public class MDIMonitorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "🔧 Services", "🔗 Dependencies", "📊 Performance", "📝 Logs", "⚙️ Settings" };
        
        private MDIContainer _currentContainer;
        private bool _autoRefresh = true;
        private float _refreshInterval = 1.0f;
        private double _lastRefreshTime;
        
        private string _searchFilter = "";
        private ServiceStatus _statusFilter = ServiceStatus.Initialized;
        private bool _showAllStatuses = true;
        private bool _showOnlyErrors = false;
        private bool _compactView = false;
        private bool _showPerformanceMetrics = true;
        private bool _showMemoryUsage = true;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _serviceBoxStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _successStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toolbarStyle;
        
        // Colors
        private readonly Color _healthyColor = new Color(0.2f, 0.8f, 0.2f);
        private readonly Color _warningColor = new Color(1f, 0.8f, 0.2f);
        private readonly Color _errorColor = new Color(0.8f, 0.2f, 0.2f);
        private readonly Color _infoColor = new Color(0.3f, 0.7f, 1f);
        
        // Performance tracking
        private Dictionary<Type, float> _lastResolveTime = new Dictionary<Type, float>();
        private List<string> _recentLogs = new List<string>();
        private int _maxLogCount = 100;
        
        [MenuItem("MDI+/🔍 Service Monitor", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<MDIMonitorWindow>("MDI+ Monitor");
            window.minSize = new Vector2(900, 700);
            window.titleContent = new GUIContent("MDI+ Monitor", "MDI+ Service Monitor");
            window.Show();
        }
        
        private void OnEnable()
        {
            FindCurrentContainer();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                FindCurrentContainer();
                Repaint();
            }
        }
        
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.gray }
            };
            
            _serviceBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(5, 5, 3, 3),
                normal = { background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.2f)) }
            };
            
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = _errorColor },
                fontStyle = FontStyle.Bold
            };
            
            _successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = _healthyColor },
                fontStyle = FontStyle.Bold
            };
            
            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = _warningColor },
                fontStyle = FontStyle.Bold
            };
            
            _infoStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = _infoColor }
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            _toolbarStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 25
            };
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        private void FindCurrentContainer()
        {
            // Global container'ı kontrol et
            try
            {
                if (Application.isPlaying)
                {
                    // Runtime'da MDI.GlobalContainer'ı kullan
                    var mdiType = System.Type.GetType("MDI.Core.MDI, Assembly-CSharp");
                    if (mdiType != null)
                    {
                        var globalContainerProperty = mdiType.GetProperty("GlobalContainer");
                        if (globalContainerProperty != null)
                        {
                            _currentContainer = globalContainerProperty.GetValue(null) as MDIContainer;
                        }
                    }
                }
                
                // Eğer bulunamadıysa, scene'deki MDIBootstrapper'ı ara
                if (_currentContainer == null)
                {
                    var bootstrapper = FindObjectOfType<MonoBehaviour>()
                        ?.GetComponents<MonoBehaviour>()
                        ?.FirstOrDefault(mb => mb.GetType().Name.Contains("Bootstrap"));
                    
                    if (bootstrapper != null)
                    {
                        var containerField = bootstrapper.GetType()
                            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .FirstOrDefault(f => f.FieldType == typeof(MDIContainer));
                        
                        if (containerField != null)
                        {
                            _currentContainer = containerField.GetValue(bootstrapper) as MDIContainer;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Container bulunamadı: {ex.Message}");
            }
        }
        
        private void OnGUI()
        {
            if (_headerStyle == null)
                InitializeStyles();
            
            // Auto refresh
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                FindCurrentContainer();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
            
            DrawHeader();
            DrawToolbar();
            
            if (_currentContainer == null)
            {
                DrawNoContainerMessage();
                return;
            }
            
            DrawTabs();
            
            EditorGUILayout.Space(5);
            
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
                case 4:
                    DrawSettingsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("🚀 MDI+ Service Monitor", _headerStyle, GUILayout.ExpandWidth(true));
            
            // Container status
            if (_currentContainer != null)
            {
                var healthStatus = _currentContainer.HealthChecker?.GetOverallHealth() ?? HealthStatus.Unknown;
                var statusColor = GetHealthStatusColor(healthStatus);
                var oldColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label($"● {healthStatus}", _titleStyle, GUILayout.Width(100));
                GUI.color = oldColor;
            }
            else
            {
                GUI.color = _errorColor;
                GUILayout.Label("● Disconnected", _titleStyle, GUILayout.Width(100));
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(_toolbarStyle);
            
            // Refresh button
            if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                FindCurrentContainer();
                Repaint();
            }
            
            // Auto refresh toggle
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(50));
            
            // Refresh interval
            GUILayout.Label("Interval:", EditorStyles.miniLabel, GUILayout.Width(50));
            _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 5.0f, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            // Search filter
            GUILayout.Label("🔍", EditorStyles.miniLabel, GUILayout.Width(20));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
            
            // Clear search
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int i = 0; i < _tabNames.Length; i++)
            {
                var style = _selectedTab == i ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
                var oldColor = GUI.backgroundColor;
                
                if (_selectedTab == i)
                {
                    GUI.backgroundColor = _infoColor;
                }
                
                if (GUILayout.Button(_tabNames[i], style, GUILayout.Height(25)))
                {
                    _selectedTab = i;
                }
                
                GUI.backgroundColor = oldColor;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNoContainerMessage()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(400), GUILayout.Height(200));
            
            GUILayout.Space(20);
            GUILayout.Label("🔍 Container Bulunamadı", _titleStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            
            GUILayout.Label("MDI+ Container aktif değil veya bulunamadı.", _subtitleStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            
            if (Application.isPlaying)
            {
                GUILayout.Label("• Play mode'da MDI.GlobalContainer kontrol ediliyor", EditorStyles.wordWrappedLabel);
                GUILayout.Label("• Scene'deki MDIBootstrapper aranıyor", EditorStyles.wordWrappedLabel);
            }
            else
            {
                GUILayout.Label("• Play mode'a geçin", EditorStyles.wordWrappedLabel);
                GUILayout.Label("• MDIBootstrapper ekleyin", EditorStyles.wordWrappedLabel);
                GUILayout.Label("• MDI.GlobalContainer'ı başlatın", EditorStyles.wordWrappedLabel);
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("🚀 Setup Wizard'ı Aç", _buttonStyle, GUILayout.Height(30)))
            {
                MDISetupWizard.ShowWizard();
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawServicesTab()
        {
            DrawServiceFilters();
            
            if (_currentContainer.ServiceDescriptors == null || !_currentContainer.ServiceDescriptors.Any())
            {
                EditorGUILayout.HelpBox("Henüz kayıtlı service bulunmuyor.", MessageType.Info);
                return;
            }
            
            var filteredServices = FilterServices(_currentContainer.ServiceDescriptors);
            
            EditorGUILayout.LabelField($"📦 Services ({filteredServices.Count()}/{_currentContainer.ServiceDescriptors.Count()})", _titleStyle);
            EditorGUILayout.Space(5);
            
            foreach (var kvp in filteredServices)
            {
                DrawServiceNode(kvp.Key, kvp.Value);
            }
        }
        
        private void DrawServiceFilters()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Status filter
            GUILayout.Label("Status:", EditorStyles.miniLabel, GUILayout.Width(50));
            _showAllStatuses = GUILayout.Toggle(_showAllStatuses, "All", EditorStyles.toolbarButton, GUILayout.Width(40));
            
            if (!_showAllStatuses)
            {
                _statusFilter = (ServiceStatus)EditorGUILayout.EnumPopup(_statusFilter, EditorStyles.toolbarPopup, GUILayout.Width(100));
            }
            
            GUILayout.Space(10);
            
            // View options
            _showOnlyErrors = GUILayout.Toggle(_showOnlyErrors, "Errors Only", EditorStyles.toolbarButton, GUILayout.Width(80));
            _compactView = GUILayout.Toggle(_compactView, "Compact", EditorStyles.toolbarButton, GUILayout.Width(70));
            
            GUILayout.FlexibleSpace();
            
            // Performance toggles
            _showPerformanceMetrics = GUILayout.Toggle(_showPerformanceMetrics, "📊 Metrics", EditorStyles.toolbarButton, GUILayout.Width(80));
            _showMemoryUsage = GUILayout.Toggle(_showMemoryUsage, "💾 Memory", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
        }
        
        private IEnumerable<KeyValuePair<Type, ServiceDescriptor>> FilterServices(IEnumerable<KeyValuePair<Type, ServiceDescriptor>> services)
        {
            var filtered = services;
            
            // Search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(kvp => 
                    kvp.Key.Name.ToLower().Contains(_searchFilter.ToLower()) ||
                    kvp.Value.ImplementationType?.Name.ToLower().Contains(_searchFilter.ToLower()) == true);
            }
            
            // Status filter
            if (!_showAllStatuses)
            {
                filtered = filtered.Where(kvp => GetServiceStatus(kvp.Value) == _statusFilter);
            }
            
            // Error filter
            if (_showOnlyErrors)
            {
                filtered = filtered.Where(kvp => GetServiceStatus(kvp.Value) == ServiceStatus.Error);
            }
            
            return filtered;
        }
        
        private void DrawServiceNode(Type serviceType, ServiceDescriptor descriptor)
        {
            var status = GetServiceStatus(descriptor);
            var statusColor = GetStatusColor(status);
            
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(GetStatusIcon(status), _titleStyle, GUILayout.Width(20));
            GUI.color = oldColor;
            
            // Service name
            GUILayout.Label(serviceType.Name, _titleStyle, GUILayout.ExpandWidth(true));
            
            // Lifetime badge
            DrawLifetimeBadge(descriptor.Lifetime);
            
            EditorGUILayout.EndHorizontal();
            
            if (!_compactView)
            {
                // Implementation type
                if (descriptor.ImplementationType != null)
                {
                    GUILayout.Label($"Implementation: {descriptor.ImplementationType.Name}", _subtitleStyle);
                }
                
                // Performance metrics
                if (_showPerformanceMetrics && _currentContainer.ServiceMonitor != null)
                {
                    var metrics = _currentContainer.ServiceMonitor.Metrics;
                    if (metrics.TryGetValue(serviceType, out var metric))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label($"📊 Resolves: {metric.TotalResolveCount}", _infoStyle, GUILayout.Width(120));
                        GUILayout.Label($"⏱️ Avg: {metric.AverageResolveTime:F2}ms", _infoStyle, GUILayout.Width(100));
                        if (metric.ErrorCount > 0)
                        {
                            GUILayout.Label($"❌ Errors: {metric.ErrorCount}", _errorStyle);
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        if (_showMemoryUsage)
                        {
                            GUILayout.Label($"💾 Memory: {FormatBytes(metric.MemoryUsage)}", _infoStyle);
                        }
                    }
                }
                
                // Dependencies
                if (_currentContainer.DependencyGraph != null)
                {
                    var dependencies = _currentContainer.DependencyGraph.GetDependencies(serviceType);
                    if (dependencies.Any())
                    {
                        GUILayout.Label($"🔗 Dependencies: {string.Join(", ", dependencies.Select(d => d.Name).ToArray())}", _subtitleStyle);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLifetimeBadge(ServiceLifetime lifetime)
        {
            var badgeColor = lifetime switch
            {
                ServiceLifetime.Singleton => _healthyColor,
                ServiceLifetime.Scoped => _warningColor,
                ServiceLifetime.Transient => _infoColor,
                ServiceLifetime.Lazy => Color.magenta,
                _ => Color.gray
            };
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = badgeColor;
            GUILayout.Label(lifetime.ToString(), EditorStyles.miniButton, GUILayout.Width(80));
            GUI.backgroundColor = oldColor;
        }
        
        private void DrawDependenciesTab()
        {
            if (_currentContainer.DependencyGraph == null)
            {
                EditorGUILayout.HelpBox("Dependency graph mevcut değil.", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.LabelField("🔗 Dependency Graph", _titleStyle);
            EditorGUILayout.Space(5);
            
            // Circular dependencies check
            var hasCircularDeps = _currentContainer.DependencyGraph.DetectCircularDependencies();
            if (hasCircularDeps)
            {
                var circularDeps = _currentContainer.DependencyGraph.CircularDependencies;
                EditorGUILayout.HelpBox($"⚠️ Circular dependencies detected: {string.Join(", ", circularDeps)}", MessageType.Warning);
            }
            
            // Dependency tree
            foreach (var node in _currentContainer.DependencyGraph.GetAllNodes())
            {
                DrawDependencyNode(node, 0);
            }
        }
        
        private void DrawDependencyNode(DependencyNode node, int depth)
        {
            var indent = new string(' ', depth * 4);
            var status = GetServiceStatus(_currentContainer.ServiceDescriptors[node.ServiceType]);
            var statusIcon = GetStatusIcon(status);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{indent}{statusIcon} {node.ServiceType.Name}", GetStatusStyle(status));
            EditorGUILayout.EndHorizontal();
            
            if (depth < 3) // Prevent infinite recursion
            {
                foreach (var dependency in node.Dependencies)
                {
                    var dependencyNode = _currentContainer.DependencyGraph.GetNode(dependency);
                    if (dependencyNode != null)
                    {
                        DrawDependencyNode(dependencyNode, depth + 1);
                    }
                }
            }
        }
        
        private void DrawPerformanceTab()
        {
            if (_currentContainer.ServiceMonitor == null)
            {
                EditorGUILayout.HelpBox("Service monitor mevcut değil.", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.LabelField("📊 Performance Metrics", _titleStyle);
            EditorGUILayout.Space(5);
            
            // Quick stats
            var metrics = _currentContainer.ServiceMonitor.Metrics;
            if (metrics.Any())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                
                var totalResolves = metrics.Values.Sum(m => m.TotalResolveCount);
                var avgResolveTime = metrics.Values.Where(m => m.TotalResolveCount > 0).Average(m => m.AverageResolveTime);
                var totalErrors = metrics.Values.Sum(m => m.ErrorCount);
                
                GUILayout.Label($"📦 Services: {metrics.Count}", EditorStyles.miniLabel);
                GUILayout.Label($"🔄 Total Resolves: {totalResolves}", EditorStyles.miniLabel);
                GUILayout.Label($"⏱️ Avg Time: {avgResolveTime:F2}ms", EditorStyles.miniLabel);
                GUILayout.Label($"❌ Errors: {totalErrors}", EditorStyles.miniLabel);
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("📋 Generate Report", EditorStyles.toolbarButton))
                {
                    var report = _currentContainer.ServiceMonitor.GeneratePerformanceReport();
                    Debug.Log(report);
                    EditorUtility.DisplayDialog("Performance Report", "Report generated and logged to console.", "OK");
                }
                
                if (GUILayout.Button("📤 Export JSON", EditorStyles.toolbarButton))
                {
                    ExportMetricsToJson();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                // Detailed metrics
                foreach (var kvp in metrics.OrderByDescending(m => m.Value.TotalResolveCount))
                {
                    DrawPerformanceMetric(kvp.Key, kvp.Value);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Henüz performance verisi yok.", MessageType.Info);
            }
        }
        
        private void DrawPerformanceMetric(Type serviceType, ServiceMetrics metric)
        {
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(serviceType.Name, _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Resolves: {metric.TotalResolveCount}", _infoStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Avg: {metric.AverageResolveTime:F2}ms", EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.Label($"Min: {metric.MinResolveTime:F2}ms", EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.Label($"Max: {metric.MaxResolveTime:F2}ms", EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.Label($"Memory: {FormatBytes(metric.MemoryUsage)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            if (metric.ErrorCount > 0)
            {
                GUILayout.Label($"❌ Errors: {metric.ErrorCount} - Last: {metric.LastError}", _errorStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("📝 Service Logs", _titleStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("🧹 Clear", EditorStyles.toolbarButton))
            {
                _recentLogs.Clear();
                if (_currentContainer.ServiceMonitor != null)
                {
                    _currentContainer.ServiceMonitor.ClearLogs();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (_currentContainer.ServiceMonitor != null)
            {
                var logs = _currentContainer.ServiceMonitor.Logs;
                if (logs.Any())
                {
                    foreach (var log in logs.TakeLast(50))
                    {
                        var logStyle = log.Contains("ERROR") ? _errorStyle : 
                                      log.Contains("WARNING") ? _warningStyle : 
                                      log.Contains("SUCCESS") ? _successStyle : 
                                      EditorStyles.label;
                        
                        GUILayout.Label(log, logStyle);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Henüz log kaydı yok.", MessageType.Info);
                }
            }
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("⚙️ Monitor Settings", _titleStyle);
            EditorGUILayout.Space(10);
            
            // Refresh settings
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            GUILayout.Label("🔄 Refresh Settings", _subtitleStyle);
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);
            _refreshInterval = EditorGUILayout.Slider("Refresh Interval (s)", _refreshInterval, 0.1f, 10.0f);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Display settings
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            GUILayout.Label("🎨 Display Settings", _subtitleStyle);
            _compactView = EditorGUILayout.Toggle("Compact View", _compactView);
            _showPerformanceMetrics = EditorGUILayout.Toggle("Show Performance Metrics", _showPerformanceMetrics);
            _showMemoryUsage = EditorGUILayout.Toggle("Show Memory Usage", _showMemoryUsage);
            _maxLogCount = EditorGUILayout.IntSlider("Max Log Count", _maxLogCount, 50, 500);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Actions
            EditorGUILayout.BeginVertical(_serviceBoxStyle);
            GUILayout.Label("🔧 Actions", _subtitleStyle);
            
            if (GUILayout.Button("🚀 Open Setup Wizard", _buttonStyle))
            {
                MDISetupWizard.ShowWizard();
            }
            
            if (GUILayout.Button("📊 Generate Full Report", _buttonStyle))
            {
                GenerateFullReport();
            }
            
            if (GUILayout.Button("🧹 Clear All Data", _buttonStyle))
            {
                if (EditorUtility.DisplayDialog("Clear Data", "Are you sure you want to clear all monitoring data?", "Yes", "No"))
                {
                    ClearAllData();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ExportMetricsToJson()
        {
            if (_currentContainer?.ServiceMonitor?.Metrics == null)
                return;
            
            var path = EditorUtility.SaveFilePanel("Export Metrics", "", "mdi_metrics", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var json = JsonUtility.ToJson(_currentContainer.ServiceMonitor.Metrics, true);
                    System.IO.File.WriteAllText(path, json);
                    EditorUtility.DisplayDialog("Export Complete", $"Metrics exported to {path}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Failed", $"Failed to export metrics: {ex.Message}", "OK");
                }
            }
        }
        
        private void GenerateFullReport()
        {
            if (_currentContainer == null)
                return;
            
            var report = $"MDI+ Full System Report\n" +
                        $"Generated: {DateTime.Now}\n" +
                        $"Container Status: {(_currentContainer.HealthChecker?.GetOverallHealth() ?? HealthStatus.Unknown)}\n" +
                        $"Services: {_currentContainer.ServiceDescriptors?.Count ?? 0}\n";
            
            if (_currentContainer.ServiceMonitor != null)
            {
                report += _currentContainer.ServiceMonitor.GeneratePerformanceReport();
            }
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("Full Report", "Full system report generated and logged to console.", "OK");
        }
        
        private void ClearAllData()
        {
            _recentLogs.Clear();
            _lastResolveTime.Clear();
            _currentContainer?.ServiceMonitor?.ClearLogs();
            Repaint();
        }
        
        // Helper methods
        private ServiceStatus GetServiceStatus(ServiceDescriptor descriptor)
        {
            if (descriptor?.Instance != null)
                return ServiceStatus.Initialized;
            if (descriptor?.ImplementationType != null)
                return ServiceStatus.NotInitialized;
            return ServiceStatus.Error;
        }
        
        private string GetStatusIcon(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Initialized => "✅",
                ServiceStatus.NotInitialized => "📦",
                ServiceStatus.Initializing => "⏳",
                ServiceStatus.Error => "❌",
                ServiceStatus.Disposed => "⚫",
                _ => "❓"
            };
        }
        
        private Color GetStatusColor(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Initialized => _healthyColor,
                ServiceStatus.NotInitialized => _infoColor,
                ServiceStatus.Initializing => _warningColor,
                ServiceStatus.Error => _errorColor,
                ServiceStatus.Disposed => Color.gray,
                _ => Color.gray
            };
        }
        
        private Color GetHealthStatusColor(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => _healthyColor,
                HealthStatus.Warning => _warningColor,
                HealthStatus.Critical or HealthStatus.Unhealthy => _errorColor,
                _ => Color.gray
            };
        }
        
        private GUIStyle GetStatusStyle(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Initialized => _successStyle,
                ServiceStatus.Error => _errorStyle,
                ServiceStatus.Initializing => _warningStyle,
                _ => EditorStyles.label
            };
        }
        
        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}