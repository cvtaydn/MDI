using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Signal bus implementasyonu
    /// </summary>
    public class SignalBus : ISignalBus
    {
        private readonly Dictionary<Type, List<ISignalHandler>> _handlers;
        private readonly Dictionary<Type, List<ActionSubscription>> _actionSubscriptions;
        private readonly Dictionary<string, ActionSubscription> _subscriptionById;
        private readonly Queue<ISignal> _signalQueue;
        private readonly Queue<DelayedSignal> _delayedSignals;
        private readonly Queue<ConditionalSignal> _conditionalSignals;
        
        private bool _isActive = true;
        private bool _isPaused = false;
        private int _nextSubscriptionId = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        public SignalBus()
        {
            _handlers = new Dictionary<Type, List<ISignalHandler>>();
            _actionSubscriptions = new Dictionary<Type, List<ActionSubscription>>();
            _subscriptionById = new Dictionary<string, ActionSubscription>();
            _signalQueue = new Queue<ISignal>();
            _delayedSignals = new Queue<DelayedSignal>();
            _conditionalSignals = new Queue<ConditionalSignal>();
        }

        /// <summary>
        /// Signal'a handler ekler
        /// </summary>
        public void Subscribe<TSignal>(ISignalHandler<TSignal> handler) where TSignal : ISignal
        {
            if (handler == null) return;

            var signalType = typeof(TSignal);
            if (!_handlers.ContainsKey(signalType))
            {
                _handlers[signalType] = new List<ISignalHandler>();
            }

            // Generic handler'ı non-generic wrapper ile sar
            var wrapper = new GenericSignalHandlerWrapper<TSignal>(handler);
            _handlers[signalType].Add(wrapper);
            _handlers[signalType].Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Yüksek priority önce
        }

        /// <summary>
        /// Signal'dan handler'ı çıkarır
        /// </summary>
        public void Unsubscribe<TSignal>(ISignalHandler<TSignal> handler) where TSignal : ISignal
        {
            if (handler == null) return;

            var signalType = typeof(TSignal);
            if (_handlers.ContainsKey(signalType))
            {
                // Generic handler'ı non-generic wrapper ile sar ve kaldır
                var wrapper = new GenericSignalHandlerWrapper<TSignal>(handler);
                _handlers[signalType].Remove(wrapper);
            }
        }

        /// <summary>
        /// Action-based subscription ekler
        /// </summary>
        public string Subscribe<TSignal>(Action<TSignal> action, int priority = 0) where TSignal : ISignal
        {
            if (action == null) return null;

            var subscriptionId = $"sub_{_nextSubscriptionId++}";
            var signalType = typeof(TSignal);
            
            if (!_actionSubscriptions.ContainsKey(signalType))
            {
                _actionSubscriptions[signalType] = new List<ActionSubscription>();
            }

            var subscription = new ActionSubscription
            {
                Id = subscriptionId,
                Action = signal => action((TSignal)signal),
                Priority = priority,
                SignalType = signalType
            };

            _actionSubscriptions[signalType].Add(subscription);
            _subscriptionById[subscriptionId] = subscription;
            
            // Priority'ye göre sırala
            _actionSubscriptions[signalType].Sort((a, b) => b.Priority.CompareTo(a.Priority));

            return subscriptionId;
        }

        /// <summary>
        /// Action-based subscription'ı çıkarır
        /// </summary>
        public void Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId) || !_subscriptionById.ContainsKey(subscriptionId))
                return;

            var subscription = _subscriptionById[subscriptionId];
            var signalType = subscription.SignalType;

            if (_actionSubscriptions.ContainsKey(signalType))
            {
                _actionSubscriptions[signalType].Remove(subscription);
            }

            _subscriptionById.Remove(subscriptionId);
        }

        /// <summary>
        /// Signal'ı publish eder
        /// </summary>
        public void Publish<TSignal>(TSignal signal) where TSignal : ISignal
        {
            if (signal == null || !_isActive || _isPaused) return;

            try
            {
                // Handler'ları çağır
                PublishToHandlers(signal);
                
                // Action subscription'ları çağır
                PublishToActions(signal);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing signal {typeof(TSignal).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Signal'ı async olarak publish eder
        /// </summary>
        public async Task PublishAsync<TSignal>(TSignal signal) where TSignal : ISignal
        {
            if (signal == null || !_isActive || _isPaused) return;

            try
            {
                await Task.Run(() =>
                {
                    PublishToHandlers(signal);
                    PublishToActions(signal);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing signal {typeof(TSignal).Name} async: {ex.Message}");
            }
        }

        /// <summary>
        /// Signal'ı belirli bir delay ile publish eder
        /// </summary>
        public void PublishDelayed<TSignal>(TSignal signal, int delayMs) where TSignal : ISignal
        {
            if (signal == null || delayMs <= 0) return;

            var delayedSignal = new DelayedSignal
            {
                Signal = signal,
                PublishTime = DateTimeOffset.UtcNow.AddMilliseconds(delayMs)
            };

            _delayedSignals.Enqueue(delayedSignal);
        }

        /// <summary>
        /// Signal'ı belirli bir condition'a kadar bekletir
        /// </summary>
        public void PublishWhen<TSignal>(TSignal signal, Func<bool> condition) where TSignal : ISignal
        {
            if (signal == null || condition == null) return;

            var conditionalSignal = new ConditionalSignal
            {
                Signal = signal,
                Condition = condition
            };

            _conditionalSignals.Enqueue(conditionalSignal);
        }

        /// <summary>
        /// Tüm subscription'ları temizler
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
            _actionSubscriptions.Clear();
            _subscriptionById.Clear();
            _signalQueue.Clear();
            _delayedSignals.Clear();
            _conditionalSignals.Clear();
        }

        /// <summary>
        /// Belirli bir signal tipi için tüm subscription'ları temizler
        /// </summary>
        public void Clear<TSignal>() where TSignal : ISignal
        {
            var signalType = typeof(TSignal);
            
            if (_handlers.ContainsKey(signalType))
                _handlers[signalType].Clear();
                
            if (_actionSubscriptions.ContainsKey(signalType))
                _actionSubscriptions[signalType].Clear();
        }

        /// <summary>
        /// Signal bus'ın durumunu kontrol eder
        /// </summary>
        public bool IsActive => _isActive && !_isPaused;

        /// <summary>
        /// Signal bus'ı pause eder
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Signal bus'ı resume eder
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Update method - delayed ve conditional signal'ları işler
        /// </summary>
        public void Update()
        {
            if (!_isActive || _isPaused) return;

            // Delayed signal'ları kontrol et
            ProcessDelayedSignals();
            
            // Conditional signal'ları kontrol et
            ProcessConditionalSignals();
        }

        /// <summary>
        /// Handler'lara signal publish eder
        /// </summary>
        private void PublishToHandlers<TSignal>(TSignal signal) where TSignal : ISignal
        {
            var signalType = typeof(TSignal);
            
            if (!_handlers.ContainsKey(signalType)) return;

            var handlers = _handlers[signalType].ToList(); // Copy to avoid modification during iteration
            
            foreach (var handler in handlers)
            {
                if (handler.IsActive)
                {
                    try
                    {
                        handler.Handle(signal);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in signal handler {handler.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Action subscription'lara signal publish eder
        /// </summary>
        private void PublishToActions<TSignal>(TSignal signal) where TSignal : ISignal
        {
            var signalType = typeof(TSignal);
            
            if (!_actionSubscriptions.ContainsKey(signalType)) return;

            var subscriptions = _actionSubscriptions[signalType].ToList(); // Copy to avoid modification during iteration
            
            foreach (var subscription in subscriptions)
            {
                try
                {
                    subscription.Action(signal);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in signal action subscription {subscription.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Delayed signal'ları işler
        /// </summary>
        private void ProcessDelayedSignals()
        {
            var currentTime = DateTimeOffset.UtcNow;
            var signalsToPublish = new List<ISignal>();

            while (_delayedSignals.Count > 0)
            {
                var delayedSignal = _delayedSignals.Peek();
                
                if (currentTime >= delayedSignal.PublishTime)
                {
                    _delayedSignals.Dequeue();
                    signalsToPublish.Add(delayedSignal.Signal);
                }
                else
                {
                    break; // Sıradaki signal henüz hazır değil
                }
            }

            // Publish edilecek signal'ları işle
            foreach (var signal in signalsToPublish)
            {
                var method = typeof(SignalBus).GetMethod("Publish").MakeGenericMethod(signal.GetType());
                method.Invoke(this, new object[] { signal });
            }
        }

        /// <summary>
        /// Conditional signal'ları işler
        /// </summary>
        private void ProcessConditionalSignals()
        {
            var signalsToPublish = new List<ISignal>();
            var remainingSignals = new Queue<ConditionalSignal>();

            while (_conditionalSignals.Count > 0)
            {
                var conditionalSignal = _conditionalSignals.Dequeue();
                
                try
                {
                    if (conditionalSignal.Condition())
                    {
                        signalsToPublish.Add(conditionalSignal.Signal);
                    }
                    else
                    {
                        remainingSignals.Enqueue(conditionalSignal);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error checking conditional signal condition: {ex.Message}");
                    remainingSignals.Enqueue(conditionalSignal);
                }
            }

            // Kalan signal'ları geri ekle
            while (remainingSignals.Count > 0)
            {
                _conditionalSignals.Enqueue(remainingSignals.Dequeue());
            }

            // Publish edilecek signal'ları işle
            foreach (var signal in signalsToPublish)
            {
                var method = typeof(SignalBus).GetMethod("Publish").MakeGenericMethod(signal.GetType());
                method.Invoke(this, new object[] { signal });
            }
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        public void Dispose()
        {
            Clear();
        }

        #region Helper Classes

        private class ActionSubscription
        {
            public string Id { get; set; }
            public Action<ISignal> Action { get; set; }
            public int Priority { get; set; }
            public Type SignalType { get; set; }
        }

        private class DelayedSignal
        {
            public ISignal Signal { get; set; }
            public DateTimeOffset PublishTime { get; set; }
        }

        private class ConditionalSignal
        {
            public ISignal Signal { get; set; }
            public Func<bool> Condition { get; set; }
        }

        #endregion
    }
}
