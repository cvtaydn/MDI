using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Temel Subject implementasyonu
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public class Subject<T> : ISubject<T>
    {
        private readonly Dictionary<string, IObserver<T>> _observers;
        private readonly Dictionary<string, ActionObserver<T>> _actionObservers;
        private readonly object _lock = new object();
        
        private T _value;
        private SubjectState _state;
        private Exception _error;
        private int _nextSubscriptionId = 1;
        private int _notificationCount = 0;

        #region Properties

        public string Id { get; }
        public string Name { get; set; }
        public T Value 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _value; 
                } 
            } 
        }
        public bool IsActive => _state == SubjectState.Active;
        public bool IsCompleted => _state == SubjectState.Completed || _state == SubjectState.Error || _state == SubjectState.Disposed;
        public Exception LastError 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _error; 
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
        public SubjectState State 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _state; 
                } 
            } 
        }
        public DateTime LastUpdateTime { get; private set; }
        public int NotificationCount 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _notificationCount; 
                } 
            } 
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialValue">Başlangıç değeri</param>
        /// <param name="name">Subject adı</param>
        public Subject(T initialValue = default(T), string name = null)
        {
            Id = Guid.NewGuid().ToString();
            Name = name ?? $"Subject_{typeof(T).Name}_{Id[..8]}";
            _value = initialValue;
            _state = SubjectState.Active;
            _observers = new Dictionary<string, IObserver<T>>();
            _actionObservers = new Dictionary<string, ActionObserver<T>>();
            LastUpdateTime = DateTime.UtcNow;
        }

        #endregion

        #region IObservable Implementation

        public string Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            lock (_lock)
            {
                if (_state == SubjectState.Disposed)
                    throw new ObjectDisposedException(nameof(Subject<T>));

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

        public virtual string Subscribe(Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null, int priority = 0)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            
            lock (_lock)
            {
                if (_state == SubjectState.Disposed)
                    throw new ObjectDisposedException(nameof(Subject<T>));

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
                if (_state != SubjectState.Active) return;
                
                _state = SubjectState.Completed;
                LastUpdateTime = DateTime.UtcNow;
            }
            
            NotifyCompleted();
        }

        /// <summary>
        /// Hata durumunu raporlar ve tüm abonelere bildirir.
        /// </summary>
        /// <param name="error">Oluşan hata</param>
        public void Error(Exception error)
        {
            lock (_lock)
            {
                if (_state != SubjectState.Active) return;
                
                _error = error;
                _state = SubjectState.Error;
                LastUpdateTime = DateTime.UtcNow;
            }
            
            NotifyError(error);
        }

        public virtual void Dispose()
        {
            lock (_lock)
            {
                if (_state == SubjectState.Disposed) return;
                
                _state = SubjectState.Disposed;
                _observers.Clear();
                _actionObservers.Clear();
                LastUpdateTime = DateTime.UtcNow;
            }
        }

        #endregion

        #region IObserver Implementation

        public int Priority => 0;

        public void OnNext(T value)
        {
            SetValue(value);
        }

        public void OnError(Exception error)
        {
            Error(error);
        }

        public void OnCompleted()
        {
            Complete();
        }

        #endregion

        #region IMutableObservable Implementation

        public virtual void SetValue(T value)
        {
            lock (_lock)
            {
                if (_state != SubjectState.Active) return;
                
                _value = value;
                _notificationCount++;
                LastUpdateTime = DateTime.UtcNow;
            }
            
            NotifyObservers();
        }

        public virtual void SetValueSilently(T value)
        {
            lock (_lock)
            {
                if (_state != SubjectState.Active) return;
                
                _value = value;
                LastUpdateTime = DateTime.UtcNow;
            }
        }

        public void NotifyObservers()
        {
            if (_state != SubjectState.Active) return;
            
            T currentValue;
            List<IObserver<T>> observers;
            List<ActionObserver<T>> actionObservers;
            
            lock (_lock)
            {
                currentValue = _value;
                observers = _observers.Values.Where(o => o.IsActive).OrderByDescending(o => o.Priority).ToList();
                actionObservers = _actionObservers.Values.Where(o => o.IsActive).OrderByDescending(o => o.Priority).ToList();
            }
            
            // Notify observers
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
            
            // Notify action observers
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
            if (observer == null || !observer.IsActive || _state != SubjectState.Active) return;
            
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

        #region ISubject Implementation

        public virtual void Reset()
        {
            lock (_lock)
            {
                _value = default(T);
                _error = null;
                _state = SubjectState.Active;
                _notificationCount = 0;
                LastUpdateTime = DateTime.UtcNow;
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (_state == SubjectState.Active)
                {
                    _state = SubjectState.Paused;
                    LastUpdateTime = DateTime.UtcNow;
                }
            }
        }

        public void Resume()
        {
            lock (_lock)
            {
                if (_state == SubjectState.Paused)
                {
                    _state = SubjectState.Active;
                    LastUpdateTime = DateTime.UtcNow;
                }
            }
        }

        public virtual ISubject<T> Clone()
        {
            lock (_lock)
            {
                return new Subject<T>(_value, $"{Name}_Clone");
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
                observers = _observers.Values.Where(o => o.IsActive).ToList();
                actionObservers = _actionObservers.Values.Where(o => o.IsActive).ToList();
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
                observers = _observers.Values.Where(o => o.IsActive).ToList();
                actionObservers = _actionObservers.Values.Where(o => o.IsActive).ToList();
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
    }

    /// <summary>
    /// Action-based observer wrapper
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    internal class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public string Id { get; }
        public int Priority { get; }
        public bool IsActive { get; private set; } = true;

        public ActionObserver(string id, Action<T> onNext, Action<Exception> onError, Action onCompleted, int priority)
        {
            Id = id;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
            Priority = priority;
        }

        public void OnNext(T value)
        {
            if (IsActive)
            {
                _onNext?.Invoke(value);
            }
        }

        public void OnError(Exception error)
        {
            if (IsActive)
            {
                _onError?.Invoke(error);
            }
        }

        public void OnCompleted()
        {
            if (IsActive)
            {
                _onCompleted?.Invoke();
                IsActive = false;
            }
        }
    }
    
    /// <summary>
    /// Async action-based observer implementasyonu
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    internal class AsyncObserver<T> : IAsyncObserver<T>
    {
        private readonly Func<T, Task> _onNext;
        private readonly Func<Exception, Task> _onError;
        private readonly Func<Task> _onCompleted;

        public string Id { get; }
        public int Priority { get; }
        public bool IsActive { get; private set; } = true;

        public AsyncObserver(string id, Func<T, Task> onNext, Func<Exception, Task> onError, Func<Task> onCompleted, int priority)
        {
            Id = id;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
            Priority = priority;
        }

        public async Task OnNextAsync(T value, CancellationToken cancellationToken = default)
        {
            if (IsActive && _onNext != null)
            {
                await _onNext(value);
            }
        }

        public async Task OnErrorAsync(Exception error, CancellationToken cancellationToken = default)
        {
            if (IsActive && _onError != null)
            {
                await _onError(error);
            }
        }

        public async Task OnCompletedAsync(CancellationToken cancellationToken = default)
        {
            if (IsActive && _onCompleted != null)
            {
                await _onCompleted();
                IsActive = false;
            }
        }
    }
}
