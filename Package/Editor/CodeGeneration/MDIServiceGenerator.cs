using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using MDI.Core;

namespace MDI.Editor.CodeGeneration
{
    /// <summary>
    /// MDI+ Service kod üretici
    /// </summary>
    public class MDIServiceGenerator : EditorWindow
    {
        private string _serviceName = "";
        private string _serviceNamespace = "Game.Services";
        private string _outputPath = "Assets/Scripts/Services";
        private ServiceTemplate _selectedTemplate = ServiceTemplate.BasicService;
        private bool _generateInterface = true;
        private bool _generateImplementation = true;
        private bool _generateTests = false;
        private bool _addToBootstrapper = true;
        private ServiceLifetime _defaultLifetime = ServiceLifetime.Singleton;

        private bool _isValidConfiguration = false;
        private string _validationMessage = "";

        private Vector2 _scrollPosition;
        private string _previewCode = "";
        private bool _showPreview = false;

        private static GUIStyle _headerStyle;
        private static GUIStyle _templateStyle;
        private static GUIStyle _previewStyle;
        private static bool _stylesInitialized = false;

        [MenuItem("MDI+/🔧 Service Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<MDIServiceGenerator>("MDI+ Service Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            InitializeStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawServiceConfiguration();
            DrawTemplateSelection();
            DrawGenerationOptions();
            DrawOutputConfiguration();
            DrawPreviewSection();
            DrawGenerationButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_headerStyle);

            GUILayout.Label("🔧 MDI+ Service Generator", EditorStyles.largeLabel);
            GUILayout.Label("Hızlıca service interface'leri ve implementasyonları oluşturun", EditorStyles.helpBox);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawServiceConfiguration()
        {
            EditorGUILayout.LabelField("📝 Service Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            _serviceName = EditorGUILayout.TextField("Service Name:", _serviceName);
            _serviceNamespace = EditorGUILayout.TextField("Namespace:", _serviceNamespace);
            _defaultLifetime = (ServiceLifetime)EditorGUILayout.EnumPopup("Default Lifetime:", _defaultLifetime);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawTemplateSelection()
        {
            EditorGUILayout.LabelField("📋 Template Selection", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var templates = Enum.GetValues(typeof(ServiceTemplate)).Cast<ServiceTemplate>().ToArray();

            foreach (var template in templates)
            {
                EditorGUILayout.BeginHorizontal();

                var isSelected = _selectedTemplate == template;
                var newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));

                if (newSelected && !isSelected)
                {
                    _selectedTemplate = template;
                }

                EditorGUILayout.LabelField(GetTemplateDisplayName(template), EditorStyles.label);
                EditorGUILayout.LabelField(GetTemplateDescription(template), EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawGenerationOptions()
        {
            EditorGUILayout.LabelField("⚙️ Generation Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            _generateInterface = EditorGUILayout.Toggle("Generate Interface", _generateInterface);
            _generateImplementation = EditorGUILayout.Toggle("Generate Implementation", _generateImplementation);
            _generateTests = EditorGUILayout.Toggle("Generate Unit Tests", _generateTests);
            _addToBootstrapper = EditorGUILayout.Toggle("Add to Bootstrapper", _addToBootstrapper);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawOutputConfiguration()
        {
            EditorGUILayout.LabelField("📁 Output Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("Output Path:", _outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", _outputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _outputPath = FileUtil.GetProjectRelativePath(selectedPath);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("👁️ Code Preview", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_showPreview ? "Hide Preview" : "Show Preview", GUILayout.Width(100)))
            {
                _showPreview = !_showPreview;
                if (_showPreview)
                {
                    GeneratePreview();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (_showPreview)
            {
                EditorGUILayout.BeginVertical(_previewStyle);

                var previewScrollPos = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(200));
                EditorGUILayout.TextArea(_previewCode, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
        }

        private void DrawGenerationButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = IsValidConfiguration();

            if (GUILayout.Button("🚀 Generate Service", GUILayout.Height(30)))
            {
                GenerateService();
            }

            GUI.enabled = true;

            if (GUILayout.Button("🔄 Reset", GUILayout.Width(60), GUILayout.Height(30)))
            {
                ResetConfiguration();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void GeneratePreview()
        {
            if (!IsValidConfiguration())
            {
                _previewCode = "// Invalid configuration. Please check your settings.";
                return;
            }

            var generator = new ServiceCodeGenerator(_serviceName, _serviceNamespace, _selectedTemplate, _defaultLifetime);

            var preview = new StringBuilder();

            if (_generateInterface)
            {
                preview.AppendLine("// Interface:");
                preview.AppendLine(generator.GenerateInterface());
                preview.AppendLine();
            }

            if (_generateImplementation)
            {
                preview.AppendLine("// Implementation:");
                preview.AppendLine(generator.GenerateImplementation());
            }

            _previewCode = preview.ToString();
        }

        private void GenerateService()
        {
            try
            {
                ValidateConfiguration();
                if (!_isValidConfiguration)
                {
                    EditorUtility.DisplayDialog("Validation Error", _validationMessage, "OK");
                    return;
                }

                // Create output directory
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                }

                var generator = new ServiceCodeGenerator(_serviceName, _serviceNamespace, _selectedTemplate, _defaultLifetime);
                var generatedFiles = new List<string>();

                // Generate files
                if (_generateInterface)
                {
                    var interfaceCode = generator.GenerateInterface();
                    var interfacePath = Path.Combine(_outputPath, $"I{_serviceName}.cs");
                    File.WriteAllText(interfacePath, interfaceCode);
                    generatedFiles.Add(interfacePath);
                }

                if (_generateImplementation)
                {
                    var implementationCode = generator.GenerateImplementation();
                    var implementationPath = Path.Combine(_outputPath, $"{_serviceName}.cs");
                    File.WriteAllText(implementationPath, implementationCode);
                    generatedFiles.Add(implementationPath);
                }

                if (_generateTests)
                {
                    var testCode = generator.GenerateTests();
                    var testPath = Path.Combine(_outputPath, $"{_serviceName}Tests.cs");
                    File.WriteAllText(testPath, testCode);
                    generatedFiles.Add(testPath);
                }

                // Add to bootstrapper if requested
                if (_addToBootstrapper)
                {
                    AddToBootstrapper();
                }

                // Refresh asset database
                AssetDatabase.Refresh();

                var message = $"Service '{_serviceName}' successfully generated!\n\nGenerated files:\n{string.Join("\n", generatedFiles)}";
                EditorUtility.DisplayDialog("Success", message, "OK");

                ResetConfiguration();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to generate service: {ex.Message}", "OK");
            }
        }

        private void AddToBootstrapper()
        {
            // MDIBootstrapper dosyasını bul ve service kaydını ekle
            var bootstrapperFiles = Directory.GetFiles("Assets", "*Bootstrap*.cs", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("MDIBootstrapper") || File.ReadAllText(f).Contains("MDIContainer"))
                .ToArray();

            if (bootstrapperFiles.Any())
            {
                var bootstrapperPath = bootstrapperFiles.First();
                var content = File.ReadAllText(bootstrapperPath);

                // Service kaydını ekle
                var serviceRegistration = $"\n            container.Register{_defaultLifetime}<I{_serviceName}, {_serviceName}>();";

                // RegisterServices metodunu bul ve ekle
                if (content.Contains("RegisterServices"))
                {
                    var insertIndex = content.LastIndexOf("}", content.IndexOf("RegisterServices"));
                    if (insertIndex > 0)
                    {
                        content = content.Insert(insertIndex, serviceRegistration);
                        File.WriteAllText(bootstrapperPath, content);
                    }
                }
            }
        }

        private void ValidateConfiguration()
        {
            _isValidConfiguration = true;
            _validationMessage = "";

            if (string.IsNullOrEmpty(_serviceName))
            {
                _isValidConfiguration = false;
                _validationMessage = "Service name is required.";
                return;
            }

            if (string.IsNullOrEmpty(_serviceNamespace))
            {
                _isValidConfiguration = false;
                _validationMessage = "Namespace is required.";
                return;
            }

            if (string.IsNullOrEmpty(_outputPath))
            {
                _isValidConfiguration = false;
                _validationMessage = "Output path is required.";
                return;
            }

            if (!_generateInterface && !_generateImplementation)
            {
                _isValidConfiguration = false;
                _validationMessage = "At least one of Interface or Implementation must be selected.";
                return;
            }
        }

        private bool IsValidConfiguration()
        {
            ValidateConfiguration();
            return _isValidConfiguration;
        }

        private void ResetConfiguration()
        {
            _serviceName = "";
            _serviceNamespace = "Game.Services";
            _outputPath = "Assets/Scripts/Services";
            _selectedTemplate = ServiceTemplate.BasicService;
            _generateInterface = true;
            _generateImplementation = true;
            _generateTests = false;
            _addToBootstrapper = true;
            _defaultLifetime = ServiceLifetime.Singleton;
            _previewCode = "";
            _showPreview = false;
        }

        private string GetTemplateDisplayName(ServiceTemplate template)
        {
            return template switch
            {
                ServiceTemplate.BasicService => "🔧 Basic Service",
                ServiceTemplate.DataService => "💾 Data Service",
                ServiceTemplate.NetworkService => "🌐 Network Service",
                ServiceTemplate.AudioService => "🔊 Audio Service",
                ServiceTemplate.UIService => "🖼️ UI Service",
                ServiceTemplate.GameplayService => "🎮 Gameplay Service",
                ServiceTemplate.ConfigService => "⚙️ Config Service",
                _ => template.ToString()
            };
        }

        private string GetTemplateDescription(ServiceTemplate template)
        {
            return template switch
            {
                ServiceTemplate.BasicService => "Simple service with basic structure",
                ServiceTemplate.DataService => "Service for data management and persistence",
                ServiceTemplate.NetworkService => "Service for network operations",
                ServiceTemplate.AudioService => "Service for audio management",
                ServiceTemplate.UIService => "Service for UI management",
                ServiceTemplate.GameplayService => "Service for gameplay logic",
                ServiceTemplate.ConfigService => "Service for configuration management",
                _ => ""
            };
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle("box")
            {
                padding = new RectOffset(15, 15, 15, 15),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _templateStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };

            _previewStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _stylesInitialized = true;
        }
    }
}