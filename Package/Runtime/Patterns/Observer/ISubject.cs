using System;
using System.Threading.Tasks;

namespace MDI.Patterns.Observer
{
    /// <summary>
    /// Subject pattern için temel arayüz - hem Observable hem Observer
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public interface ISubject<T> : IObservable<T>, IObserver<T>, IMutableObservable<T>
    {
        /// <summary>
        /// Subject'in durumu
        /// </summary>
        SubjectState State { get; }

        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        DateTime LastUpdateTime { get; }

        /// <summary>
        /// Toplam notification sayısı
        /// </summary>
        int NotificationCount { get; }

        /// <summary>
        /// Subject'i resetler
        /// </summary>
        void Reset();

        /// <summary>
        /// Subject'i pause eder
        /// </summary>
        void Pause();

        /// <summary>
        /// Subject'i resume eder
        /// </summary>
        void Resume();

        /// <summary>
        /// Subject'in bir kopyasını oluşturur
        /// </summary>
        /// <returns>Kopya subject</returns>
        ISubject<T> Clone();
    }

    /// <summary>
    /// Async subject arayüzü
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public interface IAsyncSubject<T> : ISubject<T>, IAsyncObservable<T>, IAsyncObserver<T>
    {
        /// <summary>
        /// Değeri asenkron olarak değiştirir
        /// </summary>
        /// <param name="value">Yeni değer</param>
        Task SetValueAsync(T value);

        /// <summary>
        /// Subject'i asenkron olarak tamamlar
        /// </summary>
        Task CompleteAsync();

        /// <summary>
        /// Subject'i asenkron olarak hata ile tamamlar
        /// </summary>
        /// <param name="error">Hata</param>
        Task ErrorAsync(Exception error);
    }

    /// <summary>
    /// Behavior subject arayüzü - son değeri saklar
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public interface IBehaviorSubject<T> : ISubject<T>
    {
        /// <summary>
        /// Başlangıç değeri
        /// </summary>
        T InitialValue { get; }

        /// <summary>
        /// Değer var mı
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// Son değeri alır
        /// </summary>
        /// <returns>Son değer</returns>
        T GetLastValue();

        /// <summary>
        /// Değeri başlangıç değerine resetler
        /// </summary>
        void ResetToInitialValue();
    }

    /// <summary>
    /// Replay subject arayüzü - belirli sayıda son değeri saklar
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public interface IReplaySubject<T> : ISubject<T>
    {
        /// <summary>
        /// Buffer boyutu
        /// </summary>
        int BufferSize { get; }

        /// <summary>
        /// Mevcut buffer'daki değer sayısı
        /// </summary>
        int CurrentBufferCount { get; }

        /// <summary>
        /// Buffer'daki tüm değerleri alır
        /// </summary>
        /// <returns>Buffer'daki değerler</returns>
        T[] GetBufferedValues();

        /// <summary>
        /// Buffer'ı temizler
        /// </summary>
        void ClearBuffer();

        /// <summary>
        /// Buffer boyutunu değiştirir
        /// </summary>
        /// <param name="newSize">Yeni boyut</param>
        void ResizeBuffer(int newSize);
    }

    /// <summary>
    /// Publish subject arayüzü - sadece subscription sonrası değerleri gönderir
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public interface IPublishSubject<T> : ISubject<T>
    {
        /// <summary>
        /// Hot observable mı (subscription öncesi değerleri göndermez)
        /// </summary>
        bool IsHot { get; }

        /// <summary>
        /// Cold observable'a çevirir
        /// </summary>
        /// <returns>Cold observable</returns>
        IObservable<T> ToCold();

        /// <summary>
        /// Hot observable'a çevirir
        /// </summary>
        /// <returns>Hot observable</returns>
        IObservable<T> ToHot();
    }

    /// <summary>
    /// Subject durumları
    /// </summary>
    public enum SubjectState
    {
        /// <summary>
        /// Aktif - normal çalışma durumu
        /// </summary>
        Active,

        /// <summary>
        /// Duraklatılmış - notification'lar gönderilmez
        /// </summary>
        Paused,

        /// <summary>
        /// Tamamlanmış - artık değer kabul etmez
        /// </summary>
        Completed,

        /// <summary>
        /// Hata durumu - hata ile tamamlanmış
        /// </summary>
        Error,

        /// <summary>
        /// Dispose edilmiş
        /// </summary>
        Disposed
    }

    /// <summary>
    /// Subject factory arayüzü
    /// </summary>
    public interface ISubjectFactory
    {
        /// <summary>
        /// Behavior subject oluşturur
        /// </summary>
        /// <typeparam name="T">Veri tipi</typeparam>
        /// <param name="initialValue">Başlangıç değeri</param>
        /// <returns>Behavior subject</returns>
        IBehaviorSubject<T> CreateBehaviorSubject<T>(T initialValue);

        /// <summary>
        /// Replay subject oluşturur
        /// </summary>
        /// <typeparam name="T">Veri tipi</typeparam>
        /// <param name="bufferSize">Buffer boyutu</param>
        /// <returns>Replay subject</returns>
        IReplaySubject<T> CreateReplaySubject<T>(int bufferSize = 10);

        /// <summary>
        /// Publish subject oluşturur
        /// </summary>
        /// <typeparam name="T">Veri tipi</typeparam>
        /// <returns>Publish subject</returns>
        IPublishSubject<T> CreatePublishSubject<T>();

        /// <summary>
        /// Async subject oluşturur
        /// </summary>
        /// <typeparam name="T">Veri tipi</typeparam>
        /// <returns>Async subject</returns>
        IAsyncSubject<T> CreateAsyncSubject<T>();
    }
}