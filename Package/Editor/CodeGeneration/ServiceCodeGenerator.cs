using System;
using System.Text;
using MDI.Core;

namespace MDI.Editor.CodeGeneration
{
    /// <summary>
    /// Service kod üretici engine
    /// </summary>
    public class ServiceCodeGenerator
    {
        private readonly string _serviceName;
        private readonly string _namespace;
        private readonly ServiceTemplate _template;
        private readonly ServiceLifetime _lifetime;
        
        public ServiceCodeGenerator(string serviceName, string namespaceName, ServiceTemplate template, ServiceLifetime lifetime)
        {
            _serviceName = serviceName;
            _namespace = namespaceName;
            _template = template;
            _lifetime = lifetime;
        }
        
        public string GenerateInterface()
        {
            var code = new StringBuilder();
            
            // Using statements
            code.AppendLine("using System;");
            code.AppendLine("using System.Threading.Tasks;");
            code.AppendLine("using UnityEngine;");
            
            if (_template == ServiceTemplate.NetworkService)
            {
                code.AppendLine("using System.Collections.Generic;");
                code.AppendLine("using UnityEngine.Networking;");
            }
            else if (_template == ServiceTemplate.DataService)
            {
                code.AppendLine("using System.Collections.Generic;");
            }
            
            code.AppendLine();
            
            // Namespace
            code.AppendLine($"namespace {_namespace}");
            code.AppendLine("{");
            
            // Interface documentation
            code.AppendLine("    /// <summary>");
            code.AppendLine($"    /// {GetServiceDescription()}");
            code.AppendLine("    /// </summary>");
            
            // Interface declaration
            code.AppendLine($"    public interface I{_serviceName}");
            code.AppendLine("    {");
            
            // Interface methods based on template
            code.Append(GenerateInterfaceMethods());
            
            code.AppendLine("    }");
            code.AppendLine("}");
            
            return code.ToString();
        }
        
        public string GenerateImplementation()
        {
            var code = new StringBuilder();
            
            // Using statements
            code.AppendLine("using System;");
            code.AppendLine("using System.Threading.Tasks;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using MDI.Attributes;");
            
            if (_template == ServiceTemplate.NetworkService)
            {
                code.AppendLine("using System.Collections.Generic;");
                code.AppendLine("using UnityEngine.Networking;");
            }
            else if (_template == ServiceTemplate.DataService)
            {
                code.AppendLine("using System.Collections.Generic;");
                code.AppendLine("using System.IO;");
            }
            
            code.AppendLine();
            
            // Namespace
            code.AppendLine($"namespace {_namespace}");
            code.AppendLine("{");
            
            // Class documentation
            code.AppendLine("    /// <summary>");
            code.AppendLine($"    /// {GetServiceDescription()}");
            code.AppendLine("    /// </summary>");
            
            // Auto-register attribute
            code.AppendLine($"    [MDIAutoRegister(typeof(I{_serviceName}), ServiceLifetime.{_lifetime})]");
            
            // Class declaration
            var baseClass = GetBaseClass();
            var interfaces = $"I{_serviceName}";
            
            if (!string.IsNullOrEmpty(baseClass))
            {
                code.AppendLine($"    public class {_serviceName} : {baseClass}, {interfaces}");
            }
            else
            {
                code.AppendLine($"    public class {_serviceName} : {interfaces}");
            }
            
            code.AppendLine("    {");
            
            // Fields and properties
            code.Append(GenerateFields());
            
            // Constructor
            code.Append(GenerateConstructor());
            
            // Implementation methods
            code.Append(GenerateImplementationMethods());
            
            // Unity lifecycle methods (if MonoBehaviour)
            if (baseClass == "MonoBehaviour")
            {
                code.Append(GenerateUnityMethods());
            }
            
            code.AppendLine("    }");
            code.AppendLine("}");
            
            return code.ToString();
        }
        
        public string GenerateTests()
        {
            var code = new StringBuilder();
            
            // Using statements
            code.AppendLine("using NUnit.Framework;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using UnityEngine.TestTools;");
            code.AppendLine("using System.Collections;");
            code.AppendLine($"using {_namespace};");
            code.AppendLine();
            
            // Namespace
            code.AppendLine($"namespace {_namespace}.Tests");
            code.AppendLine("{");
            
            // Test class
            code.AppendLine($"    public class {_serviceName}Tests");
            code.AppendLine("    {");
            
            // Setup
            code.AppendLine($"        private I{_serviceName} _service;");
            code.AppendLine();
            code.AppendLine("        [SetUp]");
            code.AppendLine("        public void Setup()");
            code.AppendLine("        {");
            code.AppendLine($"            _service = new {_serviceName}();");
            code.AppendLine("        }");
            code.AppendLine();
            
            // Basic test
            code.AppendLine("        [Test]");
            code.AppendLine($"        public void {_serviceName}_ShouldInitialize_WhenCreated()");
            code.AppendLine("        {");
            code.AppendLine("            // Arrange & Act & Assert");
            code.AppendLine("            Assert.IsNotNull(_service);");
            code.AppendLine("        }");
            code.AppendLine();
            
            // Template-specific tests
            code.Append(GenerateTemplateSpecificTests());
            
            code.AppendLine("    }");
            code.AppendLine("}");
            
            return code.ToString();
        }
        
