using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Replay Subject implementasyonu - belirli sayıda son değeri saklar ve yeni subscriber'lara gönderir
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public class ReplaySubject<T> : Subject<T>, IReplaySubject<T>
    {
        private readonly Queue<T> _buffer;
        private readonly object _bufferLock = new object();
        private int _bufferSize;

        #region Properties

        public int BufferSize 
        { 
            get 
            { 
                lock (_bufferLock) 
                { 
                    return _bufferSize; 
                } 
            } 
        }

        public int CurrentBufferCount 
        { 
            get 
            { 
                lock (_bufferLock) 
                { 
                    return _buffer.Count; 
                } 
            } 
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bufferSize">Buffer boyutu</param>
        /// <param name="initialValue">Başlangıç değeri</param>
        /// <param name="name">Subject adı</param>
        public ReplaySubject(int bufferSize = 10, T initialValue = default(T), string name = null) : base(initialValue, name)
        {
            if (bufferSize <= 0)
                throw new ArgumentException("Buffer size must be greater than 0", nameof(bufferSize));
            
            _bufferSize = bufferSize;
            _buffer = new Queue<T>(bufferSize);
            
            // Başlangıç değeri varsa buffer'a ekle
            if (!EqualityComparer<T>.Default.Equals(initialValue, default(T)))
            {
                _buffer.Enqueue(initialValue);
            }
        }

        #endregion

        #region IReplaySubject Implementation

        public T[] GetBufferedValues()
        {
            lock (_bufferLock)
            {
                return _buffer.ToArray();
            }
        }

        public void ClearBuffer()
        {
            lock (_bufferLock)
            {
                _buffer.Clear();
            }
        }

        public void ResizeBuffer(int newSize)
        {
            if (newSize <= 0)
                throw new ArgumentException("Buffer size must be greater than 0", nameof(newSize));
            
            lock (_bufferLock)
            {
                _bufferSize = newSize;
                
                // Eğer mevcut buffer yeni boyuttan büyükse, eski değerleri çıkar
                while (_buffer.Count > _bufferSize)
                {
                    _buffer.Dequeue();
                }
            }
        }

        #endregion

        #region Override Methods

        public new string Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            var subscriptionId = base.Subscribe(observer);
            
            // Yeni subscriber'a buffer'daki tüm değerleri gönder
            if (IsActive)
            {
                T[] bufferedValues;
                lock (_bufferLock)
                {
                    bufferedValues = _buffer.ToArray();
                }
                
                foreach (var value in bufferedValues)
                {
                    try
                    {
                        observer.OnNext(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending buffered value to new observer: {ex.Message}");
                    }
                }
            }
            
            return subscriptionId;
        }

        public override string Subscribe(Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null, int priority = 0)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            
            var subscriptionId = base.Subscribe(onNext, onError, onCompleted, priority);
            
            // Yeni subscriber'a buffer'daki tüm değerleri gönder
            if (IsActive)
            {
                T[] bufferedValues;
                lock (_bufferLock)
                {
                    bufferedValues = _buffer.ToArray();
                }
                
                foreach (var value in bufferedValues)
                {
                    try
                    {
                        onNext(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending buffered value to new action subscriber: {ex.Message}");
                    }
                }
            }
            
            return subscriptionId;
        }

        public override void SetValue(T value)
        {
            // Buffer'a değeri ekle
            lock (_bufferLock)
            {
                _buffer.Enqueue(value);
                
                // Buffer boyutunu aş tıysa eski değeri çıkar
                if (_buffer.Count > _bufferSize)
                {
                    _buffer.Dequeue();
                }
            }
            
            base.SetValue(value);
        }

        public override void SetValueSilently(T value)
        {
            // Buffer'a değeri ekle
            lock (_bufferLock)
            {
                _buffer.Enqueue(value);
                
                // Buffer boyutunu aştıysa eski değeri çıkar
                if (_buffer.Count > _bufferSize)
                {
                    _buffer.Dequeue();
                }
            }
            
            base.SetValueSilently(value);
        }

        public override void Reset()
        {
            lock (_bufferLock)
            {
                _buffer.Clear();
            }
            
            base.Reset();
        }

        public override ISubject<T> Clone()
        {
            lock (_bufferLock)
            {
                var clone = new ReplaySubject<T>(_bufferSize, default(T), $"{Name}_Clone");
                
                // Buffer'daki değerleri klona kopyala
                foreach (var value in _buffer)
                {
                    clone.SetValueSilently(value);
                }
                
                return clone;
            }
        }

        public override void Dispose()
        {
            lock (_bufferLock)
            {
                _buffer.Clear();
            }
            
            base.Dispose();
        }

        #endregion
    }
}
