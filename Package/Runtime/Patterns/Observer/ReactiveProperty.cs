using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Reactive property implementasyonu - gelişmiş reactive programming özellikleri
    /// </summary>
    /// <typeparam name="T">Property tipi</typeparam>
    [Serializable]
    public class ReactiveProperty<T> : ObservableProperty<T>, IAsyncObservable<T>, IFilterableObservable<T>, ITransformableObservable<T, T>
    {
        private readonly List<Func<T, bool>> _filters;
        private readonly Dictionary<string, Func<T, bool>> _namedFilters;
        private readonly object _filterLock = new object();
        
        private bool _isFilterEnabled = true;
        private TimeSpan _throttleInterval = TimeSpan.Zero;
        private DateTime _lastNotificationTime = DateTime.MinValue;
        private bool _isThrottleEnabled = false;
        
        private readonly SemaphoreSlim _asyncSemaphore = new SemaphoreSlim(1, 1);
        private readonly List<IAsyncObserver<T>> _asyncObservers = new List<IAsyncObserver<T>>();
        private readonly Dictionary<string, Func<T, Task<bool>>> _asyncFilters = new Dictionary<string, Func<T, Task<bool>>>();

        #region Events

        /// <summary>
        /// Filtre uygulandığında tetiklenir
        /// </summary>
        public event Action<T, bool> FilterApplied;

        /// <summary>
        /// Throttle uygulandığında tetiklenir
        /// </summary>
        public event Action<T> ThrottleApplied;

        #endregion

        #region Properties

        /// <summary>
        /// Filtre etkin mi
        /// </summary>
        public bool IsFilterEnabled
        {
            get
            {
                lock (_filterLock)
                {
                    return _isFilterEnabled;
                }
            }
            set
            {
                lock (_filterLock)
                {
                    _isFilterEnabled = value;
                }
            }
        }

        /// <summary>
        /// Throttle etkin mi
        /// </summary>
        public bool IsThrottleEnabled
        {
            get { return _isThrottleEnabled; }
            set { _isThrottleEnabled = value; }
        }

        /// <summary>
        /// Throttle aralığı
        /// </summary>
        public TimeSpan ThrottleInterval
        {
            get { return _throttleInterval; }
            set 
            { 
                _throttleInterval = value;
                _isThrottleEnabled = value > TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Son bildirim zamanı
        /// </summary>
        public DateTime LastNotificationTime
        {
            get { return _lastNotificationTime; }
        }

        /// <summary>
        /// Aktif filtre sayısı
        /// </summary>
        public int FilterCount
        {
            get
            {
                lock (_filterLock)
                {
                    return _filters.Count + _namedFilters.Count;
                }
            }
        }

        /// <summary>
        /// Async observer sayısı
        /// </summary>
        public int AsyncObserverCount
        {
            get
            {
                lock (_asyncObservers)
                {
                    return _asyncObservers.Count;
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
        /// <param name="throttleMs">Throttle süresi (ms)</param>
        public ReactiveProperty(T initialValue = default(T), string name = null, IEqualityComparer<T> comparer = null, int throttleMs = 0)
            : base(initialValue, name ?? $"ReactiveProperty_{typeof(T).Name}", comparer)
        {
            _filters = new List<Func<T, bool>>();
            _namedFilters = new Dictionary<string, Func<T, bool>>();
            
            if (throttleMs > 0)
            {
                ThrottleInterval = TimeSpan.FromMilliseconds(throttleMs);
            }
        }

        #endregion

        #region IAsyncObservable Implementation

        public async Task<string> SubscribeAsync(IAsyncObserver<T> observer, CancellationToken cancellationToken = default)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            await _asyncSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!IsActive)
                    throw new InvalidOperationException("ReactiveProperty is not active");

                lock (_asyncObservers)
                {
                    _asyncObservers.Add(observer);
                }
                
                return observer.Id;
            }
            finally
            {
                _asyncSemaphore.Release();
            }
        }

        public async Task UnsubscribeAsync(IAsyncObserver<T> observer, CancellationToken cancellationToken = default)
        {
            if (observer == null) return;
            
            await _asyncSemaphore.WaitAsync(cancellationToken);
            try
            {
                lock (_asyncObservers)
                {
                    _asyncObservers.Remove(observer);
                }
            }
            finally
            {
                _asyncSemaphore.Release();
            }
        }

        public async Task NotifyObserversAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive || IsCompleted) return;
            
            T currentValue;
            List<IAsyncObserver<T>> asyncObservers;
            
            lock (_asyncObservers)
            {
                currentValue = Value;
                asyncObservers = new List<IAsyncObserver<T>>(_asyncObservers.Where(o => o.IsActive));
            }
            
            // Async observer'ları bilgilendir
            var tasks = asyncObservers.Select(async observer =>
            {
                try
                {
                    await observer.OnNextAsync(currentValue, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying async observer {observer.GetType().Name}: {ex.Message}");
                }
            });
            
            await Task.WhenAll(tasks);
            
            // Sync observer'ları da bilgilendir
            NotifyObservers();
        }

        #endregion

        #region IFilterableObservable Implementation

        public string AddFilter(Func<T, bool> filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            
            lock (_filterLock)
            {
                var filterId = Guid.NewGuid().ToString();
                _namedFilters[filterId] = filter;
                return filterId;
            }
        }

        public void AddFilter(string name, Func<T, bool> filter)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Filter name cannot be null or empty", nameof(name));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            
            lock (_filterLock)
            {
                _namedFilters[name] = filter;
            }
        }

        public void RemoveFilter(Func<T, bool> filter)
        {
            if (filter == null) return;
            
            lock (_filterLock)
            {
                _filters.Remove(filter);
            }
        }

        public void RemoveFilter(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            
            lock (_filterLock)
            {
                _namedFilters.Remove(name);
            }
        }

        public void ClearFilters()
        {
            lock (_filterLock)
            {
                _filters.Clear();
                _namedFilters.Clear();
            }
        }

        public bool HasFilter(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            lock (_filterLock)
            {
                return _namedFilters.ContainsKey(name);
            }
        }

        #endregion

        #region ITransformableObservable Implementation

        public IObservable<TOutput> Transform<TOutput>(Func<T, TOutput> transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            
            var transformedProperty = new ReactiveProperty<TOutput>(transform(Value), $"{Name}_Transformed");
            
            // Bu property'nin değişikliklerini transform edilmiş property'ye aktar
            Subscribe(value =>
            {
                try
                {
                    var transformedValue = transform(value);
                    transformedProperty.SetValue(transformedValue);
                }
                catch (Exception ex)
                {
                    transformedProperty.Error(ex);
                }
            });
            
            return (ITransformableObservable<T, TOutput>)transformedProperty;
        }

        public IObservable<TOutput> SelectMany<TOutput>(Func<T, IObservable<TOutput>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            
            var resultProperty = new ReactiveProperty<TOutput>(default(TOutput), $"{Name}_SelectMany");
            
            Subscribe(value =>
            {
                try
                {
                    var innerObservable = selector(value);
                    innerObservable.Subscribe(innerValue =>
                    {
                        resultProperty.SetValue(innerValue);
                    });
                }
                catch (Exception ex)
                {
                    resultProperty.Error(ex);
                }
            });
            
            return resultProperty;
        }

        #endregion

        #region Reactive Extensions

        /// <summary>
        /// Async filtre ekler
        /// </summary>
        /// <param name="name">Filtre adı</param>
        /// <param name="asyncFilter">Async filtre fonksiyonu</param>
        public void AddAsyncFilter(string name, Func<T, Task<bool>> asyncFilter)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Filter name cannot be null or empty", nameof(name));
            if (asyncFilter == null) throw new ArgumentNullException(nameof(asyncFilter));
            
            lock (_asyncFilters)
            {
                _asyncFilters[name] = asyncFilter;
            }
        }

        /// <summary>
        /// Async filtre kaldırır
        /// </summary>
        /// <param name="name">Filtre adı</param>
        public void RemoveAsyncFilter(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            
            lock (_asyncFilters)
            {
                _asyncFilters.Remove(name);
            }
        }

        /// <summary>
        /// Throttle ile değer atar
        /// </summary>
        /// <param name="value">Yeni değer</param>
        public new void SetValue(T value)
        {
            // Filter kontrolü
            if (_isFilterEnabled && !PassesFilters(value))
            {
                FilterApplied?.Invoke(value, false);
                return;
            }
            
            // Throttle kontrolü
            if (_isThrottleEnabled && DateTime.UtcNow - _lastNotificationTime < _throttleInterval)
            {
                ThrottleApplied?.Invoke(value);
                return;
            }
            
            base.SetValue(value);
            _lastNotificationTime = DateTime.UtcNow;
        }

        public async Task<string> SubscribeAsync(Func<T, Task> onNext, Func<Exception, Task> onError = null, Func<Task> onCompleted = null, int priority = 0)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            
            await _asyncSemaphore.WaitAsync();
            try
            {
                var subscriptionId = Guid.NewGuid().ToString();
                var asyncObserver = new AsyncObserver<T>(subscriptionId, onNext, onError, onCompleted, priority);
                _asyncObservers.Add(asyncObserver);
                return subscriptionId;
            }
            finally
            {
                _asyncSemaphore.Release();
            }
        }

        public ITransformableObservable<T, TResult> TransformObservable<TResult>(Func<T, TResult> transformer)
        {
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));
            
            var transformedProperty = new ReactiveProperty<TResult>(default(TResult));
            
            Subscribe(value => 
            {
                try
                {
                    var transformedValue = transformer(value);
                    transformedProperty.SetValue(transformedValue);
                }
                catch (Exception ex)
                {
                    transformedProperty.Error(ex);
                }
            });
            
            return (ITransformableObservable<T, TResult>)transformedProperty;
        }

        /// <summary>
        /// Async olarak değer atar
        /// </summary>
        /// <param name="value">Yeni değer</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetValueAsync(T value, CancellationToken cancellationToken = default)
        {
            if (_isThrottleEnabled && ShouldThrottle())
            {
                ThrottleApplied?.Invoke(value);
                return;
            }
            
            if (_isFilterEnabled && !await PassesAsyncFilters(value, cancellationToken))
            {
                FilterApplied?.Invoke(value, false);
                return;
            }
            
            FilterApplied?.Invoke(value, true);
            _lastNotificationTime = DateTime.UtcNow;
            base.SetValue(value);
            
            await NotifyObserversAsync(cancellationToken);
        }

        /// <summary>
        /// Debounce ile değer atar
        /// </summary>
        /// <param name="value">Yeni değer</param>
        /// <param name="debounceMs">Debounce süresi (ms)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetValueDebounced(T value, int debounceMs, CancellationToken cancellationToken = default)
        {
            await Task.Delay(debounceMs, cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                await SetValueAsync(value, cancellationToken);
            }
        }

        /// <summary>
        /// Koşullu async değer atama
        /// </summary>
        /// <param name="condition">Async koşul</param>
        /// <param name="value">Yeni değer</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Değer değişti mi</returns>
        public async Task<bool> SetValueIfAsync(Func<T, Task<bool>> condition, T value, CancellationToken cancellationToken = default)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            
            if (await condition(Value))
            {
                await SetValueAsync(value, cancellationToken);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Async transform
        /// </summary>
        /// <param name="transform">Async transform fonksiyonu</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task TransformAsync(Func<T, Task<T>> transform, CancellationToken cancellationToken = default)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            
            var newValue = await transform(Value);
            await SetValueAsync(newValue, cancellationToken);
        }

        /// <summary>
        /// Reactive property'yi başka bir property ile birleştirir
        /// </summary>
        /// <typeparam name="TOther">Diğer property tipi</typeparam>
        /// <typeparam name="TResult">Sonuç tipi</typeparam>
        /// <param name="other">Diğer property</param>
        /// <param name="combiner">Birleştirici fonksiyon</param>
        /// <returns>Birleştirilmiş property</returns>
        public ReactiveProperty<TResult> CombineWith<TOther, TResult>(ReactiveProperty<TOther> other, Func<T, TOther, TResult> combiner)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (combiner == null) throw new ArgumentNullException(nameof(combiner));
            
            var combinedProperty = new ReactiveProperty<TResult>(
                combiner(Value, other.Value), 
                $"{Name}_Combined_{other.Name}"
            );
            
            // Her iki property'nin değişikliklerini dinle
            Subscribe(value => combinedProperty.SetValue(combiner(value, other.Value)));
            other.Subscribe(otherValue => combinedProperty.SetValue(combiner(Value, otherValue)));
            
            return combinedProperty;
        }

        /// <summary>
        /// Property'yi başka bir property'ye bağlar
        /// </summary>
        /// <param name="target">Hedef property</param>
        /// <param name="transform">Transform fonksiyonu (opsiyonel)</param>
        public void BindTo(ReactiveProperty<T> target, Func<T, T> transform = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            
            Subscribe(value =>
            {
                var transformedValue = transform != null ? transform(value) : value;
                target.SetValue(transformedValue);
            });
        }

        #endregion

        #region Private Methods

        private bool ShouldThrottle()
        {
            return DateTime.UtcNow - _lastNotificationTime < _throttleInterval;
        }

        private bool PassesFilters(T value)
        {
            lock (_filterLock)
            {
                // Anonymous filtreler
                foreach (var filter in _filters)
                {
                    try
                    {
                        if (!filter(value))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in filter: {ex.Message}");
                        return false;
                    }
                }
                
                // Named filtreler
                foreach (var filter in _namedFilters.Values)
                {
                    try
                    {
                        if (!filter(value))
                            return false;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in named filter: {ex.Message}");
                        return false;
                    }
                }
                
                return true;
            }
        }

        private async Task<bool> PassesAsyncFilters(T value, CancellationToken cancellationToken)
        {
            Func<T, Task<bool>>[] filters;
            lock (_asyncFilters)
            {
                filters = _asyncFilters.Values.ToArray();
            }
            
            var tasks = filters.Select(async filter =>
            {
                try
                {
                    return await filter(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in async filter: {ex.Message}");
                    return false;
                }
            });
            
            var results = await Task.WhenAll(tasks);
            return results.All(result => result);
        }

        #endregion

        #region Dispose

        public new void Dispose()
        {
            _asyncSemaphore?.Dispose();
            
            lock (_asyncObservers)
            {
                _asyncObservers.Clear();
            }
            
            lock (_filterLock)
            {
                _filters.Clear();
                _namedFilters.Clear();
            }
            
            lock (_asyncFilters)
            {
                _asyncFilters.Clear();
            }
            
            FilterApplied = null;
            ThrottleApplied = null;
            
            base.Dispose();
        }

        #region IAsyncObservable Implementation

        string IAsyncObservable<T>.Subscribe(IAsyncObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            lock (_asyncObservers)
            {
                if (!IsActive)
                    throw new InvalidOperationException("ReactiveProperty is not active");

                _asyncObservers.Add(observer);
                return observer.Id;
            }
        }

        string IAsyncObservable<T>.SubscribeAsync(Func<T, Task> onNext, Func<Exception, Task> onError, Func<Task> onCompleted, int priority)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            
            _asyncSemaphore.Wait();
            try
            {
                var subscriptionId = Guid.NewGuid().ToString();
                var asyncObserver = new AsyncObserver<T>(subscriptionId, onNext, onError, onCompleted, priority);
                _asyncObservers.Add(asyncObserver);
                return subscriptionId;
            }
            finally
            {
                _asyncSemaphore.Release();
            }
        }

        Task IAsyncObservable<T>.NotifyObserversAsync()
        {
            return NotifyObserversAsync(CancellationToken.None);
        }

        #endregion

        #region ITransformableObservable Implementation

        IObservable<T> ITransformableObservable<T, T>.Source => this;

        Func<T, T> ITransformableObservable<T, T>.Transform => value => value;

        #endregion

        #endregion

        #region ToString

        public override string ToString()
        {
            return $"ReactiveProperty<{typeof(T).Name}> [Name: {Name}, Value: {Value}, Observers: {ObserverCount}, AsyncObservers: {AsyncObserverCount}, Filters: {FilterCount}, Throttle: {_isThrottleEnabled}]";
        }

        #endregion
    }
}