        private string GenerateInterfaceMethods()
        {
            var methods = new StringBuilder();
            
            switch (_template)
            {
                case ServiceTemplate.BasicService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Initializes the service");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void Initialize();");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Gets the service status");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        bool IsReady { get; }");
                    break;
                    
                case ServiceTemplate.DataService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Saves data asynchronously");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        Task<bool> SaveDataAsync<T>(string key, T data);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Loads data asynchronously");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        Task<T> LoadDataAsync<T>(string key);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Checks if data exists");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        bool HasData(string key);");
                    break;
                    
                case ServiceTemplate.NetworkService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Sends a network request");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Gets connection status");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        bool IsConnected { get; }");
                    break;
                    
                case ServiceTemplate.AudioService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Plays an audio clip");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void PlaySound(AudioClip clip, float volume = 1f);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Sets master volume");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void SetMasterVolume(float volume);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Gets master volume");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        float MasterVolume { get; }");
                    break;
                    
                case ServiceTemplate.UIService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Shows a UI panel");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void ShowPanel(string panelName);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Hides a UI panel");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void HidePanel(string panelName);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Shows a notification");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void ShowNotification(string message, float duration = 3f);");
                    break;
                    
                case ServiceTemplate.GameplayService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Starts the game");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void StartGame();");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Pauses the game");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void PauseGame();");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Gets game state");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        GameState CurrentState { get; }");
                    break;
                    
                case ServiceTemplate.ConfigService:
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Gets a configuration value");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        T GetConfig<T>(string key, T defaultValue = default);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Sets a configuration value");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void SetConfig<T>(string key, T value);");
                    methods.AppendLine();
                    methods.AppendLine("        /// <summary>");
                    methods.AppendLine("        /// Saves configuration");
                    methods.AppendLine("        /// </summary>");
                    methods.AppendLine("        void SaveConfig();");
                    break;
            }
            
            methods.AppendLine();
            return methods.ToString();
        }
        
        private string GenerateFields()
        {
            var fields = new StringBuilder();
            
            switch (_template)
            {
                case ServiceTemplate.BasicService:
                    fields.AppendLine("        private bool _isReady = false;");
                    break;
                    
                case ServiceTemplate.DataService:
                    fields.AppendLine("        private readonly Dictionary<string, object> _dataCache = new Dictionary<string, object>();");
                    fields.AppendLine("        private readonly string _dataPath;");
                    break;
                    
                case ServiceTemplate.NetworkService:
                    fields.AppendLine("        private bool _isConnected = false;");
                    break;
                    
                case ServiceTemplate.AudioService:
                    fields.AppendLine("        private AudioSource _audioSource;");
                    fields.AppendLine("        private float _masterVolume = 1f;");
                    break;
                    
                case ServiceTemplate.UIService:
                    fields.AppendLine("        private readonly Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();");
                    break;
                    
                case ServiceTemplate.GameplayService:
                    fields.AppendLine("        private GameState _currentState = GameState.Menu;");
                    break;
                    
                case ServiceTemplate.ConfigService:
                    fields.AppendLine("        private readonly Dictionary<string, object> _configData = new Dictionary<string, object>();");
                    fields.AppendLine("        private readonly string _configPath;");
                    break;
            }
            
            fields.AppendLine();
            return fields.ToString();
        }
        
        private string GenerateConstructor()
        {
            var constructor = new StringBuilder();
            
            constructor.AppendLine($"        public {_serviceName}()");
            constructor.AppendLine("        {");
            
            switch (_template)
            {
                case ServiceTemplate.DataService:
                    constructor.AppendLine("            _dataPath = Path.Combine(Application.persistentDataPath, \"GameData\");");
                    constructor.AppendLine("            if (!Directory.Exists(_dataPath))");
                    constructor.AppendLine("                Directory.CreateDirectory(_dataPath);");
                    break;
                    
                case ServiceTemplate.AudioService:
                    constructor.AppendLine("            var audioObject = new GameObject(\"AudioService\");");
                    constructor.AppendLine("            _audioSource = audioObject.AddComponent<AudioSource>();");
                    constructor.AppendLine("            UnityEngine.Object.DontDestroyOnLoad(audioObject);");
                    break;
                    
                case ServiceTemplate.ConfigService:
                    constructor.AppendLine("            _configPath = Path.Combine(Application.persistentDataPath, \"config.json\");");
                    constructor.AppendLine("            LoadConfigFromFile();");
                    break;
            }
            
            constructor.AppendLine("        }");
            constructor.AppendLine();
            
            return constructor.ToString();
        }
        
        private string GenerateImplementationMethods()
        {
            var methods = new StringBuilder();
            
            switch (_template)
            {
                case ServiceTemplate.BasicService:
                    methods.AppendLine("        public void Initialize()");
                    methods.AppendLine("        {");
                    methods.AppendLine("            _isReady = true;");
                    methods.AppendLine("            Debug.Log($\"[{GetType().Name}] Service initialized\");");
                    methods.AppendLine("        }");
                    methods.AppendLine();
                    methods.AppendLine("        public bool IsReady => _isReady;");
                    break;
                    
                case ServiceTemplate.DataService:
                    methods.AppendLine("        public async Task<bool> SaveDataAsync<T>(string key, T data)");
                    methods.AppendLine("        {");
                    methods.AppendLine("            try");
                    methods.AppendLine("            {");
                    methods.AppendLine("                _dataCache[key] = data;");
                    methods.AppendLine("                var json = JsonUtility.ToJson(data);");
                    methods.AppendLine("                var filePath = Path.Combine(_dataPath, $\"{key}.json\");");
                    methods.AppendLine("                await File.WriteAllTextAsync(filePath, json);");
                    methods.AppendLine("                return true;");
                    methods.AppendLine("            }");
                    methods.AppendLine("            catch (Exception ex)");
                    methods.AppendLine("            {");
                    methods.AppendLine("                Debug.LogError($\"Failed to save data: {ex.Message}\");");
                    methods.AppendLine("                return false;");
                    methods.AppendLine("            }");
                    methods.AppendLine("        }");
                    methods.AppendLine();
                    methods.AppendLine("        public async Task<T> LoadDataAsync<T>(string key)");
                    methods.AppendLine("        {");
                    methods.AppendLine("            if (_dataCache.TryGetValue(key, out var cachedData))");
                    methods.AppendLine("                return (T)cachedData;");
                    methods.AppendLine();
                    methods.AppendLine("            var filePath = Path.Combine(_dataPath, $\"{key}.json\");");
                    methods.AppendLine("            if (File.Exists(filePath))");
                    methods.AppendLine("            {");
                    methods.AppendLine("                var json = await File.ReadAllTextAsync(filePath);");
                    methods.AppendLine("                var data = JsonUtility.FromJson<T>(json);");
                    methods.AppendLine("                _dataCache[key] = data;");
                    methods.AppendLine("                return data;");
                    methods.AppendLine("            }");
                    methods.AppendLine();
                    methods.AppendLine("            return default(T);");
                    methods.AppendLine("        }");
                    methods.AppendLine();
                    methods.AppendLine("        public bool HasData(string key)");
                    methods.AppendLine("        {");
                    methods.AppendLine("            return _dataCache.ContainsKey(key) || File.Exists(Path.Combine(_dataPath, $\"{key}.json\"));");
                    methods.AppendLine("        }");
                    break;
                    
                // Diğer template'ler için benzer implementasyonlar...
                default:
                    methods.AppendLine("        // TODO: Implement interface methods");
                    break;
            }
            
            methods.AppendLine();
            return methods.ToString();
        }
        
        private string GenerateUnityMethods()
        {
            var methods = new StringBuilder();
            
            methods.AppendLine("        private void Awake()");
            methods.AppendLine("        {");
            methods.AppendLine("            // Unity Awake initialization");
            methods.AppendLine("        }");
            methods.AppendLine();
            
            methods.AppendLine("        private void Start()");
            methods.AppendLine("        {");
            methods.AppendLine("            // Unity Start initialization");
            methods.AppendLine("        }");
            methods.AppendLine();
            
            return methods.ToString();
        }
        
        private string GenerateTemplateSpecificTests()
        {
            var tests = new StringBuilder();
            
            switch (_template)
            {
                case ServiceTemplate.BasicService:
                    tests.AppendLine("        [Test]");
                    tests.AppendLine("        public void Initialize_ShouldSetReady_WhenCalled()");
                    tests.AppendLine("        {");
                    tests.AppendLine("            // Act");
                    tests.AppendLine("            _service.Initialize();");
                    tests.AppendLine();
                    tests.AppendLine("            // Assert");
                    tests.AppendLine("            Assert.IsTrue(_service.IsReady);");
                    tests.AppendLine("        }");
                    break;
                    
                default:
                    tests.AppendLine("        // TODO: Add template-specific tests");
                    break;
            }
            
            tests.AppendLine();
            return tests.ToString();
        }
        
        private string GetServiceDescription()
        {
            return _template switch
            {
                ServiceTemplate.BasicService => "Basic service implementation",
                ServiceTemplate.DataService => "Service for data management and persistence",
                ServiceTemplate.NetworkService => "Service for network operations",
                ServiceTemplate.AudioService => "Service for audio management",
                ServiceTemplate.UIService => "Service for UI management",
                ServiceTemplate.GameplayService => "Service for gameplay logic",
                ServiceTemplate.ConfigService => "Service for configuration management",
                _ => "Service implementation"
            };
        }
        
        private string GetBaseClass()
        {
            return _template switch
            {
                ServiceTemplate.AudioService => "MonoBehaviour",
                ServiceTemplate.UIService => "MonoBehaviour",
                ServiceTemplate.GameplayService => "MonoBehaviour",
                _ => null
            };
        }
    }
    
    /// <summary>
    /// Game state enum for GameplayService
    /// </summary>
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }
}