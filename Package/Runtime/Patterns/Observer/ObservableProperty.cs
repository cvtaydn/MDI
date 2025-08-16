using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Observable property implementasyonu - property değişikliklerini gözlemlenebilir hale getirir
    /// </summary>
    /// <typeparam name="T">Property tipi</typeparam>
    [Serializable]
    public class ObservableProperty<T> : IObservable<T>, IMutableObservable<T>, IDisposable
    {
        [SerializeField] private T _value;
        private readonly Dictionary<string, IObserver<T>> _observers;
        private readonly Dictionary<string, ActionObserver<T>> _actionObservers;
        private readonly object _lock = new object();
        private readonly IEqualityComparer<T> _comparer;
        
        private bool _isActive = true;
        private bool _isCompleted = false;
        private Exception _error;
        private int _nextSubscriptionId = 1;
        private DateTime _lastUpdateTime;
        private int _changeCount = 0;

        #region Events

        /// <summary>
        /// Değer değiştiğinde tetiklenir
        /// </summary>
        public event Action<T> ValueChanged;

        /// <summary>
        /// Değer değişmeden önce tetiklenir
        /// </summary>
        public event Action<T, T> ValueChanging;

        #endregion

        #region Properties

        public string Id { get; }
        public string Name { get; set; }
        
        /// <summary>
        /// Mevcut değer
        /// </summary>
        public T Value 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _value; 
                } 
            } 
            set 
            { 
                SetValue(value); 
            } 
        }

        public bool IsActive 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _isActive; 
                } 
            } 
        }

        public bool IsCompleted 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _isCompleted; 
                } 
            } 
        }



        public int ObserverCount 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _observers.Count + _actionObservers.Count; 
                } 
            } 
        }

        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        public DateTime LastUpdateTime 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _lastUpdateTime; 
                } 
            } 
        }

        /// <summary>
        /// Toplam değişiklik sayısı
        /// </summary>
        public int ChangeCount 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _changeCount; 
                } 
            } 
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialValue">Başlangıç değeri</param>
        /// <param name="name">Property adı</param>
        /// <param name="comparer">Değer karşılaştırıcı</param>
        public ObservableProperty(T initialValue = default(T), string name = null, IEqualityComparer<T> comparer = null)
        {
            Id = Guid.NewGuid().ToString();
            Name = name ?? $"ObservableProperty_{typeof(T).Name}_{Id[..8]}";
            _value = initialValue;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _observers = new Dictionary<string, IObserver<T>>();
            _actionObservers = new Dictionary<string, ActionObserver<T>>();
            _lastUpdateTime = DateTime.UtcNow;
        }

        #endregion

        #region IObservable Implementation

        public string Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            lock (_lock)
            {
                if (!_isActive)
                    throw new InvalidOperationException("ObservableProperty is not active");

                var subscriptionId = $"{Id}_{_nextSubscriptionId++}";
                _observers[subscriptionId] = observer;
                
                return subscriptionId;
            }
        }

        public void Unsubscribe(IObserver<T> observer)
        {
            if (observer == null) return;
            
            lock (_lock)
            {
                var kvp = _observers.FirstOrDefault(x => x.Value.Equals(observer));
                if (!kvp.Equals(default(KeyValuePair<string, IObserver<T>>)))
                {
                    _observers.Remove(kvp.Key);
                }
            }
        }

        public void Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId)) return;
            
            lock (_lock)
            {
                _observers.Remove(subscriptionId);
                _actionObservers.Remove(subscriptionId);
            }
        }

        public string Subscribe(Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null, int priority = 0)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            
            lock (_lock)
            {
                if (!_isActive)
                    throw new InvalidOperationException("ObservableProperty is not active");

                var subscriptionId = $"{Id}_{_nextSubscriptionId++}";
                var actionObserver = new ActionObserver<T>(subscriptionId, onNext, onError, onCompleted, priority);
                _actionObservers[subscriptionId] = actionObserver;
                
                return subscriptionId;
            }
        }

        public void UnsubscribeAll()
        {
            lock (_lock)
            {
                _observers.Clear();
                _actionObservers.Clear();
            }
        }

        public void Complete()
        {
            lock (_lock)
            {
                if (!_isActive || _isCompleted) return;
                
                _isCompleted = true;
                _lastUpdateTime = DateTime.UtcNow;
            }
            
            NotifyCompleted();
        }

        public void Error(Exception error)
        {
            lock (_lock)
            {
                if (!_isActive || _isCompleted) return;
                
                _error = error;
                _isCompleted = true;
                _lastUpdateTime = DateTime.UtcNow;
            }
            
            NotifyError(error);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _isActive = false;
                _isCompleted = true;
                _observers.Clear();
                _actionObservers.Clear();
                _lastUpdateTime = DateTime.UtcNow;
            }
            
            ValueChanged = null;
            ValueChanging = null;
        }

        #endregion

        #region IMutableObservable Implementation

        public void SetValue(T value)
        {
            T oldValue;
            bool shouldNotify;
            
            lock (_lock)
            {
                if (!_isActive || _isCompleted) return;
                
                oldValue = _value;
                shouldNotify = !_comparer.Equals(oldValue, value);
                
                if (shouldNotify)
                {
                    // ValueChanging event'ini tetikle
                    try
                    {
                        ValueChanging?.Invoke(oldValue, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in ValueChanging event: {ex.Message}");
                    }
                    
                    _value = value;
                    _changeCount++;
                    _lastUpdateTime = DateTime.UtcNow;
                }
            }
            
            if (shouldNotify)
            {
                // ValueChanged event'ini tetikle
                try
                {
                    ValueChanged?.Invoke(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in ValueChanged event: {ex.Message}");
                }
                
                NotifyObservers();
            }
        }

        public void SetValueSilently(T value)
        {
            lock (_lock)
            {
                if (!_isActive || _isCompleted) return;
                
                _value = value;
                _lastUpdateTime = DateTime.UtcNow;
            }
        }

        public void NotifyObservers()
        {
            if (!_isActive || _isCompleted) return;
            
            T currentValue;
            List<IObserver<T>> observers;
            List<ActionObserver<T>> actionObservers;
            
            lock (_lock)
            {
                currentValue = _value;
                observers = new List<IObserver<T>>(_observers.Values.Where(o => o.IsActive));
                actionObservers = new List<ActionObserver<T>>(_actionObservers.Values.Where(o => o.IsActive));
            }
            
            // Priority'ye göre sırala
            observers = observers.OrderByDescending(o => o.Priority).ToList();
            actionObservers = actionObservers.OrderByDescending(o => o.Priority).ToList();
            
            // Observer'ları bilgilendir
            foreach (var observer in observers)
            {
                try
                {
                    observer.OnNext(currentValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying observer {observer.GetType().Name}: {ex.Message}");
                }
            }
            
            // Action observer'ları bilgilendir
            foreach (var actionObserver in actionObservers)
            {
                try
                {
                    actionObserver.OnNext(currentValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying action observer: {ex.Message}");
                }
            }
        }

        public void NotifyObserver(IObserver<T> observer)
        {
            if (observer == null || !observer.IsActive || !_isActive || _isCompleted) return;
            
            T currentValue;
            lock (_lock)
            {
                currentValue = _value;
            }
            
            try
            {
                observer.OnNext(currentValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error notifying specific observer {observer.GetType().Name}: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Değeri resetler
        /// </summary>
        public void Reset()
        {
            SetValue(default(T));
        }

        /// <summary>
        /// Property'nin bir kopyasını oluşturur
        /// </summary>
        /// <returns>Kopya property</returns>
        public ObservableProperty<T> Clone()
        {
            lock (_lock)
            {
                return new ObservableProperty<T>(_value, $"{Name}_Clone", _comparer);
            }
        }

        /// <summary>
        /// Değeri koşullu olarak değiştirir
        /// </summary>
        /// <param name="condition">Koşul</param>
        /// <param name="value">Yeni değer</param>
        /// <returns>Değer değişti mi</returns>
        public bool SetValueIf(Func<T, bool> condition, T value)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            
            lock (_lock)
            {
                if (!_isActive || _isCompleted) return false;
                
                if (condition(_value))
                {
                    SetValue(value);
                    return true;
                }
                
                return false;
            }
        }

        /// <summary>
        /// Değeri transform eder
        /// </summary>
        /// <param name="transform">Transform fonksiyonu</param>
        public void Transform(Func<T, T> transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            
            lock (_lock)
            {
                if (!_isActive || _isCompleted) return;
                
                var newValue = transform(_value);
                SetValue(newValue);
            }
        }

        #endregion

        #region Private Methods

        private void NotifyCompleted()
        {
            List<IObserver<T>> observers;
            List<ActionObserver<T>> actionObservers;
            
            lock (_lock)
            {
                observers = new List<IObserver<T>>(_observers.Values.Where(o => o.IsActive));
                actionObservers = new List<ActionObserver<T>>(_actionObservers.Values.Where(o => o.IsActive));
            }
            
            foreach (var observer in observers)
            {
                try
                {
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying observer completion {observer.GetType().Name}: {ex.Message}");
                }
            }
            
            foreach (var actionObserver in actionObservers)
            {
                try
                {
                    actionObserver.OnCompleted();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying action observer completion: {ex.Message}");
                }
            }
        }

        private void NotifyError(Exception error)
        {
            List<IObserver<T>> observers;
            List<ActionObserver<T>> actionObservers;
            
            lock (_lock)
            {
                observers = new List<IObserver<T>>(_observers.Values.Where(o => o.IsActive));
                actionObservers = new List<ActionObserver<T>>(_actionObservers.Values.Where(o => o.IsActive));
            }
            
            foreach (var observer in observers)
            {
                try
                {
                    observer.OnError(error);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying observer error {observer.GetType().Name}: {ex.Message}");
                }
            }
            
            foreach (var actionObserver in actionObservers)
            {
                try
                {
                    actionObserver.OnError(error);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying action observer error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Operators

        /// <summary>
        /// Implicit conversion to T
        /// </summary>
        public static implicit operator T(ObservableProperty<T> property)
        {
            return property.Value;
        }

        /// <summary>
        /// Implicit conversion from T
        /// </summary>
        public static implicit operator ObservableProperty<T>(T value)
        {
            return new ObservableProperty<T>(value);
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            lock (_lock)
            {
                return $"ObservableProperty<{typeof(T).Name}> [Name: {Name}, Value: {_value}, Observers: {ObserverCount}, Changes: {_changeCount}]";
            }
        }

        #endregion
    }
}