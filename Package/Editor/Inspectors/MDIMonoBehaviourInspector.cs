using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using MDI.Core;
using MDI.Containers;
using MDI.Attributes;
using MDI.Extensions;

namespace MDI.Editor.Inspectors
{
    /// <summary>
    /// MDI+ inject attribute'larına sahip MonoBehaviour'lar için özel inspector
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class MDIMonoBehaviourInspector : UnityEditor.Editor
    {
        private List<FieldInfo> _injectableFields;
        private bool _showMDISection = true;
        private bool _autoInjectOnPlay = true;
        
        private static GUIStyle _mdiHeaderStyle;
        private static GUIStyle _injectableFieldStyle;
        private static GUIStyle _statusStyle;
        private static bool _stylesInitialized = false;
        
        private void OnEnable()
        {
            _injectableFields = GetInjectableFields();
        }
        
        public override void OnInspectorGUI()
        {
            // Önce normal inspector'ı çiz
            DrawDefaultInspector();
            
            // MDI+ alanları varsa özel bölümü göster
            if (_injectableFields != null && _injectableFields.Any())
            {
                InitializeStyles();
                EditorGUILayout.Space(10);
                DrawMDISection();
            }
        }
        
        private List<FieldInfo> GetInjectableFields()
        {
            var targetType = target.GetType();
            var fields = new List<FieldInfo>();
            
            // Tüm field'ları kontrol et
            var allFields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in allFields)
            {
                // Inject attribute'u var mı?
                if (field.GetCustomAttribute<InjectAttribute>() != null)
                {
                    fields.Add(field);
                }
            }
            
            return fields;
        }
        
