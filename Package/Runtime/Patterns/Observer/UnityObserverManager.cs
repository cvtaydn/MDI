using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MDI.Core;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Unity Observer Manager - Observer Pattern'ı Unity ortamına entegre eder
    /// </summary>
    public class UnityObserverManager : MonoBehaviour
    {
        #region Singleton

        private static UnityObserverManager _instance;
        private static readonly object _lock = new object();

        public static UnityObserverManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("[UnityObserverManager]");
                            _instance = go.AddComponent<UnityObserverManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        [Header("Observer Manager Settings")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private int maxObservableCount = 1000;
        [SerializeField] private int maxObserverCount = 10000;
        [SerializeField] private bool autoCleanup = true;
        [SerializeField] private float cleanupInterval = 60f;

        private readonly Dictionary<string, IObservable<object>> _observables = new Dictionary<string, IObservable<object>>();
        private readonly Dictionary<string, List<IObserver<object>>> _observers = new Dictionary<string, List<IObserver<object>>>();
        private readonly Dictionary<string, ReactiveProperty<object>> _reactiveProperties = new Dictionary<string, ReactiveProperty<object>>();
        private readonly Dictionary<string, Subject<object>> _subjects = new Dictionary<string, Subject<object>>();
        
        private readonly Dictionary<string, ObserverStatistics> _statistics = new Dictionary<string, ObserverStatistics>();
        private readonly List<string> _cleanupQueue = new List<string>();
        
        private float _lastCleanupTime;
        private int _totalNotificationCount;
        private int _totalErrorCount;
        
        private readonly SemaphoreSlim _asyncSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Events

        /// <summary>
        /// Observable oluşturulduğunda tetiklenir
        /// </summary>
        public event Action<string, Type> ObservableCreated;

        /// <summary>
        /// Observable silindiğinde tetiklenir
        /// </summary>
        public event Action<string, Type> ObservableDestroyed;

        /// <summary>
        /// Observer eklendiğinde tetiklenir
        /// </summary>
        public event Action<string, string> ObserverAdded;

        /// <summary>
        /// Observer kaldırıldığında tetiklenir
        /// </summary>
        public event Action<string, string> ObserverRemoved;

        /// <summary>
        /// Hata oluştuğunda tetiklenir
        /// </summary>
        public event Action<string, Exception> ErrorOccurred;

        #endregion

        #region Properties

        /// <summary>
        /// Kayıtlı observable sayısı
        /// </summary>
        public int ObservableCount => _observables.Count;

        /// <summary>
        /// Toplam observer sayısı
        /// </summary>
        public int TotalObserverCount => _observers.Values.Sum(list => list.Count);

        /// <summary>
        /// Toplam bildirim sayısı
        /// </summary>
        public int TotalNotificationCount => _totalNotificationCount;

        /// <summary>
        /// Toplam hata sayısı
        /// </summary>
        public int TotalErrorCount => _totalErrorCount;

        /// <summary>
        /// Logging etkin mi
        /// </summary>
        public bool EnableLogging
        {
            get => enableLogging;
            set => enableLogging = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _lastCleanupTime = Time.time;
            _cancellationTokenSource = new CancellationTokenSource();
            
            if (enableLogging)
            {
                Debug.Log("[UnityObserverManager] Started successfully");
            }
        }

        private void Update()
        {
            if (autoCleanup && Time.time - _lastCleanupTime >= cleanupInterval)
            {
                PerformCleanup();
                _lastCleanupTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            DisposeAllObservables();
            
            if (enableLogging)
            {
                Debug.Log("[UnityObserverManager] Destroyed");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseAllObservables();
            }
            else
            {
                ResumeAllObservables();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // İstatistikleri başlat
            _totalNotificationCount = 0;
            _totalErrorCount = 0;
            
            if (enableLogging)
            {
                Debug.Log("[UnityObserverManager] Initialized");
            }
        }

        #endregion

        #region Observable Management

        /// <summary>
        /// ReactiveProperty oluşturur
        /// </summary>
        /// <typeparam name="T">Property tipi</typeparam>
        /// <param name="name">Property adı</param>
        /// <param name="initialValue">Başlangıç değeri</param>
        /// <param name="throttleMs">Throttle süresi (ms)</param>
        /// <returns>ReactiveProperty</returns>
        public ReactiveProperty<T> CreateReactiveProperty<T>(string name, T initialValue = default(T), int throttleMs = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Property name cannot be null or empty", nameof(name));

            if (_reactiveProperties.ContainsKey(name))
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"[UnityObserverManager] ReactiveProperty '{name}' already exists");
                }
                return (ReactiveProperty<T>)(object)_reactiveProperties[name];
            }

            if (_reactiveProperties.Count >= maxObservableCount)
            {
                throw new InvalidOperationException($"Maximum observable count ({maxObservableCount}) reached");
            }

            var property = new ReactiveProperty<T>(initialValue, name, null, throttleMs);
            _reactiveProperties[name] = (ReactiveProperty<object>)(object)property;
            _statistics[name] = new ObserverStatistics { Name = name, CreatedTime = DateTime.UtcNow };

            ObservableCreated?.Invoke(name, typeof(T));

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Created ReactiveProperty '{name}' of type {typeof(T).Name}");
            }

            return property;
        }

        /// <summary>
        /// Subject oluşturur
        /// </summary>
        /// <typeparam name="T">Subject tipi</typeparam>
        /// <param name="name">Subject adı</param>
        /// <returns>Subject</returns>
        public Subject<T> CreateSubject<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Subject name cannot be null or empty", nameof(name));

            if (_subjects.ContainsKey(name))
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"[UnityObserverManager] Subject '{name}' already exists");
                }
                return (Subject<T>)(object)_subjects[name];
            }

            if (_subjects.Count >= maxObservableCount)
            {
                throw new InvalidOperationException($"Maximum observable count ({maxObservableCount}) reached");
            }

            var subject = new Subject<T>(default(T), name);
            _subjects[name] = (Subject<object>)(object)subject;
            _statistics[name] = new ObserverStatistics { Name = name, CreatedTime = DateTime.UtcNow };

            ObservableCreated?.Invoke(name, typeof(T));

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Created Subject '{name}' of type {typeof(T).Name}");
            }

            return subject;
        }

        /// <summary>
        /// BehaviorSubject oluşturur
        /// </summary>
        /// <typeparam name="T">Subject tipi</typeparam>
        /// <param name="name">Subject adı</param>
        /// <param name="initialValue">Başlangıç değeri</param>
        /// <returns>BehaviorSubject</returns>
        public BehaviorSubject<T> CreateBehaviorSubject<T>(string name, T initialValue)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Subject name cannot be null or empty", nameof(name));

            if (_subjects.ContainsKey(name))
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"[UnityObserverManager] BehaviorSubject '{name}' already exists");
                }
                return (BehaviorSubject<T>)(object)_subjects[name];
            }

            var subject = new BehaviorSubject<T>(initialValue, name);
            _subjects[name] = (Subject<object>)(object)subject;
            _statistics[name] = new ObserverStatistics { Name = name, CreatedTime = DateTime.UtcNow };

            ObservableCreated?.Invoke(name, typeof(T));

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Created BehaviorSubject '{name}' of type {typeof(T).Name}");
            }

            return subject;
        }

        /// <summary>
        /// ReplaySubject oluşturur
        /// </summary>
        /// <typeparam name="T">Subject tipi</typeparam>
        /// <param name="name">Subject adı</param>
        /// <param name="bufferSize">Buffer boyutu</param>
        /// <returns>ReplaySubject</returns>
        public ReplaySubject<T> CreateReplaySubject<T>(string name, int bufferSize = 10)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Subject name cannot be null or empty", nameof(name));

            if (_subjects.ContainsKey(name))
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"[UnityObserverManager] ReplaySubject '{name}' already exists");
                }
                return (ReplaySubject<T>)(object)_subjects[name];
            }

            var subject = new ReplaySubject<T>(bufferSize, default(T), name);
            _subjects[name] = (Subject<object>)(object)subject;
            _statistics[name] = new ObserverStatistics { Name = name, CreatedTime = DateTime.UtcNow };

            ObservableCreated?.Invoke(name, typeof(T));

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Created ReplaySubject '{name}' of type {typeof(T).Name} with buffer size {bufferSize}");
            }

            return subject;
        }

        /// <summary>
        /// Observable'ı alır
        /// </summary>
        /// <typeparam name="T">Observable tipi</typeparam>
        /// <param name="name">Observable adı</param>
        /// <returns>Observable veya null</returns>
        public T GetObservable<T>(string name) where T : class
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_reactiveProperties.TryGetValue(name, out var reactiveProperty))
            {
                return (T)(object)reactiveProperty;
            }

            if (_subjects.TryGetValue(name, out var subject))
            {
                return (T)(object)subject;
            }

            return null;
        }

        /// <summary>
        /// Observable'ı siler
        /// </summary>
        /// <param name="name">Observable adı</param>
        /// <returns>Silindi mi</returns>
        public bool RemoveObservable(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            bool removed = false;
            Type observableType = null;

            if (_reactiveProperties.TryGetValue(name, out var reactiveProperty))
            {
                observableType = reactiveProperty.GetType();
                reactiveProperty.Dispose();
                _reactiveProperties.Remove(name);
                removed = true;
            }
            else if (_subjects.TryGetValue(name, out var subject))
            {
                observableType = subject.GetType();
                subject.Dispose();
                _subjects.Remove(name);
                removed = true;
            }

            if (removed)
            {
                _statistics.Remove(name);
                ObservableDestroyed?.Invoke(name, observableType);

                if (enableLogging)
                {
                    Debug.Log($"[UnityObserverManager] Removed observable '{name}'");
                }
            }

            return removed;
        }

        /// <summary>
        /// Tüm observable'ları siler
        /// </summary>
        public void RemoveAllObservables()
        {
            var names = _reactiveProperties.Keys.Concat(_subjects.Keys).ToList();
            
            foreach (var name in names)
            {
                RemoveObservable(name);
            }

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Removed all observables ({names.Count})");
            }
        }

        #endregion

        #region Observer Management

        /// <summary>
        /// Global observer ekler
        /// </summary>
        /// <typeparam name="T">Observer tipi</typeparam>
        /// <param name="observableName">Observable adı</param>
        /// <param name="observer">Observer</param>
        /// <returns>Subscription ID</returns>
        public string AddObserver<T>(string observableName, IObserver<T> observer)
        {
            if (string.IsNullOrEmpty(observableName) || observer == null)
                return null;

            var observable = GetObservable<IObservable<T>>(observableName);
            if (observable == null)
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"[UnityObserverManager] Observable '{observableName}' not found");
                }
                return null;
            }

            var subscriptionId = observable.Subscribe(observer);
            
            if (_statistics.TryGetValue(observableName, out var stats))
            {
                stats.ObserverCount++;
            }

            ObserverAdded?.Invoke(observableName, observer.Id);

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Added observer '{observer.Id}' to observable '{observableName}'");
            }

            return subscriptionId;
        }

        /// <summary>
        /// Global observer kaldırır
        /// </summary>
        /// <typeparam name="T">Observer tipi</typeparam>
        /// <param name="observableName">Observable adı</param>
        /// <param name="observer">Observer</param>
        public void RemoveObserver<T>(string observableName, IObserver<T> observer)
        {
            if (string.IsNullOrEmpty(observableName) || observer == null)
                return;

            var observable = GetObservable<IObservable<T>>(observableName);
            if (observable == null)
                return;

            observable.Unsubscribe(observer);
            
            if (_statistics.TryGetValue(observableName, out var stats))
            {
                stats.ObserverCount = Math.Max(0, stats.ObserverCount - 1);
            }

            ObserverRemoved?.Invoke(observableName, observer.Id);

            if (enableLogging)
            {
                Debug.Log($"[UnityObserverManager] Removed observer '{observer.Id}' from observable '{observableName}'");
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Observable istatistiklerini alır
        /// </summary>
        /// <param name="name">Observable adı</param>
        /// <returns>İstatistikler</returns>
        public ObserverStatistics GetStatistics(string name)
        {
            return _statistics.TryGetValue(name, out var stats) ? stats : null;
        }

        /// <summary>
        /// Tüm istatistikleri alır
        /// </summary>
        /// <returns>Tüm istatistikler</returns>
        public Dictionary<string, ObserverStatistics> GetAllStatistics()
        {
            return new Dictionary<string, ObserverStatistics>(_statistics);
        }

        /// <summary>
        /// İstatistikleri temizler
        /// </summary>
        public void ClearStatistics()
        {
            _statistics.Clear();
            _totalNotificationCount = 0;
            _totalErrorCount = 0;

            if (enableLogging)
            {
                Debug.Log("[UnityObserverManager] Statistics cleared");
            }
        }

        #endregion

        #region Cleanup

        private void PerformCleanup()
        {
            var cleanedCount = 0;
            var observablesToRemove = new List<string>();

            // Inactive observable'ları temizle
            foreach (var kvp in _reactiveProperties)
            {
                if (!kvp.Value.IsActive)
                {
                    observablesToRemove.Add(kvp.Key);
                }
            }

            foreach (var kvp in _subjects)
            {
                if (!kvp.Value.IsActive)
                {
                    observablesToRemove.Add(kvp.Key);
                }
            }

            foreach (var name in observablesToRemove)
            {
                RemoveObservable(name);
                cleanedCount++;
            }

            if (enableLogging && cleanedCount > 0)
            {
                Debug.Log($"[UnityObserverManager] Cleanup completed: {cleanedCount} observables removed");
            }
        }

        private void PauseAllObservables()
        {
            foreach (var subject in _subjects.Values)
            {
                if (subject is ISubject<object> pausableSubject)
                {
                    pausableSubject.Pause();
                }
            }

            if (enableLogging)
            {
                Debug.Log("[UnityObserverManager] All observables paused");
            }
        }

        private void ResumeAllObservables()
        {
            foreach (var subject in _subjects.Values)
            {
                if (subject is ISubject<object> pausableSubject)
                {
                    pausableSubject.Resume();
                }
            }

            if (enableLogging)
            {
                Debug.Log("[UnityObserverManager] All observables resumed");
            }
        }

        private void DisposeAllObservables()
        {
            foreach (var property in _reactiveProperties.Values)
            {
                property?.Dispose();
            }
            _reactiveProperties.Clear();

            foreach (var subject in _subjects.Values)
            {
                subject?.Dispose();
            }
            _subjects.Clear();

            _statistics.Clear();
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// ReactiveProperty oluşturur (static)
        /// </summary>
        public static ReactiveProperty<T> CreatePropertyStatic<T>(string name, T initialValue = default(T), int throttleMs = 0)
        {
            return Instance.CreateReactiveProperty(name, initialValue, throttleMs);
        }

        /// <summary>
        /// Subject oluşturur (static)
        /// </summary>
        public static Subject<T> CreateSubjectStatic<T>(string name)
        {
            return Instance.CreateSubject<T>(name);
        }

        /// <summary>
        /// BehaviorSubject oluşturur (static)
        /// </summary>
        public static BehaviorSubject<T> CreateBehaviorSubjectStatic<T>(string name, T initialValue)
        {
            return Instance.CreateBehaviorSubject(name, initialValue);
        }

        /// <summary>
        /// ReplaySubject oluşturur (static)
        /// </summary>
        public static ReplaySubject<T> CreateReplaySubjectStatic<T>(string name, int bufferSize = 10)
        {
            return Instance.CreateReplaySubject<T>(name, bufferSize);
        }

        /// <summary>
        /// Observable alır (static)
        /// </summary>
        public static T GetObservableStatic<T>(string name) where T : class
        {
            return Instance.GetObservable<T>(name);
        }

        #endregion
    }

    /// <summary>
    /// Observer istatistikleri
    /// </summary>
    [Serializable]
    public class ObserverStatistics
    {
        public string Name;
        public DateTime CreatedTime;
        public int ObserverCount;
        public int NotificationCount;
        public int ErrorCount;
        public DateTime LastNotificationTime;
        public DateTime LastErrorTime;
        public Exception LastError;
    }
}
