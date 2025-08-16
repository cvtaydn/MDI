using System;
using System.Threading;
using System.Threading.Tasks;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Observer pattern için temel observer arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IObserver<in T>
    {
        /// <summary>
        /// Observer'ın unique identifier'ı
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Observer'ın priority'si (yüksek sayı = yüksek priority)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Observer'ın aktif olup olmadığı
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Değer değiştiğinde çağrılır
        /// </summary>
        /// <param name="value">Yeni değer</param>
        void OnNext(T value);

        /// <summary>
        /// Hata oluştuğunda çağrılır
        /// </summary>
        /// <param name="error">Hata</param>
        void OnError(Exception error);

        /// <summary>
        /// Observable tamamlandığında çağrılır
        /// </summary>
        void OnCompleted();
    }

    /// <summary>
    /// Async observer arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IAsyncObserver<in T>
    {
        /// <summary>
        /// Observer'ın unique identifier'ı
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Observer'ın priority'si
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Observer'ın aktif olup olmadığı
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Değer değiştiğinde asenkron çağrılır
        /// </summary>
        /// <param name="value">Yeni değer</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task OnNextAsync(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hata oluştuğunda asenkron çağrılır
        /// </summary>
        /// <param name="error">Hata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task OnErrorAsync(Exception error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Observable tamamlandığında asenkron çağrılır
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task OnCompletedAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Koşullu observer arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IConditionalObserver<in T> : IObserver<T>
    {
        /// <summary>
        /// Observer'ın bu değer için çalışıp çalışmayacağını belirler
        /// </summary>
        /// <param name="value">Kontrol edilecek değer</param>
        /// <returns>True ise observer çalışır</returns>
        bool ShouldNotify(T value);
    }

    /// <summary>
    /// Filtrelenmiş observer arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IFilteredObserver<in T> : IObserver<T>
    {
        /// <summary>
        /// Değeri filtreler
        /// </summary>
        /// <param name="value">Filtrelenecek değer</param>
        /// <returns>Değer kabul edildi mi</returns>
        bool Filter(T value);

        /// <summary>
        /// Filtreleme aktif mi
        /// </summary>
        bool IsFilterEnabled { get; }
    }

    /// <summary>
    /// Throttled observer arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IThrottledObserver<in T> : IObserver<T>
    {
        /// <summary>
        /// Throttle süresi (milliseconds)
        /// </summary>
        int ThrottleMs { get; }

        /// <summary>
        /// Son notification zamanı
        /// </summary>
        long LastNotificationTime { get; }

        /// <summary>
        /// Throttle aktif mi
        /// </summary>
        bool IsThrottleEnabled { get; }
    }
}