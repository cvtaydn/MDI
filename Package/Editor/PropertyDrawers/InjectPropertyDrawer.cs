using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MDI.Core;
using MDI.Containers;
using MDI.Attributes;
using MDI.Extensions;

namespace MDI.Editor.PropertyDrawers
{
    /// <summary>
    /// [Inject] attribute için özel property drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(InjectAttribute))]
    public class InjectPropertyDrawer : PropertyDrawer
    {
        private const float ICON_SIZE = 16f;
        private const float BUTTON_WIDTH = 60f;
        private const float SPACING = 5f;
        private const float STATUS_WIDTH = 100f;
        
        private static GUIStyle _injectStyle;
        private static GUIStyle _statusStyle;
        private static GUIStyle _buttonStyle;
        private static bool _stylesInitialized = false;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeStyles();
            
            EditorGUI.BeginProperty(position, label, property);
            
            var injectAttribute = attribute as InjectAttribute;
            var fieldType = fieldInfo.FieldType;
            
            // Icon ve label
            var iconRect = new Rect(position.x, position.y, ICON_SIZE, position.height);
            var labelRect = new Rect(position.x + ICON_SIZE + SPACING, position.y, 
                position.width - ICON_SIZE - STATUS_WIDTH - BUTTON_WIDTH - SPACING * 3, position.height);
            var statusRect = new Rect(position.width - STATUS_WIDTH - BUTTON_WIDTH - SPACING, position.y, 
                STATUS_WIDTH, position.height);
            var buttonRect = new Rect(position.width - BUTTON_WIDTH, position.y, BUTTON_WIDTH, position.height);
            
            // Inject icon
            GUI.Label(iconRect, "💉", _injectStyle);
            
            // Property field
            EditorGUI.PropertyField(labelRect, property, new GUIContent(ObjectNames.NicifyVariableName(property.name)));
            
            // Status
            var container = GetCurrentContainer();
            var isRegistered = container?.IsRegistered(fieldType) ?? false;
            var canResolve = isRegistered && container != null;
            
            var statusText = GetStatusText(isRegistered, canResolve);
            var statusColor = GetStatusColor(isRegistered, canResolve);
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUI.LabelField(statusRect, statusText, _statusStyle);
            GUI.color = oldColor;
            
            // Auto-inject button
            GUI.enabled = canResolve && Application.isPlaying;
            if (GUI.Button(buttonRect, "Inject", _buttonStyle))
            {
                PerformInjection(property, fieldType, injectAttribute);
            }
            GUI.enabled = true;
            
            // Tooltip
            if (Event.current.type == EventType.Repaint)
            {
                var tooltipRect = new Rect(position.x, position.y, position.width, position.height);
                var tooltip = GetTooltipText(fieldType, isRegistered, canResolve, injectAttribute);
                GUI.tooltip = tooltip;
            }
            
            EditorGUI.EndProperty();
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _injectStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            _statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 9,
                padding = new RectOffset(2, 2, 1, 1)
            };
            
            _stylesInitialized = true;
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
        
        private void PerformInjection(SerializedProperty property, Type fieldType, InjectAttribute injectAttribute)
        {
            var container = GetCurrentContainer();
            if (container == null) return;
            
            try
            {
                var instance = container.Resolve(fieldType);
                if (instance != null)
                {
                    // MonoBehaviour veya ScriptableObject ise property'ye ata
                    if (instance is UnityEngine.Object unityObj)
                    {
                        property.objectReferenceValue = unityObj;
                        property.serializedObject.ApplyModifiedProperties();
                        
                        Debug.Log($"[MDI+] Successfully injected {fieldType.Name} into {property.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MDI+] Cannot inject non-Unity object {fieldType.Name} through Inspector");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MDI+] Failed to inject {fieldType.Name}: {ex.Message}");
            }
        }
        
        private string GetStatusText(bool isRegistered, bool canResolve)
        {
            if (!isRegistered)
                return "Not Registered";
            if (canResolve)
                return "Ready";
            return "Registered";
        }
        
        private Color GetStatusColor(bool isRegistered, bool canResolve)
        {
            if (!isRegistered)
                return new Color(0.8f, 0.3f, 0.3f); // Red
            if (canResolve)
                return new Color(0.3f, 0.8f, 0.3f); // Green
            return new Color(0.8f, 0.8f, 0.3f); // Yellow
        }
        
        private string GetTooltipText(Type fieldType, bool isRegistered, bool canResolve, InjectAttribute injectAttribute)
        {
            var tooltip = $"[Inject] {fieldType.Name}\n";
            
            if (injectAttribute.Optional)
                tooltip += "• Optional injection\n";
            
            if (!string.IsNullOrEmpty(injectAttribute.Id))
                tooltip += $"• Service ID: {injectAttribute.Id}\n";
            
            if (isRegistered)
            {
                tooltip += "• Service is registered\n";
                if (canResolve)
                    tooltip += "• Ready for injection";
                else
                    tooltip += "• Play mode required for injection";
            }
            else
            {
                tooltip += "• Service not registered\n";
                tooltip += "• Register service in container first";
            }
            
            return tooltip;
        }
    }
}