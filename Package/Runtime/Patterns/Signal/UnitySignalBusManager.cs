using UnityEngine;
using System;

namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Unity ile entegre signal bus manager
    /// </summary>
    public class UnitySignalBusManager : MonoBehaviour
    {
        private static UnitySignalBusManager _instance;
        private SignalBus _signalBus;
        private bool _isInitialized = false;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static UnitySignalBusManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UnitySignalBusManager>();

                    if (_instance == null)
                    {
                        var go = new GameObject("UnitySignalBusManager");
                        _instance = go.AddComponent<UnitySignalBusManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Signal bus instance'ı
        /// </summary>
        public SignalBus SignalBus
        {
            get
            {
                if (!_isInitialized)
                {
                    Initialize();
                }

                return _signalBus;
            }
        }

        /// <summary>
        /// Awake
        /// </summary>
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

        /// <summary>
        /// Initialize
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;

            _signalBus = new SignalBus();
            _isInitialized = true;

            Debug.Log("UnitySignalBusManager initialized successfully!");
        }

        /// <summary>
        /// Update - signal bus'ı günceller
        /// </summary>
        private void Update()
        {
            if (_isInitialized)
            {
                _signalBus.Update();
            }
        }

        /// <summary>
        /// OnDestroy
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _signalBus?.Dispose();
                _instance = null;
            }
        }

        /// <summary>
        /// OnApplicationPause
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (_isInitialized)
            {
                if (pauseStatus)
                {
                    _signalBus.Pause();
                }
                else
                {
                    _signalBus.Resume();
                }
            }
        }

        /// <summary>
        /// OnApplicationFocus
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (_isInitialized)
            {
                if (!hasFocus)
                {
                    _signalBus.Pause();
                }
                else
                {
                    _signalBus.Resume();
                }
            }
        }

        #region Static Helper Methods

        /// <summary>
        /// Signal'a subscribe olur
        /// </summary>
        public static string Subscribe<TSignal>(Action<TSignal> action, int priority = 0) where TSignal : ISignal
        {
            return Instance.SignalBus.Subscribe(action, priority);
        }

        /// <summary>
        /// Signal'dan unsubscribe olur
        /// </summary>
        public static void Unsubscribe(string subscriptionId)
        {
            Instance.SignalBus.Unsubscribe(subscriptionId);
        }

        /// <summary>
        /// Signal'ı publish eder
        /// </summary>
        public static void Publish<TSignal>(TSignal signal) where TSignal : ISignal
        {
            Instance.SignalBus.Publish(signal);
        }

        /// <summary>
        /// Signal'ı async olarak publish eder
        /// </summary>
        public static async System.Threading.Tasks.Task PublishAsync<TSignal>(TSignal signal) where TSignal : ISignal
        {
            await Instance.SignalBus.PublishAsync(signal);
        }

        /// <summary>
        /// Signal'ı delayed olarak publish eder
        /// </summary>
        public static void PublishDelayed<TSignal>(TSignal signal, int delayMs) where TSignal : ISignal
        {
            Instance.SignalBus.PublishDelayed(signal, delayMs);
        }

        /// <summary>
        /// Signal'ı condition'a göre publish eder
        /// </summary>
        public static void PublishWhen<TSignal>(TSignal signal, Func<bool> condition) where TSignal : ISignal
        {
            Instance.SignalBus.PublishWhen(signal, condition);
        }

        /// <summary>
        /// Signal bus'ı pause eder
        /// </summary>
        public static void Pause()
        {
            Instance.SignalBus.Pause();
        }

        /// <summary>
        /// Signal bus'ı resume eder
        /// </summary>
        public static void Resume()
        {
            Instance.SignalBus.Resume();
        }

        /// <summary>
        /// Signal bus'ı temizler
        /// </summary>
        public static void Clear()
        {
            Instance.SignalBus.Clear();
        }

        /// <summary>
        /// Belirli bir signal tipi için subscription'ları temizler
        /// </summary>
        public static void Clear<TSignal>() where TSignal : ISignal
        {
            Instance.SignalBus.Clear<TSignal>();
        }

        #endregion
    }
}