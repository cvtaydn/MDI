using System;
using System.Threading;
using System.Threading.Tasks;

namespace MDI.Patterns.Pipeline
{
    /// <summary>
    /// Pipeline step sonuç durumu
    /// </summary>
    public enum PipelineStepResult
    {
        /// <summary>
        /// Başarılı, bir sonraki step'e geç
        /// </summary>
        Success,
        
        /// <summary>
        /// Başarısız, pipeline'ı durdur
        /// </summary>
        Failed,
        
        /// <summary>
        /// Atla, bir sonraki step'e geç ama bu step'i işleme
        /// </summary>
        Skip,
        
        /// <summary>
        /// Yeniden dene
        /// </summary>
        Retry,
        
        /// <summary>
        /// Pipeline'ı durdur (hata değil)
        /// </summary>
        Stop
    }

    /// <summary>
    /// Pipeline step execution context
    /// </summary>
    public class PipelineContext
    {
        /// <summary>
        /// Context verileri
        /// </summary>
        public object Data { get; set; }
        
        /// <summary>
        /// Pipeline metadata
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; }
        
        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
        
        /// <summary>
        /// Step execution başlangıç zamanı
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Mevcut step index
        /// </summary>
        public int CurrentStepIndex { get; set; }
        
        /// <summary>
        /// Toplam step sayısı
        /// </summary>
        public int TotalSteps { get; set; }
        
        /// <summary>
        /// Hata bilgisi
        /// </summary>
        public Exception LastException { get; set; }
        
        /// <summary>
        /// Retry sayısı
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public PipelineContext()
        {
            Metadata = new System.Collections.Generic.Dictionary<string, object>();
            StartTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Constructor with data
        /// </summary>
        /// <param name="data">Initial data</param>
        public PipelineContext(object data) : this()
        {
            Data = data;
        }
        
        /// <summary>
        /// Metadata'ya değer ekle
        /// </summary>
        /// <param name="key">Anahtar</param>
        /// <param name="value">Değer</param>
        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
        }
        
        /// <summary>
        /// Metadata'dan değer al
        /// </summary>
        /// <typeparam name="T">Değer tipi</typeparam>
        /// <param name="key">Anahtar</param>
        /// <returns>Değer</returns>
        public T GetMetadata<T>(string key)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }
        
        /// <summary>
        /// Context data'yı ayarla
        /// </summary>
        /// <param name="data">Ayarlanacak data</param>
        public void SetData(object data)
        {
            Data = data;
        }
        
        /// <summary>
        /// Context data'yı al
        /// </summary>
        /// <typeparam name="T">Data tipi</typeparam>
        /// <returns>Data</returns>
        public T GetData<T>()
        {
            if (Data is T typedData)
            {
                return typedData;
            }
            return default(T);
        }
        
        /// <summary>
        /// Hata ayarla
        /// </summary>
        /// <param name="error">Hata mesajı</param>
        public void SetError(string error)
        {
            LastException = new Exception(error);
        }
        
        /// <summary>
        /// Hata var mı kontrol et
        /// </summary>
        public bool HasError => LastException != null;
        
        /// <summary>
        /// Hata mesajını al
        /// </summary>
        public string Error => LastException?.Message;
        
        /// <summary>
        /// Context'i klonla
        /// </summary>
        /// <returns>Klonlanmış context</returns>
        public PipelineContext Clone()
        {
            var clone = new PipelineContext(Data)
            {
                CancellationToken = CancellationToken,
                StartTime = StartTime,
                CurrentStepIndex = CurrentStepIndex,
                TotalSteps = TotalSteps,
                LastException = LastException,
                RetryCount = RetryCount
            };
            
            foreach (var kvp in Metadata)
            {
                clone.Metadata[kvp.Key] = kvp.Value;
            }
            
            return clone;
        }
    }

    /// <summary>
    /// Pipeline step temel arayüzü
    /// </summary>
    public interface IPipelineStep
    {
        /// <summary>
        /// Step adı
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Step açıklaması
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Step önceliği
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Step çalıştırılabilir mi
        /// </summary>
        bool CanExecute { get; }
        
        /// <summary>
        /// Maksimum retry sayısı
        /// </summary>
        int MaxRetries { get; }
        
        /// <summary>
        /// Timeout süresi (milliseconds)
        /// </summary>
        int TimeoutMs { get; }
        
        /// <summary>
        /// Step'i çalıştır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step sonucu</returns>
        Task<PipelineStepResult> ExecuteAsync(PipelineContext context);
        
        /// <summary>
        /// Step'i validate et
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Validation başarılı mı</returns>
        Task<bool> ValidateAsync(PipelineContext context);
        
        /// <summary>
        /// Step başlamadan önce çağrılır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        Task OnBeforeExecuteAsync(PipelineContext context);
        
        /// <summary>
        /// Step bittikten sonra çağrılır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <param name="result">Step sonucu</param>
        Task OnAfterExecuteAsync(PipelineContext context, PipelineStepResult result);
        
        /// <summary>
        /// Step hata verdiğinde çağrılır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <param name="exception">Hata</param>
        /// <returns>Hata handle edildi mi</returns>
        Task<bool> OnErrorAsync(PipelineContext context, Exception exception);
    }

    /// <summary>
    /// Generic pipeline step arayüzü
    /// </summary>
    /// <typeparam name="TInput">Input tipi</typeparam>
    /// <typeparam name="TOutput">Output tipi</typeparam>
    public interface IPipelineStep<TInput, TOutput> : IPipelineStep
    {
        /// <summary>
        /// Step'i çalıştır (typed)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Output data ve step sonucu</returns>
        Task<(TOutput Output, PipelineStepResult Result)> ExecuteAsync(TInput input, PipelineContext context);
    }

    /// <summary>
    /// Conditional pipeline step arayüzü
    /// </summary>
    public interface IConditionalPipelineStep : IPipelineStep
    {
        /// <summary>
        /// Step çalıştırılacak mı kontrol et
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step çalıştırılsın mı</returns>
        Task<bool> ShouldExecuteAsync(PipelineContext context);
    }

    /// <summary>
    /// Parallel pipeline step arayüzü
    /// </summary>
    public interface IParallelPipelineStep : IPipelineStep
    {
        /// <summary>
        /// Paralel çalıştırılabilir mi
        /// </summary>
        bool CanRunInParallel { get; }
        
        /// <summary>
        /// Paralel execution için dependency'ler
        /// </summary>
        string[] Dependencies { get; }
    }

    /// <summary>
    /// Pipeline step factory arayüzü
    /// </summary>
    public interface IPipelineStepFactory
    {
        /// <summary>
        /// Step oluştur
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <returns>Step instance</returns>
        T CreateStep<T>() where T : class, IPipelineStep;
        
        /// <summary>
        /// Step oluştur
        /// </summary>
        /// <param name="stepType">Step tipi</param>
        /// <returns>Step instance</returns>
        IPipelineStep CreateStep(Type stepType);
        
        /// <summary>
        /// Step'i register et
        /// </summary>
        /// <typeparam name="TInterface">Interface tipi</typeparam>
        /// <typeparam name="TImplementation">Implementation tipi</typeparam>
        void RegisterStep<TInterface, TImplementation>()
            where TInterface : class, IPipelineStep
            where TImplementation : class, TInterface;
    }
}