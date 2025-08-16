using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Observable pattern için temel observable arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IObservable<out T>
    {
        /// <summary>
        /// Observable'ın unique identifier'ı
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Observable'ın adı
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Mevcut değer
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Observable'ın aktif olup olmadığı
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Observable'ın tamamlanıp tamamlanmadığı
        /// </summary>
        bool IsCompleted { get; }



        /// <summary>
        /// Observer sayısı
        /// </summary>
        int ObserverCount { get; }

        /// <summary>
        /// Observer ekler
        /// </summary>
        /// <param name="observer">Eklenecek observer</param>
        /// <returns>Subscription ID</returns>
        string Subscribe(IObserver<T> observer);

        /// <summary>
        /// Observer çıkarır
        /// </summary>
        /// <param name="observer">Çıkarılacak observer</param>
        void Unsubscribe(IObserver<T> observer);

        /// <summary>
        /// Subscription ID ile observer çıkarır
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        void Unsubscribe(string subscriptionId);

        /// <summary>
        /// Action-based subscription ekler
        /// </summary>
        /// <param name="onNext">OnNext action</param>
        /// <param name="onError">OnError action (optional)</param>
        /// <param name="onCompleted">OnCompleted action (optional)</param>
        /// <param name="priority">Priority (default: 0)</param>
        /// <returns>Subscription ID</returns>
        string Subscribe(Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null, int priority = 0);

        /// <summary>
        /// Tüm observer'ları çıkarır
        /// </summary>
        void UnsubscribeAll();

        /// <summary>
        /// Observable'ı tamamlar
        /// </summary>
        void Complete();

        /// <summary>
        /// Observable'ı hata ile tamamlar
        /// </summary>
        /// <param name="error">Hata</param>
        void Error(Exception error);

        /// <summary>
        /// Observable'ı dispose eder
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Mutable observable arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IMutableObservable<T> : IObservable<T>
    {
        /// <summary>
        /// Değeri değiştirir ve observer'ları bilgilendirir
        /// </summary>
        /// <param name="value">Yeni değer</param>
        void SetValue(T value);

        /// <summary>
        /// Değeri sessizce değiştirir (observer'ları bilgilendirmez)
        /// </summary>
        /// <param name="value">Yeni değer</param>
        void SetValueSilently(T value);

        /// <summary>
        /// Observer'ları manuel olarak bilgilendirir
        /// </summary>
        void NotifyObservers();

        /// <summary>
        /// Belirli bir observer'ı bilgilendirir
        /// </summary>
        /// <param name="observer">Bilgilendirilecek observer</param>
        void NotifyObserver(IObserver<T> observer);
    }

    /// <summary>
    /// Async observable arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IAsyncObservable<out T> : IObservable<T>
    {
        /// <summary>
        /// Async observer ekler
        /// </summary>
        /// <param name="observer">Eklenecek async observer</param>
        /// <returns>Subscription ID</returns>
        string Subscribe(IAsyncObserver<T> observer);

        /// <summary>
        /// Async action-based subscription ekler
        /// </summary>
        /// <param name="onNextAsync">OnNext async action</param>
        /// <param name="onErrorAsync">OnError async action (optional)</param>
        /// <param name="onCompletedAsync">OnCompleted async action (optional)</param>
        /// <param name="priority">Priority (default: 0)</param>
        /// <returns>Subscription ID</returns>
        string SubscribeAsync(Func<T, Task> onNextAsync, Func<Exception, Task> onErrorAsync = null, Func<Task> onCompletedAsync = null, int priority = 0);

        /// <summary>
        /// Observer'ları asenkron bilgilendirir
        /// </summary>
        Task NotifyObserversAsync();
    }

    /// <summary>
    /// Filtrelenebilir observable arayüzü
    /// </summary>
    /// <typeparam name="T">Gözlemlenen veri tipi</typeparam>
    public interface IFilterableObservable<T> : IObservable<T>
    {
        /// <summary>
        /// Filtre ekler
        /// </summary>
        /// <param name="filter">Filtre fonksiyonu</param>
        /// <returns>Filter ID</returns>
        string AddFilter(Func<T, bool> filter);

        /// <summary>
        /// Filtre çıkarır
        /// </summary>
        /// <param name="filterId">Filter ID</param>
        void RemoveFilter(string filterId);

        /// <summary>
        /// Tüm filtreleri temizler
        /// </summary>
        void ClearFilters();

        /// <summary>
        /// Aktif filtre sayısı
        /// </summary>
        int FilterCount { get; }
    }

    /// <summary>
    /// Transformable observable arayüzü
    /// </summary>
    /// <typeparam name="TInput">Giriş tipi</typeparam>
    /// <typeparam name="TOutput">Çıkış tipi</typeparam>
    public interface ITransformableObservable<TInput, out TOutput> : IObservable<TOutput>
    {
        /// <summary>
        /// Kaynak observable
        /// </summary>
        IObservable<TInput> Source { get; }

        /// <summary>
        /// Transform fonksiyonu
        /// </summary>
        Func<TInput, TOutput> Transform { get; }
    }

    /// <summary>
    /// Disposable subscription arayüzü
    /// </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Subscription ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Subscription aktif mi
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Observer
        /// </summary>
        object Observer { get; }

        /// <summary>
        /// Observable
        /// </summary>
        object Observable { get; }

        /// <summary>
        /// Subscription zamanı
        /// </summary>
        DateTime SubscriptionTime { get; }
    }
}