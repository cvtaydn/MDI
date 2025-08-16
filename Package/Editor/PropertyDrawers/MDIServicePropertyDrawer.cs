using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MDI.Core;
using MDI.Containers;

namespace MDI.Editor.PropertyDrawers
{
    /// <summary>
    /// MDI+ Service için özel property drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(MDIServiceAttribute))]
    public class MDIServicePropertyDrawer : PropertyDrawer
    {
        private const float BUTTON_WIDTH = 80f;
        private const float SPACING = 5f;
        private const float LINE_HEIGHT = 18f;
        
        private static GUIStyle _serviceBoxStyle;
        private static GUIStyle _statusStyle;
        private static GUIStyle _buttonStyle;
        private static bool _stylesInitialized = false;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeStyles();
            
            EditorGUI.BeginProperty(position, label, property);
            
            var serviceAttribute = attribute as MDIServiceAttribute;
            var serviceType = serviceAttribute?.ServiceType ?? fieldInfo.FieldType;
            
            // Ana container
            var containerRect = new Rect(position.x, position.y, position.width, GetPropertyHeight(property, label));
            GUI.Box(containerRect, "", _serviceBoxStyle);
            
            var currentY = position.y + SPACING;
            
            // Service başlığı
            var titleRect = new Rect(position.x + SPACING, currentY, position.width - SPACING * 2, LINE_HEIGHT);
            EditorGUI.LabelField(titleRect, $"🔧 {serviceType.Name}", EditorStyles.boldLabel);
            currentY += LINE_HEIGHT + SPACING;
            
            // Service durumu
            var statusRect = new Rect(position.x + SPACING, currentY, position.width - BUTTON_WIDTH - SPACING * 3, LINE_HEIGHT);
            var buttonRect = new Rect(position.width - BUTTON_WIDTH + SPACING, currentY, BUTTON_WIDTH, LINE_HEIGHT);
            
            var container = GetCurrentContainer();
            var isRegistered = container?.IsRegistered(serviceType) ?? false;
            var instance = container?.TryResolve(serviceType);
            
            // Durum göstergesi
            var statusText = GetStatusText(isRegistered, instance != null);
            var statusColor = GetStatusColor(isRegistered, instance != null);
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUI.LabelField(statusRect, statusText, _statusStyle);
            GUI.color = oldColor;
            
            // Resolve butonu
            if (GUI.Button(buttonRect, "Resolve", _buttonStyle))
            {
                ResolveService(property, serviceType);
            }
            
            currentY += LINE_HEIGHT + SPACING;
            
            // Service detayları (eğer kayıtlıysa)
            if (isRegistered && container != null)
            {
                DrawServiceDetails(position, ref currentY, container, serviceType);
            }
            
            // Property field
            var propertyRect = new Rect(position.x + SPACING, currentY, position.width - SPACING * 2, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none);
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var baseHeight = EditorGUIUtility.singleLineHeight;
            var serviceAttribute = attribute as MDIServiceAttribute;
            var serviceType = serviceAttribute?.ServiceType ?? fieldInfo.FieldType;
            
            var container = GetCurrentContainer();
            var isRegistered = container?.IsRegistered(serviceType) ?? false;
            
            // Temel yükseklik: başlık + durum + property field + spacing
            var height = (LINE_HEIGHT * 3) + (SPACING * 4) + baseHeight;
            
            // Eğer service kayıtlıysa, detay alanı ekle
            if (isRegistered)
            {
                height += (LINE_HEIGHT * 2) + SPACING; // Lifetime + Dependencies
            }
            
            return height;
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _serviceBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 2, 2),
                normal = { background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.1f)) }
            };
            
            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                padding = new RectOffset(5, 5, 2, 2)
            };
            
            _stylesInitialized = true;
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
        
        private void DrawServiceDetails(Rect position, ref float currentY, MDIContainer container, Type serviceType)
        {
            // Lifetime bilgisi
            if (container.ServiceDescriptors.TryGetValue(serviceType, out var descriptor))
            {
                var lifetimeRect = new Rect(position.x + SPACING * 2, currentY, position.width - SPACING * 4, LINE_HEIGHT);
                var lifetimeColor = GetLifetimeColor(descriptor.Lifetime);
                
                var oldColor = GUI.color;
                GUI.color = lifetimeColor;
                EditorGUI.LabelField(lifetimeRect, $"⏱️ Lifetime: {descriptor.Lifetime}", EditorStyles.miniLabel);
                GUI.color = oldColor;
                
                currentY += LINE_HEIGHT;
            }
            
            // Dependencies
            if (container.DependencyGraph != null)
            {
                var dependencies = container.DependencyGraph.GetDependencies(serviceType);
                if (dependencies.Any())
                {
                    var depRect = new Rect(position.x + SPACING * 2, currentY, position.width - SPACING * 4, LINE_HEIGHT);
                    var depText = $"🔗 Dependencies: {dependencies.Count()}";
                    EditorGUI.LabelField(depRect, depText, EditorStyles.miniLabel);
                    currentY += LINE_HEIGHT;
                }
            }
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
        
        private void ResolveService(SerializedProperty property, Type serviceType)
        {
            var container = GetCurrentContainer();
            if (container == null)
            {
                EditorUtility.DisplayDialog("MDI+ Error", "No active MDI+ container found.", "OK");
                return;
            }
            
            try
            {
                var instance = container.Resolve(serviceType);
                if (instance != null)
                {
                    // MonoBehaviour ise, property'ye ata
                    if (instance is UnityEngine.Object unityObj)
                    {
                        property.objectReferenceValue = unityObj;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    
                    EditorUtility.DisplayDialog("MDI+ Success", $"Service {serviceType.Name} resolved successfully.", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MDI+ Error", $"Failed to resolve {serviceType.Name}:\n{ex.Message}", "OK");
            }
        }
        
        private string GetStatusText(bool isRegistered, bool hasInstance)
        {
            if (!isRegistered)
                return "❌ Not Registered";
            if (hasInstance)
                return "✅ Resolved";
            return "📦 Registered";
        }
        
        private Color GetStatusColor(bool isRegistered, bool hasInstance)
        {
            if (!isRegistered)
                return new Color(0.8f, 0.2f, 0.2f); // Red
            if (hasInstance)
                return new Color(0.2f, 0.8f, 0.2f); // Green
            return new Color(0.3f, 0.7f, 1f); // Blue
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
    }
    
    /// <summary>
    /// MDI+ Service attribute - Inspector'da özel görünüm için
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class MDIServiceAttribute : PropertyAttribute
    {
        public Type ServiceType { get; }
        public bool AutoResolve { get; }
        
        public MDIServiceAttribute(Type serviceType = null, bool autoResolve = false)
        {
            ServiceType = serviceType;
            AutoResolve = autoResolve;
        }
    }
}