        private void DrawMDISection()
        {
            EditorGUILayout.BeginVertical(_mdiHeaderStyle);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            _showMDISection = EditorGUILayout.Foldout(_showMDISection, "💉 MDI+ Dependency Injection", true, EditorStyles.foldoutHeader);
            
            GUILayout.FlexibleSpace();
            
            // Inject All butonu
            GUI.enabled = Application.isPlaying && GetCurrentContainer() != null;
            if (GUILayout.Button("💉 Inject All", GUILayout.Width(80)))
            {
                InjectAllFields();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            if (_showMDISection)
            {
                EditorGUILayout.Space(5);
                
                // Container durumu
                DrawContainerStatus();
                
                EditorGUILayout.Space(5);
                
                // Auto-inject ayarı
                _autoInjectOnPlay = EditorGUILayout.Toggle("Auto-inject on Play", _autoInjectOnPlay);
                
                EditorGUILayout.Space(5);
                
                // Injectable field'lar
                DrawInjectableFields();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawContainerStatus()
        {
            var container = GetCurrentContainer();
            
            EditorGUILayout.BeginHorizontal();
            
            if (container != null)
            {
                var statusColor = container.IsHealthy() ? Color.green : Color.yellow;
                var statusText = container.IsHealthy() ? "✅ Container Ready" : "⚠️ Container Issues";
                
                var oldColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(statusText, _statusStyle);
                GUI.color = oldColor;
                
                var serviceCount = container.ServiceDescriptors?.Count ?? 0;
                GUILayout.Label($"({serviceCount} services)", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("❌ No Container Found", _statusStyle);
                GUI.color = Color.white;
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("🔍 Open Monitor", GUILayout.Width(100)))
            {
                EditorApplication.ExecuteMenuItem("MDI+/🔍 Service Monitor");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawInjectableFields()
        {
            var container = GetCurrentContainer();
            
            EditorGUILayout.LabelField("Injectable Fields:", EditorStyles.boldLabel);
            
            foreach (var field in _injectableFields)
            {
                DrawInjectableField(field, container);
            }
        }
        
        private void DrawInjectableField(FieldInfo field, MDIContainer container)
        {
            EditorGUILayout.BeginVertical(_injectableFieldStyle);
            
            var injectAttribute = field.GetCustomAttribute<InjectAttribute>();
            var fieldType = field.FieldType;
            var fieldValue = field.GetValue(target);
            
            EditorGUILayout.BeginHorizontal();
            
            // Field adı ve tipi
            GUILayout.Label($"💉 {ObjectNames.NicifyVariableName(field.Name)}", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label($"({fieldType.Name})", EditorStyles.miniLabel, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            // Durum göstergesi
            var isRegistered = container?.IsRegistered(fieldType) ?? false;
            var hasValue = fieldValue != null;
            var canInject = isRegistered && Application.isPlaying;
            
            var statusText = GetFieldStatusText(isRegistered, hasValue, canInject);
            var statusColor = GetFieldStatusColor(isRegistered, hasValue, canInject);
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusText, _statusStyle, GUILayout.Width(80));
            GUI.color = oldColor;
            
            // Inject butonu
            GUI.enabled = canInject;
            if (GUILayout.Button("Inject", GUILayout.Width(50)))
            {
                InjectField(field, container);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // Ek bilgiler
            if (injectAttribute.Optional)
            {
                EditorGUILayout.LabelField("  • Optional injection", EditorStyles.miniLabel);
            }
            
            if (!string.IsNullOrEmpty(injectAttribute.Id))
            {
                EditorGUILayout.LabelField($"  • Service ID: {injectAttribute.Id}", EditorStyles.miniLabel);
            }
            
            if (hasValue && fieldValue is UnityEngine.Object unityObj)
            {
                EditorGUILayout.ObjectField("  Current Value:", unityObj, fieldType, false);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        private void InjectField(FieldInfo field, MDIContainer container)
        {
            try
            {
                var instance = container.Resolve(field.FieldType);
                field.SetValue(target, instance);
                
                EditorUtility.SetDirty(target);
                Debug.Log($"[MDI+] Successfully injected {field.FieldType.Name} into {target.name}.{field.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MDI+] Failed to inject {field.FieldType.Name} into {target.name}.{field.Name}: {ex.Message}");
            }
        }
        
        private void InjectAllFields()
        {
            var container = GetCurrentContainer();
            if (container == null)
            {
                Debug.LogWarning("[MDI+] No container found for injection.");
                return;
            }
            
            var successCount = 0;
            var failCount = 0;
            
            foreach (var field in _injectableFields)
            {
                try
                {
                    if (container.IsRegistered(field.FieldType))
                    {
                        var instance = container.Resolve(field.FieldType);
                        field.SetValue(target, instance);
                        successCount++;
                    }
                    else
                    {
                        var injectAttribute = field.GetCustomAttribute<InjectAttribute>();
                        if (!injectAttribute.Optional)
                        {
                            Debug.LogWarning($"[MDI+] Required service {field.FieldType.Name} not registered for {target.name}.{field.Name}");
                            failCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MDI+] Failed to inject {field.FieldType.Name} into {target.name}.{field.Name}: {ex.Message}");
                    failCount++;
                }
            }
            
            EditorUtility.SetDirty(target);
            Debug.Log($"[MDI+] Injection completed for {target.name}: {successCount} successful, {failCount} failed.");
        }
        
        private MDIContainer GetCurrentContainer()
        {
            try
            {
                // Global container'ı kontrol et
                if (Application.isPlaying)
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
                
                // Scene'deki bootstrapper'ı ara
                var bootstrapper = UnityEngine.Object.FindObjectOfType<MonoBehaviour>()
                    ?.GetComponents<MonoBehaviour>()
                    ?.FirstOrDefault(mb => mb.GetType().Name.Contains("Bootstrap"));
                
                if (bootstrapper != null)
                {
                    var containerField = bootstrapper.GetType()
                        .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .FirstOrDefault(f => f.FieldType == typeof(MDIContainer));
                    
                    if (containerField != null)
                    {
                        return containerField.GetValue(bootstrapper) as MDIContainer;
                    }
                }
            }
            catch
            {
                // Sessizce başarısız ol
            }
            
            return null;
        }
        
        private string GetFieldStatusText(bool isRegistered, bool hasValue, bool canInject)
        {
            if (hasValue)
                return "✅ Injected";
            if (!isRegistered)
                return "❌ Not Reg.";
            if (canInject)
                return "📦 Ready";
            return "⏸️ Waiting";
        }
        
        private Color GetFieldStatusColor(bool isRegistered, bool hasValue, bool canInject)
        {
            if (hasValue)
                return new Color(0.2f, 0.8f, 0.2f); // Green
            if (!isRegistered)
                return new Color(0.8f, 0.2f, 0.2f); // Red
            if (canInject)
                return new Color(0.2f, 0.6f, 1f); // Blue
            return new Color(0.8f, 0.8f, 0.2f); // Yellow
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _mdiHeaderStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };
            
            _injectableFieldStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(5, 5, 1, 1)
            };
            
            _statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            _stylesInitialized = true;
        }
    }
}