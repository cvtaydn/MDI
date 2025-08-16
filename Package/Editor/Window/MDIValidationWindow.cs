using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MDI.Editor.Validation;

namespace MDI.Editor.Window
{
    /// <summary>
    /// MDI+ Dependency Validation Window
    /// </summary>
    public class MDIValidationWindow : EditorWindow
    {
        private ValidationResult _lastValidationResult;
        private Vector2 _scrollPosition;
        private bool _autoValidate = true;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfos = true;
        private string _searchFilter = "";
        private ValidationCategory _selectedCategory = ValidationCategory.All;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toolbarStyle;
        
        // Colors
        private readonly Color _errorColor = new Color(1f, 0.3f, 0.3f);
        private readonly Color _warningColor = new Color(1f, 0.8f, 0.2f);
        private readonly Color _infoColor = new Color(0.3f, 0.8f, 1f);
        private readonly Color _successColor = new Color(0.3f, 1f, 0.3f);
        
        private float _lastValidationTime;
        private const float VALIDATION_INTERVAL = 2f;
        
        [MenuItem("MDI+/🔍 Dependency Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<MDIValidationWindow>("MDI+ Dependency Validator");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeStyles();
            
            // Subscribe to validation events
            MDIDependencyValidator.Instance.OnValidationCompleted += OnValidationCompleted;
            
            // Perform initial validation
            PerformValidation();
            
            // Auto-validate during play mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            if (MDIDependencyValidator.Instance != null)
            {
                MDIDependencyValidator.Instance.OnValidationCompleted -= OnValidationCompleted;
            }
            
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                PerformValidation();
            }
        }
        
        private void OnValidationCompleted(ValidationResult result)
        {
            _lastValidationResult = result;
            Repaint();
        }
        
