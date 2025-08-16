using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MDI.Patterns.Pipeline
{
    /// <summary>
    /// Pipeline execution stratejisi
    /// </summary>
    public enum PipelineExecutionStrategy
    {
        /// <summary>
        /// Sıralı çalıştırma
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Paralel çalıştırma
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Koşullu çalıştırma
        /// </summary>
        Conditional,
        
        /// <summary>
        /// Hibrit (hem sıralı hem paralel)
        /// </summary>
        Hybrid
    }

    /// <summary>
    /// Pipeline execution sonucu
    /// </summary>
    public class PipelineExecutionResult
    {
        /// <summary>
        /// Execution başarılı mı
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Sonuç verisi
        /// </summary>
        public object Result { get; set; }
        
        /// <summary>
        /// Hata bilgisi
        /// </summary>
        public Exception Exception { get; set; }
        
        /// <summary>
        /// Execution süresi
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
        
        /// <summary>
        /// Çalıştırılan step sayısı
        /// </summary>
        public int ExecutedSteps { get; set; }
        
        /// <summary>
        /// Atlanan step sayısı
        /// </summary>
        public int SkippedSteps { get; set; }
        
        /// <summary>
        /// Başarısız step sayısı
        /// </summary>
        public int FailedSteps { get; set; }
        
        /// <summary>
        /// Step execution detayları
        /// </summary>
        public List<PipelineStepExecutionResult> StepResults { get; set; }
        
        /// <summary>
        /// Pipeline context
        /// </summary>
        public PipelineContext Context { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public PipelineExecutionResult()
        {
            StepResults = new List<PipelineStepExecutionResult>();
        }
        
        /// <summary>
        /// Başarılı sonuç oluştur
        /// </summary>
        /// <param name="result">Sonuç verisi</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Başarılı sonuç</returns>
        public static PipelineExecutionResult Success(object result, PipelineContext context)
        {
            return new PipelineExecutionResult
            {
                IsSuccess = true,
                Result = result,
                Context = context,
                ExecutionTime = DateTime.UtcNow - context.StartTime
            };
        }
        
        /// <summary>
        /// Başarısız sonuç oluştur
        /// </summary>
        /// <param name="exception">Hata</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Başarısız sonuç</returns>
        public static PipelineExecutionResult Failure(Exception exception, PipelineContext context)
        {
            return new PipelineExecutionResult
            {
                IsSuccess = false,
                Exception = exception,
                Context = context,
                ExecutionTime = DateTime.UtcNow - context.StartTime
            };
        }
    }

    /// <summary>
    /// Pipeline step execution sonucu
    /// </summary>
    public class PipelineStepExecutionResult
    {
        /// <summary>
        /// Step adı
        /// </summary>
        public string StepName { get; set; }
        
        /// <summary>
        /// Step sonucu
        /// </summary>
        public PipelineStepResult Result { get; set; }
        
        /// <summary>
        /// Execution süresi
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
        
        /// <summary>
        /// Hata bilgisi
        /// </summary>
        public Exception Exception { get; set; }
        
        /// <summary>
        /// Retry sayısı
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Step index
        /// </summary>
        public int StepIndex { get; set; }
        
        /// <summary>
        /// Input data
        /// </summary>
        public object Input { get; set; }
        
        /// <summary>
        /// Output data
        /// </summary>
        public object Output { get; set; }
    }

    /// <summary>
    /// Pipeline temel arayüzü
    /// </summary>
    public interface IPipeline
    {
        /// <summary>
        /// Pipeline adı
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Pipeline açıklaması
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Pipeline ID
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Execution stratejisi
        /// </summary>
        PipelineExecutionStrategy ExecutionStrategy { get; set; }
        
        /// <summary>
        /// Pipeline step'leri
        /// </summary>
        IReadOnlyList<IPipelineStep> Steps { get; }
        
        /// <summary>
        /// Pipeline çalışıyor mu
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// Pipeline iptal edildi mi
        /// </summary>
        bool IsCancelled { get; }
        
        /// <summary>
        /// Pipeline tamamlandı mı
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// Maksimum paralel step sayısı
        /// </summary>
        int MaxParallelSteps { get; set; }
        
        /// <summary>
        /// Global timeout (milliseconds)
        /// </summary>
        int TimeoutMs { get; set; }
        
        /// <summary>
        /// Step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <returns>Pipeline (fluent API)</returns>
        IPipeline AddStep(IPipelineStep step);
        
        /// <summary>
        /// Step ekle (generic)
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <returns>Pipeline (fluent API)</returns>
        IPipeline AddStep<T>() where T : class, IPipelineStep, new();
        
        /// <summary>
        /// Step'leri temizle
        /// </summary>
        /// <returns>Pipeline (fluent API)</returns>
        IPipeline ClearSteps();
        
        /// <summary>
        /// Pipeline'ı çalıştır
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution sonucu</returns>
        Task<PipelineExecutionResult> ExecuteAsync(object input = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Pipeline'ı context ile çalıştır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Execution sonucu</returns>
        Task<PipelineExecutionResult> ExecuteAsync(PipelineContext context);
        
        /// <summary>
        /// Pipeline'ı iptal et
        /// </summary>
        void Cancel();
        
        /// <summary>
        /// Pipeline'ı validate et
        /// </summary>
        /// <returns>Validation başarılı mı</returns>
        Task<bool> ValidateAsync();
        
        /// <summary>
        /// Pipeline klonla
        /// </summary>
        /// <returns>Klonlanmış pipeline</returns>
        IPipeline Clone();
    }

    /// <summary>
    /// Generic pipeline arayüzü
    /// </summary>
    /// <typeparam name="TInput">Input tipi</typeparam>
    /// <typeparam name="TOutput">Output tipi</typeparam>
    public interface IPipeline<TInput, TOutput> : IPipeline
    {
        /// <summary>
        /// Pipeline'ı çalıştır (typed)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution sonucu</returns>
        Task<PipelineExecutionResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic pipeline execution sonucu
    /// </summary>
    /// <typeparam name="T">Output tipi</typeparam>
    public class PipelineExecutionResult<T> : PipelineExecutionResult
    {
        /// <summary>
        /// Typed sonuç
        /// </summary>
        public new T Result
        {
            get => (T)base.Result;
            set => base.Result = value;
        }
        
        /// <summary>
        /// Başarılı sonuç oluştur
        /// </summary>
        /// <param name="result">Sonuç verisi</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Başarılı sonuç</returns>
        public static PipelineExecutionResult<T> Success(T result, PipelineContext context)
        {
            return new PipelineExecutionResult<T>
            {
                IsSuccess = true,
                Result = result,
                Context = context,
                ExecutionTime = DateTime.UtcNow - context.StartTime
            };
        }
        
        /// <summary>
        /// Başarısız sonuç oluştur
        /// </summary>
        /// <param name="exception">Hata</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Başarısız sonuç</returns>
        public static new PipelineExecutionResult<T> Failure(Exception exception, PipelineContext context)
        {
            return new PipelineExecutionResult<T>
            {
                IsSuccess = false,
                Exception = exception,
                Context = context,
                ExecutionTime = DateTime.UtcNow - context.StartTime
            };
        }
    }

    /// <summary>
    /// Pipeline builder arayüzü
    /// </summary>
    public interface IPipelineBuilder
    {
        /// <summary>
        /// Pipeline oluştur
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder CreatePipeline(string name, string description = null);
        
        /// <summary>
        /// Step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder AddStep(IPipelineStep step);
        
        /// <summary>
        /// Step ekle (factory ile)
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder AddStep<T>() where T : class, IPipelineStep;
        
        /// <summary>
        /// Parallel step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder AddParallelStep(IPipelineStep step);
        
        /// <summary>
        /// Conditional step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <param name="condition">Condition function</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder AddConditionalStep(IPipelineStep step, System.Func<PipelineContext, bool> condition);
        
        /// <summary>
        /// Async conditional step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <param name="condition">Async condition function</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder AddConditionalStep(IPipelineStep step, System.Func<PipelineContext, System.Threading.Tasks.Task<bool>> condition);
        
        /// <summary>
        /// Execution stratejisi ayarla
        /// </summary>
        /// <param name="strategy">Execution stratejisi</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder WithExecutionStrategy(PipelineExecutionStrategy strategy);
        
        /// <summary>
        /// Timeout ayarla
        /// </summary>
        /// <param name="timeoutMs">Timeout (milliseconds)</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder WithTimeout(int timeoutMs);
        
        /// <summary>
        /// Timeout ayarla (TimeSpan)
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder WithTimeout(TimeSpan timeout);
        
        /// <summary>
        /// Maksimum paralel step sayısı ayarla
        /// </summary>
        /// <param name="maxParallelSteps">Maksimum paralel step sayısı</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder WithMaxParallelSteps(int maxParallelSteps);
        
        /// <summary>
        /// Metadata ekle
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Pipeline builder</returns>
        IPipelineBuilder WithMetadata(string key, object value);
        
        /// <summary>
        /// Pipeline'ı build et
        /// </summary>
        /// <returns>Pipeline</returns>
        IPipeline Build();
        
        /// <summary>
        /// Generic pipeline'ı build et
        /// </summary>
        /// <typeparam name="TInput">Input tipi</typeparam>
        /// <typeparam name="TOutput">Output tipi</typeparam>
        /// <returns>Generic pipeline</returns>
        IPipeline<TInput, TOutput> Build<TInput, TOutput>();
    }

    /// <summary>
    /// Pipeline events arayüzü
    /// </summary>
    public interface IPipelineEvents
    {
        /// <summary>
        /// Pipeline başladığında tetiklenir
        /// </summary>
        event Action<IPipeline, PipelineContext> PipelineStarted;
        
        /// <summary>
        /// Pipeline tamamlandığında tetiklenir
        /// </summary>
        event Action<IPipeline, PipelineExecutionResult> PipelineCompleted;
        
        /// <summary>
        /// Pipeline hata verdiğinde tetiklenir
        /// </summary>
        event Action<IPipeline, Exception> PipelineFailed;
        
        /// <summary>
        /// Pipeline iptal edildiğinde tetiklenir
        /// </summary>
        event Action<IPipeline> PipelineCancelled;
        
        /// <summary>
        /// Step başladığında tetiklenir
        /// </summary>
        event Action<IPipelineStep, PipelineContext> StepStarted;
        
        /// <summary>
        /// Step tamamlandığında tetiklenir
        /// </summary>
        event Action<IPipelineStep, PipelineStepExecutionResult> StepCompleted;
        
        /// <summary>
        /// Step hata verdiğinde tetiklenir
        /// </summary>
        event Action<IPipelineStep, Exception> StepFailed;
        
        /// <summary>
        /// Step atlandığında tetiklenir
        /// </summary>
        event Action<IPipelineStep, PipelineContext> StepSkipped;
    }
}