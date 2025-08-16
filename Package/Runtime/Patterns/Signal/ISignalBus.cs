using System;

namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Signal bus interface'i - signal'ları publish/subscribe yapar
    /// </summary>
    public interface ISignalBus
    {
        /// <summary>
        /// Signal'a handler ekler
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="handler">Handler</param>
        void Subscribe<TSignal>(ISignalHandler<TSignal> handler) where TSignal : ISignal;

        /// <summary>
        /// Signal'dan handler'ı çıkarır
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="handler">Handler</param>
        void Unsubscribe<TSignal>(ISignalHandler<TSignal> handler) where TSignal : ISignal;

        /// <summary>
        /// Action-based subscription ekler
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="action">Action</param>
        /// <param name="priority">Priority (default: 0)</param>
        /// <returns>Subscription ID</returns>
        string Subscribe<TSignal>(Action<TSignal> action, int priority = 0) where TSignal : ISignal;

        /// <summary>
        /// Action-based subscription'ı çıkarır
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        void Unsubscribe(string subscriptionId);

        /// <summary>
        /// Signal'ı publish eder
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="signal">Publish edilecek signal</param>
        void Publish<TSignal>(TSignal signal) where TSignal : ISignal;

        /// <summary>
        /// Signal'ı async olarak publish eder
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="signal">Publish edilecek signal</param>
        System.Threading.Tasks.Task PublishAsync<TSignal>(TSignal signal) where TSignal : ISignal;

        /// <summary>
        /// Signal'ı belirli bir delay ile publish eder
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="signal">Publish edilecek signal</param>
        /// <param name="delayMs">Delay (milliseconds)</param>
        void PublishDelayed<TSignal>(TSignal signal, int delayMs) where TSignal : ISignal;

        /// <summary>
        /// Signal'ı belirli bir condition'a kadar bekletir
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        /// <param name="signal">Publish edilecek signal</param>
        /// <param name="condition">Condition function</param>
        void PublishWhen<TSignal>(TSignal signal, Func<bool> condition) where TSignal : ISignal;

        /// <summary>
        /// Tüm subscription'ları temizler
        /// </summary>
        void Clear();

        /// <summary>
        /// Belirli bir signal tipi için tüm subscription'ları temizler
        /// </summary>
        /// <typeparam name="TSignal">Signal tipi</typeparam>
        void Clear<TSignal>() where TSignal : ISignal;

        /// <summary>
        /// Signal bus'ın durumunu kontrol eder
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Signal bus'ı pause eder
        /// </summary>
        void Pause();

        /// <summary>
        /// Signal bus'ı resume eder
        /// </summary>
        void Resume();
    }
}