        private void OnGUI()
        {
            if (_headerStyle == null)
                InitializeStyles();
            
            DrawHeader();
            DrawToolbar();
            DrawValidationResults();
            
            // Auto-validate periodically
            if (_autoValidate && Time.realtimeSinceStartup - _lastValidationTime > VALIDATION_INTERVAL)
            {
                PerformValidation();
                _lastValidationTime = Time.realtimeSinceStartup;
            }
        }
        
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _errorStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = _errorColor },
                fontSize = 12,
                wordWrap = true
            };
            
            _warningStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = _warningColor },
                fontSize = 12,
                wordWrap = true
            };
            
            _infoStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = _infoColor },
                fontSize = 12,
                wordWrap = true
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            _toolbarStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 25
            };
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUILayout.Label("🔍 MDI+ Dependency Validator", _headerStyle);
            
            EditorGUILayout.Space(5);
            
            // Status indicator
            var statusText = "Unknown";
            var statusColor = Color.gray;
            
            if (_lastValidationResult != null)
            {
                if (_lastValidationResult.IsValid)
                {
                    statusText = "✅ All Dependencies Valid";
                    statusColor = _successColor;
                }
                else
                {
                    statusText = $"❌ {_lastValidationResult.Errors.Count} Error(s) Found";
                    statusColor = _errorColor;
                }
            }
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusText, EditorStyles.centeredGreyMiniLabel);
            GUI.color = originalColor;
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(_toolbarStyle);
            
            // Validate button
            if (GUILayout.Button("🔄 Validate Now", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                PerformValidation();
            }
            
            GUILayout.Space(10);
            
            // Auto-validate toggle
            _autoValidate = GUILayout.Toggle(_autoValidate, "Auto Validate", EditorStyles.toolbarButton, GUILayout.Width(100));
            
            GUILayout.Space(10);
            
            // Category filter
            GUILayout.Label("Category:", EditorStyles.miniLabel, GUILayout.Width(60));
            _selectedCategory = (ValidationCategory)EditorGUILayout.EnumPopup(_selectedCategory, EditorStyles.toolbarPopup, GUILayout.Width(100));
            
            GUILayout.Space(10);
            
            // Search filter
            GUILayout.Label("Search:", EditorStyles.miniLabel, GUILayout.Width(50));
            _searchFilter = GUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            
            GUILayout.FlexibleSpace();
            
            // Toggle buttons
            _showErrors = GUILayout.Toggle(_showErrors, $"Errors ({(_lastValidationResult?.Errors.Count ?? 0)})", EditorStyles.toolbarButton, GUILayout.Width(80));
            _showWarnings = GUILayout.Toggle(_showWarnings, $"Warnings ({(_lastValidationResult?.Warnings.Count ?? 0)})", EditorStyles.toolbarButton, GUILayout.Width(90));
            _showInfos = GUILayout.Toggle(_showInfos, $"Info ({(_lastValidationResult?.Infos.Count ?? 0)})", EditorStyles.toolbarButton, GUILayout.Width(70));
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawValidationResults()
        {
            if (_lastValidationResult == null)
            {
                EditorGUILayout.HelpBox("No validation results available. Click 'Validate Now' to start.", MessageType.Info);
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            var filteredResults = GetFilteredResults();
            
            if (filteredResults.Count == 0)
            {
                EditorGUILayout.HelpBox("No results match the current filter.", MessageType.Info);
            }
            else
            {
                foreach (var result in filteredResults)
                {
                    DrawValidationItem(result);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // Summary
            DrawSummary();
        }
        
        private void DrawValidationItem(ValidationItem item)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // Icon and type
            var icon = GetIconForType(item.Type);
            var style = GetStyleForType(item.Type);
            
            GUILayout.Label(icon, GUILayout.Width(20));
            GUILayout.Label(item.Type.ToString().ToUpper(), EditorStyles.boldLabel, GUILayout.Width(80));
            
            // Message
            GUILayout.Label(item.Message, style);
            
            GUILayout.FlexibleSpace();
            
            // Actions
            if (item.Type == ValidationItemType.Error)
            {
                if (GUILayout.Button("🔧 Fix", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    // TODO: Implement auto-fix functionality
                    Debug.Log($"Auto-fix for: {item.Message}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Additional details
            if (!string.IsNullOrEmpty(item.Details))
            {
                EditorGUILayout.Space(2);
                GUILayout.Label(item.Details, EditorStyles.wordWrappedMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        private void DrawSummary()
        {
            if (_lastValidationResult == null) return;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("Summary:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // Error count
            var originalColor = GUI.color;
            if (_lastValidationResult.HasErrors)
            {
                GUI.color = _errorColor;
                GUILayout.Label($"❌ {_lastValidationResult.Errors.Count} Errors", EditorStyles.miniLabel);
            }
            
            // Warning count
            if (_lastValidationResult.HasWarnings)
            {
                GUI.color = _warningColor;
                GUILayout.Label($"⚠️ {_lastValidationResult.Warnings.Count} Warnings", EditorStyles.miniLabel);
            }
            
            // Info count
            if (_lastValidationResult.HasInfos)
            {
                GUI.color = _infoColor;
                GUILayout.Label($"ℹ️ {_lastValidationResult.Infos.Count} Info", EditorStyles.miniLabel);
            }
            
            if (_lastValidationResult.IsValid)
            {
                GUI.color = _successColor;
                GUILayout.Label("✅ All Valid", EditorStyles.miniLabel);
            }
            
            GUI.color = originalColor;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private List<ValidationItem> GetFilteredResults()
        {
            var results = new List<ValidationItem>();
            
            if (_lastValidationResult == null) return results;
            
            // Add errors
            if (_showErrors)
            {
                results.AddRange(_lastValidationResult.Errors.Select(e => new ValidationItem
                {
                    Type = ValidationItemType.Error,
                    Message = e,
                    Category = GetCategoryFromMessage(e)
                }));
            }
            
            // Add warnings
            if (_showWarnings)
            {
                results.AddRange(_lastValidationResult.Warnings.Select(w => new ValidationItem
                {
                    Type = ValidationItemType.Warning,
                    Message = w,
                    Category = GetCategoryFromMessage(w)
                }));
            }
            
            // Add infos
            if (_showInfos)
            {
                results.AddRange(_lastValidationResult.Infos.Select(i => new ValidationItem
                {
                    Type = ValidationItemType.Info,
                    Message = i,
                    Category = GetCategoryFromMessage(i)
                }));
            }
            
            // Apply category filter
            if (_selectedCategory != ValidationCategory.All)
            {
                results = results.Where(r => r.Category == _selectedCategory).ToList();
            }
            
            // Apply search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                results = results.Where(r => r.Message.ToLower().Contains(_searchFilter.ToLower())).ToList();
            }
            
            return results;
        }
        
        private ValidationCategory GetCategoryFromMessage(string message)
        {
            if (message.Contains("Circular") || message.Contains("circular"))
                return ValidationCategory.CircularDependency;
            if (message.Contains("not registered") || message.Contains("missing"))
                return ValidationCategory.MissingDependency;
            if (message.Contains("lifetime") || message.Contains("Lifetime"))
                return ValidationCategory.LifetimeIssue;
            if (message.Contains("injection") || message.Contains("Inject"))
                return ValidationCategory.InjectionIssue;
            
            return ValidationCategory.General;
        }
        
        private string GetIconForType(ValidationItemType type)
        {
            return type switch
            {
                ValidationItemType.Error => "❌",
                ValidationItemType.Warning => "⚠️",
                ValidationItemType.Info => "ℹ️",
                _ => "❓"
            };
        }
        
        private GUIStyle GetStyleForType(ValidationItemType type)
        {
            return type switch
            {
                ValidationItemType.Error => _errorStyle,
                ValidationItemType.Warning => _warningStyle,
                ValidationItemType.Info => _infoStyle,
                _ => EditorStyles.label
            };
        }
        
        private void PerformValidation()
        {
            try
            {
                _lastValidationResult = MDIDependencyValidator.Instance.ValidateAll();
                _lastValidationTime = Time.realtimeSinceStartup;
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Validation failed: {ex.Message}");
            }
        }
    }
    
    public enum ValidationCategory
    {
        All,
        CircularDependency,
        MissingDependency,
        LifetimeIssue,
        InjectionIssue,
        General
    }
    
    public enum ValidationItemType
    {
        Error,
        Warning,
        Info
    }
    
    public class ValidationItem
    {
        public ValidationItemType Type { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public ValidationCategory Category { get; set; }
    }
}