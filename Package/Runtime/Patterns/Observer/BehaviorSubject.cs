using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Behavior Subject implementasyonu - son değeri saklar ve yeni subscriber'lara hemen gönderir
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public class BehaviorSubject<T> : Subject<T>, IBehaviorSubject<T>
    {
        private readonly T _initialValue;
        private bool _hasValue;
        private readonly object _lock = new object();

        #region Properties

        public T InitialValue => _initialValue;
        public bool HasValue
        {
            get
            {
                lock (_lock)
                {
                    return _hasValue;
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
        public BehaviorSubject(T initialValue, string name = null) : base(initialValue, name)
        {
            _initialValue = initialValue;
            _hasValue = true;
        }

        #endregion

        #region IBehaviorSubject Implementation

        public T GetLastValue()
        {
            lock (_lock)
            {
                if (!_hasValue)
                    throw new InvalidOperationException("BehaviorSubject has no value");

                return Value;
            }
        }

        public void ResetToInitialValue()
        {
            SetValue(_initialValue);
        }

        #endregion

        #region Override Methods

        public new string Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            var subscriptionId = base.Subscribe(observer);

            // Yeni subscriber'a hemen mevcut değeri gönder
            if (_hasValue && IsActive)
            {
                try
                {
                    observer.OnNext(Value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error sending current value to new observer: {ex.Message}");
                }
            }

            return subscriptionId;
        }

        public override string Subscribe(Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null, int priority = 0)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));

            var subscriptionId = base.Subscribe(onNext, onError, onCompleted, priority);

            // Yeni subscriber'a hemen mevcut değeri gönder
            if (_hasValue && IsActive)
            {
                try
                {
                    onNext(Value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error sending current value to new action subscriber: {ex.Message}");
                }
            }

            return subscriptionId;
        }

        public new void SetValue(T value)
        {
            lock (_lock)
            {
                _hasValue = true;
            }

            base.SetValue(value);
        }

        public override void SetValueSilently(T value)
        {
            lock (_lock)
            {
                _hasValue = true;
            }

            base.SetValueSilently(value);
        }

        public override void Reset()
        {
            lock (_lock)
            {
                _hasValue = true;
            }

            base.Reset();
        }

        public override ISubject<T> Clone()
        {
            lock (_lock)
            {
                return new BehaviorSubject<T>(_hasValue ? Value : _initialValue, $"{Name}_Clone");
            }
        }
    }
}
#endregion
