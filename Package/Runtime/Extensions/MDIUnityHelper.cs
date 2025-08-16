using System;
using System.Reflection;
using UnityEngine;
using MDI.Containers;
using MDI.Core;

namespace MDI.Extensions
{
    /// <summary>
    /// Unity ile MDI+ entegrasyonu için helper sınıfı
    /// </summary>
    public static class MDIUnityHelper
    {
        private static IContainer _globalContainer;

        /// <summary>
        /// Global container instance'ı
        /// </summary>
        public static IContainer GlobalContainer
        {
            get
            {
                if (_globalContainer == null)
                {
                    _globalContainer = new MDIContainer();
                }
                return _globalContainer;
            }
            set => _globalContainer = value;
        }

        /// <summary>
        /// MonoBehaviour'e dependency injection yapar
        /// </summary>
        /// <param name="component">Injection yapılacak component</param>
        public static void Inject(MonoBehaviour component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            InjectObject(component, GlobalContainer);
        }

        /// <summary>
        /// Belirtilen container ile MonoBehaviour'e injection yapar
        /// </summary>
        /// <param name="component">Injection yapılacak component</param>
        /// <param name="container">Kullanılacak container</param>
        public static void Inject(MonoBehaviour component, IContainer container)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            InjectObject(component, container);
        }

        /// <summary>
        /// ScriptableObject'e dependency injection yapar
        /// </summary>
        /// <param name="scriptableObject">Injection yapılacak ScriptableObject</param>
        public static void Inject(ScriptableObject scriptableObject)
        {
            if (scriptableObject == null)
                throw new ArgumentNullException(nameof(scriptableObject));

            InjectObject(scriptableObject, GlobalContainer);
        }

        /// <summary>
        /// Belirtilen container ile ScriptableObject'e injection yapar
        /// </summary>
        /// <param name="scriptableObject">Injection yapılacak ScriptableObject</param>
        /// <param name="container">Kullanılacak container</param>
        public static void Inject(ScriptableObject scriptableObject, IContainer container)
        {
            if (scriptableObject == null)
                throw new ArgumentNullException(nameof(scriptableObject));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            InjectObject(scriptableObject, container);
        }

        /// <summary>
        /// GameObject'e ve tüm component'lerine injection yapar
        /// </summary>
        /// <param name="gameObject">Injection yapılacak GameObject</param>
        public static void InjectGameObject(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            InjectGameObject(gameObject, GlobalContainer);
        }

        /// <summary>
        /// Belirtilen container ile GameObject'e ve tüm component'lerine injection yapar
        /// </summary>
        /// <param name="gameObject">Injection yapılacak GameObject</param>
        /// <param name="container">Kullanılacak container</param>
        public static void InjectGameObject(GameObject gameObject, IContainer container)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            // Tüm component'leri al ve injection yap
            var components = gameObject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null)
                {
                    InjectObject(component, container);
                }
            }
        }

        /// <summary>
        /// Prefab instantiate ederken otomatik injection yapar
        /// </summary>
        /// <typeparam name="T">Component tipi</typeparam>
        /// <param name="prefab">Prefab</param>
        /// <param name="parent">Parent transform</param>
        /// <returns>Instantiate edilen component</returns>
        public static T InstantiateWithInjection<T>(T prefab, Transform parent = null) where T : MonoBehaviour
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            var instance = parent != null ? 
                UnityEngine.Object.Instantiate(prefab, parent) : 
                UnityEngine.Object.Instantiate(prefab);

            Inject(instance);
            return instance;
        }

        /// <summary>
        /// Prefab instantiate ederken belirtilen container ile injection yapar
        /// </summary>
        /// <typeparam name="T">Component tipi</typeparam>
        /// <param name="prefab">Prefab</param>
        /// <param name="container">Kullanılacak container</param>
        /// <param name="parent">Parent transform</param>
        /// <returns>Instantiate edilen component</returns>
        public static T InstantiateWithInjection<T>(T prefab, IContainer container, Transform parent = null) where T : MonoBehaviour
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            var instance = parent != null ? 
                UnityEngine.Object.Instantiate(prefab, parent) : 
                UnityEngine.Object.Instantiate(prefab);

            Inject(instance, container);
            return instance;
        }

        /// <summary>
        /// Object'e reflection kullanarak injection yapar
        /// </summary>
        /// <param name="obj">Injection yapılacak object</param>
        /// <param name="container">Kullanılacak container</param>
        public static void InjectObject(object obj, IContainer container)
        {
            var type = obj.GetType();

            // Private ve public field'ları al
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var injectAttribute = field.GetCustomAttribute<InjectAttribute>();
                if (injectAttribute != null)
                {
                    try
                    {
                        var serviceType = injectAttribute.ServiceType ?? field.FieldType;
                        var service = container.Resolve(serviceType);
                        field.SetValue(obj, service);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to inject {field.Name} in {type.Name}: {ex.Message}");
                    }
                }
            }

            // Private ve public property'leri al
            var properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var injectAttribute = property.GetCustomAttribute<InjectAttribute>();
                if (injectAttribute != null && property.CanWrite)
                {
                    try
                    {
                        var serviceType = injectAttribute.ServiceType ?? property.PropertyType;
                        var service = container.Resolve(serviceType);
                        property.SetValue(obj, service);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to inject {property.Name} in {type.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Global container'ı temizler
        /// </summary>
        public static void ClearGlobalContainer()
        {
            _globalContainer?.Clear();
        }

        /// <summary>
        /// Global container'ı dispose eder
        /// </summary>
        public static void DisposeGlobalContainer()
        {
            _globalContainer?.Dispose();
            _globalContainer = null;
        }
    }
}
