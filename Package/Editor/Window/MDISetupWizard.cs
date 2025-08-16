using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using MDI.Containers;
using MDI.Extensions;

namespace MDI.Editor.Window
{
    /// <summary>
    /// MDI+ Setup Wizard - Kullanıcı dostu kurulum rehberi
    /// </summary>
    public class MDISetupWizard : EditorWindow
    {
        private int _currentStep = 0;
        private readonly string[] _stepTitles = {
            "Hoş Geldiniz",
            "Container Kurulumu",
            "Service Kayıtları",
            "Injection Ayarları",
            "Tamamlandı"
        };

        private Vector2 _scrollPosition;
        private bool _createBootstrapper = true;
        private bool _enableMonitoring = true;
        private bool _enableHealthCheck = true;
        private bool _createExampleServices = true;
        private string _gameManagerName = "GameManager";
        private string _bootstrapperName = "MDIBootstrapper";

        // Styles
        private GUIStyle _titleStyle;
        private GUIStyle _stepStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _boxStyle;

        [MenuItem("MDI+/Setup Wizard", false, 0)]
        public static void ShowWizard()
        {
            var wizard = GetWindow<MDISetupWizard>("MDI+ Setup Wizard");
            wizard.minSize = new Vector2(600, 500);
            wizard.maxSize = new Vector2(600, 500);
            wizard.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            _titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.6f, 1f) }
            };

            _stepStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 30
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 15, 15),
                margin = new RectOffset(10, 10, 10, 10)
            };
        }

        private void OnGUI()
        {
            if (_titleStyle == null)
                InitializeStyles();

            DrawHeader();
            DrawProgressBar();

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_currentStep)
            {
                case 0: DrawWelcomeStep(); break;
                case 1: DrawContainerSetupStep(); break;
                case 2: DrawServiceRegistrationStep(); break;
                case 3: DrawInjectionSetupStep(); break;
                case 4: DrawCompletionStep(); break;
            }

            EditorGUILayout.EndScrollView();

            DrawNavigationButtons();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MDI+ Kurulum Rehberi", _titleStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(_stepTitles[_currentStep], _stepStyle);
            EditorGUILayout.Space(10);
        }

        private void DrawProgressBar()
        {
            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            rect.x += 20;
            rect.width -= 40;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));

            // Progress
            var progressRect = new Rect(rect.x, rect.y, rect.width * (_currentStep + 1) / _stepTitles.Length, rect.height);
            EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.6f, 1f));

            // Step indicators
            for (int i = 0; i < _stepTitles.Length; i++)
            {
                var stepRect = new Rect(rect.x + (rect.width * i / (_stepTitles.Length - 1)) - 5, rect.y - 2, 10, 24);
                var color = i <= _currentStep ? new Color(0.2f, 0.6f, 1f) : new Color(0.6f, 0.6f, 0.6f);
                EditorGUI.DrawRect(stepRect, color);

                var labelRect = new Rect(stepRect.x - 10, stepRect.y + 25, 30, 20);
                EditorGUI.LabelField(labelRect, (i + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawWelcomeStep()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField("🎉 MDI+ Dependency Injection Sistemine Hoş Geldiniz!", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Bu rehber size MDI+ sistemini projenizde kurmanızda yardımcı olacak.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("✨ Özellikler:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Kolay kullanım ve kurulum", EditorStyles.label);
            EditorGUILayout.LabelField("• SOLID prensiplerine uygun tasarım", EditorStyles.label);
            EditorGUILayout.LabelField("• Unity entegrasyonu", EditorStyles.label);
            EditorGUILayout.LabelField("• Performans izleme ve sağlık kontrolü", EditorStyles.label);
            EditorGUILayout.LabelField("• Görsel editör araçları", EditorStyles.label);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Başlamak için 'İleri' butonuna tıklayın.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawContainerSetupStep()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField("🔧 Container Kurulum Ayarları", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _createBootstrapper = EditorGUILayout.Toggle("Otomatik Bootstrapper Oluştur", _createBootstrapper);
            EditorGUILayout.HelpBox("Bootstrapper, uygulamanız başladığında otomatik olarak MDI+ container'ını kurar.", MessageType.Info);

            if (_createBootstrapper)
            {
                _bootstrapperName = EditorGUILayout.TextField("Bootstrapper Adı", _bootstrapperName);
            }

            EditorGUILayout.Space();

            _enableMonitoring = EditorGUILayout.Toggle("Service Monitoring Etkinleştir", _enableMonitoring);
            _enableHealthCheck = EditorGUILayout.Toggle("Health Check Etkinleştir", _enableHealthCheck);

            EditorGUILayout.EndVertical();
        }

        private void DrawServiceRegistrationStep()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField("📋 Service Kayıt Ayarları", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _createExampleServices = EditorGUILayout.Toggle("Örnek Service'ler Oluştur", _createExampleServices);

            if (_createExampleServices)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Oluşturulacak Örnek Service'ler:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• ILogger / ConsoleLogger", EditorStyles.label);
                EditorGUILayout.LabelField("• IGameService / GameService", EditorStyles.label);
                EditorGUILayout.LabelField("• IAudioService / AudioService", EditorStyles.label);
                EditorGUILayout.LabelField("• IUIService / UIService", EditorStyles.label);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInjectionSetupStep()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField("💉 Injection Kurulum Ayarları", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _gameManagerName = EditorGUILayout.TextField("Ana GameManager Adı", _gameManagerName);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("🎯 Injection Nasıl Çalışır:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. [Inject] attribute'unu field'larınıza ekleyin", EditorStyles.label);
            EditorGUILayout.LabelField("2. MDI.Inject(this) çağrısı yapın", EditorStyles.label);
            EditorGUILayout.LabelField("3. Service'leriniz otomatik olarak inject edilir", EditorStyles.label);

            EditorGUILayout.Space();

            EditorGUILayout.TextArea(
                "[Inject] private ILogger logger;\n" +
                "\n" +
                "void Awake() {\n" +
                "    MDI.Inject(this);\n" +
                "}", EditorStyles.textArea);

            EditorGUILayout.EndVertical();
        }

        private void DrawCompletionStep()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField("🎉 Kurulum Tamamlandı!", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("MDI+ sistemi başarıyla projenize kuruldu.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("MDI+ Monitor'ü Aç", _buttonStyle))
            {
                MDIMonitorWindow.ShowWindow();
            }

            if (GUILayout.Button("Dokümantasyonu Aç", _buttonStyle))
            {
                Application.OpenURL("https://github.com/cvtaydn/MDI/wiki");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNavigationButtons()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _currentStep > 0;
            if (GUILayout.Button("◀ Geri", _buttonStyle, GUILayout.Width(100)))
            {
                _currentStep--;
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (_currentStep < _stepTitles.Length - 1)
            {
                if (GUILayout.Button("İleri ▶", _buttonStyle, GUILayout.Width(100)))
                {
                    if (_currentStep == _stepTitles.Length - 2)
                    {
                        PerformSetup();
                    }
                    _currentStep++;
                }
            }
            else
            {
                if (GUILayout.Button("Kapat", _buttonStyle, GUILayout.Width(100)))
                {
                    Close();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void PerformSetup()
        {
            try
            {
                if (_createBootstrapper) CreateBootstrapper();
                if (_createExampleServices) CreateExampleServices();
                CreateGameManager();

                AssetDatabase.Refresh();
                Debug.Log("✅ MDI+ kurulumu başarıyla tamamlandı!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ MDI+ kurulumu sırasında hata oluştu: {ex.Message}");
                EditorUtility.DisplayDialog("Hata", $"Kurulum sırasında hata oluştu:\n{ex.Message}", "Tamam");
            }
        }

        private void CreateBootstrapper()
        {
            var path = $"Assets/Scripts/{_bootstrapperName}.cs";

            if (!Directory.Exists("Assets/Scripts"))
                Directory.CreateDirectory("Assets/Scripts");

            File.WriteAllText(path, GenerateBootstrapperCode());
            Debug.Log($"✅ Bootstrapper oluşturuldu: {path}");
        }

        private void CreateExampleServices()
        {
            var servicesPath = "Assets/Scripts/Services";

            if (!Directory.Exists(servicesPath))
                Directory.CreateDirectory(servicesPath);

            CreateServiceInterface("ILogger", servicesPath);
            CreateServiceInterface("IGameService", servicesPath);
            CreateServiceInterface("IAudioService", servicesPath);
            CreateServiceInterface("IUIService", servicesPath);

            CreateServiceImplementation("ConsoleLogger", "ILogger", servicesPath);
            CreateServiceImplementation("GameService", "IGameService", servicesPath);
            CreateServiceImplementation("AudioService", "IAudioService", servicesPath);
            CreateServiceImplementation("UIService", "IUIService", servicesPath);

            Debug.Log($"✅ Örnek service'ler oluşturuldu: {servicesPath}");
        }

        private void CreateServiceInterface(string interfaceName, string path)
        {
            var content = $@"namespace Services
{{
    public interface {interfaceName}
    {{
        void Initialize();
        void Cleanup();
    }}
}}";
            File.WriteAllText($"{path}/{interfaceName}.cs", content);
        }

        private void CreateServiceImplementation(string className, string interfaceName, string path)
        {
            var content = $@"using UnityEngine;
using Services;

namespace Services.Implementations
{{
    public class {className} : {interfaceName}
    {{
        public void Initialize()
        {{
            Debug.Log(""{className} initialized"");
        }}
        
        public void Cleanup()
        {{
            Debug.Log(""{className} cleaned up"");
        }}
    }}
}}";
            File.WriteAllText($"{path}/{className}.cs", content);
        }

        private void CreateGameManager()
        {
            var path = $"Assets/Scripts/{_gameManagerName}.cs";
            File.WriteAllText(path, GenerateGameManagerCode());
            Debug.Log($"✅ GameManager oluşturuldu: {path}");
        }

        private string GenerateBootstrapperCode()
        {
            return $@"using UnityEngine;
using MDI.Containers;
using MDI.Extensions;
using Services;
using Services.Implementations;

public class {_bootstrapperName} : MonoBehaviour
{{
    [SerializeField] private bool enableMonitoring = {_enableMonitoring.ToString().ToLower()};
    [SerializeField] private bool enableHealthCheck = {_enableHealthCheck.ToString().ToLower()};
    
    private MDIContainer container;
    
    private void Awake()
    {{
        SetupContainer();
        RegisterServices();
        MDIUnityHelper.GlobalContainer = container;
        Debug.Log(""✅ MDI+ Container başarıyla kuruldu!"");
    }}
    
    private void SetupContainer()
    {{
        container = new MDIContainer();
        if (enableHealthCheck) container.StartHealthCheck();
    }}

    private void RegisterServices()
    {{
        container
            .RegisterSingleton<ILogger, ConsoleLogger>()
            .RegisterSingleton<IGameService, GameService>()
            .RegisterSingleton<IAudioService, AudioService>()
            .RegisterSingleton<IUIService, UIService>();
    }}

    private void Update()
    {{
        if (enableMonitoring) container?.Update();
    }}

    private void OnDestroy()
    {{
        container?.Dispose();
    }}
}}";
        }

        private string GenerateGameManagerCode()
        {
            return $@"using UnityEngine;
using MDI.Core;
using MDI.Extensions;
using Services;

public class {_gameManagerName} : MonoBehaviour
{{
    [Inject] private ILogger logger;
    [Inject] private IGameService gameService;
    [Inject] private IAudioService audioService;
    [Inject] private IUIService uiService;

    private void Awake()
    {{
        MDI.Inject(this);
    }}

    private void Start()
    {{
        logger?.Initialize();
        gameService?.Initialize();
        audioService?.Initialize();
        uiService?.Initialize();
        Debug.Log(""🎮 Game Manager başlatıldı - Tüm service'ler hazır!"");
    }}

    private void OnDestroy()
    {{
        logger?.Cleanup();
        gameService?.Cleanup();
        audioService?.Cleanup();
        uiService?.Cleanup();
    }}
}}";
        }
    }
}
