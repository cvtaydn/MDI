using System;
using System.Reflection;
using UnityEngine;
using MDI.Containers;
using MDI.Attributes;
using MDI.Core;
using MDI.Extensions;

namespace MDI.Unity
{
    /// <summary>
    /// Unity entegrasyonu için helper sınıfı
    /// </summary>
    public static class MDIUnityHelper
    {
        /// <summary>
        /// MonoBehaviour'a dependency injection yapar
        /// </summary>
        /// <param name="target">Injection yapılacak MonoBehaviour</param>
        /// <param name="container">Container instance</param>
        public static void Inject(MonoBehaviour target, IContainer container)
        {
            if (target == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.Inject: Target veya Container null!");
                return;
            }
            
            InjectObject(target, container);
        }
        
        /// <summary>
        /// Herhangi bir object'e dependency injection yapar
        /// </summary>
        /// <param name="target">Injection yapılacak object</param>
        /// <param name="container">Container instance</param>
        public static void InjectObject(object target, IContainer container)
        {
            if (target == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.InjectObject: Target veya Container null!");
                return;
            }
            
            var targetType = target.GetType();
            var fields = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var properties = targetType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            // Field injection
            foreach (var field in fields)
            {
                var injectAttribute = field.GetCustomAttribute<InjectAttribute>();
                if (injectAttribute != null)
                {
                    try
                    {
                        var service = container.Resolve(field.FieldType);
                        field.SetValue(target, service);
                        
                        if (Application.isEditor)
                        {
                            Debug.Log($"✅ {targetType.Name}.{field.Name} injected with {field.FieldType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ {targetType.Name}.{field.Name} injection failed: {ex.Message}");
                    }
                }
            }
            
            // Property injection
            foreach (var property in properties)
            {
                var injectAttribute = property.GetCustomAttribute<InjectAttribute>();
                if (injectAttribute != null && property.CanWrite)
                {
                    try
                    {
                        var service = container.Resolve(property.PropertyType);
                        property.SetValue(target, service);
                        
                        if (Application.isEditor)
                        {
                            Debug.Log($"✅ {targetType.Name}.{property.Name} injected with {property.PropertyType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ {targetType.Name}.{property.Name} injection failed: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// GameObject'teki tüm MonoBehaviour'lara injection yapar
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="container">Container instance</param>
        public static void InjectGameObject(GameObject gameObject, IContainer container)
        {
            if (gameObject == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.InjectGameObject: GameObject veya Container null!");
                return;
            }
            
            var components = gameObject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null)
                {
                    Inject(component, container);
                }
            }
        }
        
        /// <summary>
        /// GameObject ve tüm child'larına injection yapar
        /// </summary>
        /// <param name="gameObject">Root GameObject</param>
        /// <param name="container">Container instance</param>
        /// <param name="includeInactive">Inactive object'leri dahil et</param>
        public static void InjectGameObjectRecursive(GameObject gameObject, IContainer container, bool includeInactive = false)
        {
            if (gameObject == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.InjectGameObjectRecursive: GameObject veya Container null!");
                return;
            }
            
            // Root object'e inject
            InjectGameObject(gameObject, container);
            
            // Child'lara inject
            var components = gameObject.GetComponentsInChildren<MonoBehaviour>(includeInactive);
            foreach (var component in components)
            {
                if (component != null && component.gameObject != gameObject)
                {
                    Inject(component, container);
                }
            }
        }
        
        /// <summary>
        /// Scene'deki tüm MonoBehaviour'lara injection yapar
        /// </summary>
        /// <param name="container">Container instance</param>
        public static void InjectScene(IContainer container)
        {
            if (container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.InjectScene: Container null!");
                return;
            }
            
            var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
            int injectedCount = 0;
            
            foreach (var monoBehaviour in allMonoBehaviours)
            {
                if (monoBehaviour != null && HasInjectableFields(monoBehaviour))
                {
                    Inject(monoBehaviour, container);
                    injectedCount++;
                }
            }
            
            Debug.Log($"🎯 Scene injection tamamlandı. {injectedCount} MonoBehaviour'a injection yapıldı.");
        }
        
        /// <summary>
        /// Object'in injectable field'ları olup olmadığını kontrol eder
        /// </summary>
        /// <param name="target">Kontrol edilecek object</param>
        /// <returns>Injectable field varsa true</returns>
        public static bool HasInjectableFields(object target)
        {
            if (target == null) return false;
            
            var targetType = target.GetType();
            var fields = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var properties = targetType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<InjectAttribute>() != null)
                    return true;
            }
            
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<InjectAttribute>() != null)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Prefab instantiate edildiğinde otomatik injection yapar
        /// </summary>
        /// <param name="prefab">Instantiate edilecek prefab</param>
        /// <param name="container">Container instance</param>
        /// <returns>Instantiate edilmiş ve inject edilmiş GameObject</returns>
        public static GameObject InstantiateWithInjection(GameObject prefab, IContainer container)
        {
            if (prefab == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.InstantiateWithInjection: Prefab veya Container null!");
                return null;
            }
            
            var instance = UnityEngine.Object.Instantiate(prefab);
            InjectGameObjectRecursive(instance, container, true);
            
            Debug.Log($"🎯 {prefab.name} instantiate edildi ve injection yapıldı.");
            return instance;
        }
        
        /// <summary>
        /// Prefab instantiate edildiğinde otomatik injection yapar (position ve rotation ile)
        /// </summary>
        /// <param name="prefab">Instantiate edilecek prefab</param>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="container">Container instance</param>
        /// <returns>Instantiate edilmiş ve inject edilmiş GameObject</returns>
        public static GameObject InstantiateWithInjection(GameObject prefab, Vector3 position, Quaternion rotation, IContainer container)
        {
            if (prefab == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.InstantiateWithInjection: Prefab veya Container null!");
                return null;
            }
            
            var instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
            InjectGameObjectRecursive(instance, container, true);
            
            Debug.Log($"🎯 {prefab.name} instantiate edildi ve injection yapıldı.");
            return instance;
        }
        
        /// <summary>
        /// Component eklendiğinde otomatik injection yapar
        /// </summary>
        /// <typeparam name="T">Component tipi</typeparam>
        /// <param name="gameObject">Component eklenecek GameObject</param>
        /// <param name="container">Container instance</param>
        /// <returns>Eklenen ve inject edilmiş component</returns>
        public static T AddComponentWithInjection<T>(GameObject gameObject, IContainer container) where T : MonoBehaviour
        {
            if (gameObject == null || container == null)
            {
                Debug.LogWarning("⚠️ MDIUnityHelper.AddComponentWithInjection: GameObject veya Container null!");
                return null;
            }
            
            var component = gameObject.AddComponent<T>();
            Inject(component, container);
            
            Debug.Log($"🎯 {typeof(T).Name} component eklendi ve injection yapıldı.");
            return component;
        }
    }
}