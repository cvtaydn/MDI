using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Pipeline
{
    /// <summary>
    /// Temel pipeline step implementasyonu
    /// </summary>
    public abstract class BasePipelineStep : IPipelineStep
    {
        /// <summary>
        /// Step adı
        /// </summary>
        public virtual string Name { get; protected set; }
        
        /// <summary>
        /// Step açıklaması
        /// </summary>
        public virtual string Description { get; protected set; }
        
        /// <summary>
        /// Step önceliği
        /// </summary>
        public virtual int Priority { get; protected set; }
        
        /// <summary>
        /// Step çalıştırılabilir mi
        /// </summary>
        public virtual bool CanExecute { get; protected set; } = true;
        
        /// <summary>
        /// Maksimum retry sayısı
        /// </summary>
        public virtual int MaxRetries { get; protected set; } = 3;
        
        /// <summary>
        /// Timeout süresi (milliseconds)
        /// </summary>
        public virtual int TimeoutMs { get; protected set; } = 30000; // 30 saniye
        
        /// <summary>
        /// Step çalışıyor mu
        /// </summary>
        public bool IsExecuting { get; private set; }
        
        /// <summary>
        /// Step tamamlandı mı
        /// </summary>
        public bool IsCompleted { get; private set; }
        
        /// <summary>
        /// Son execution zamanı
        /// </summary>
        public DateTime LastExecutionTime { get; private set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Step adı</param>
        /// <param name="description">Step açıklaması</param>
        /// <param name="priority">Step önceliği</param>
        protected BasePipelineStep(string name, string description = null, int priority = 0)
        {
            Name = name ?? GetType().Name;
            Description = description ?? Name;
            Priority = priority;
        }
        
        /// <summary>
        /// Step'i çalıştır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step sonucu</returns>
        public async Task<PipelineStepResult> ExecuteAsync(PipelineContext context)
        {
            if (!CanExecute)
            {
                Debug.LogWarning($"[{Name}] Step cannot be executed");
                return PipelineStepResult.Skip;
            }
            
            if (IsExecuting)
            {
                Debug.LogWarning($"[{Name}] Step is already executing");
                return PipelineStepResult.Failed;
            }
            
            IsExecuting = true;
            LastExecutionTime = DateTime.UtcNow;
            
            try
            {
                // Validation
                if (!await ValidateAsync(context))
                {
                    Debug.LogError($"[{Name}] Step validation failed");
                    return PipelineStepResult.Failed;
                }
                
                // Before execute
                await OnBeforeExecuteAsync(context);
                
                // Execute with timeout
                var result = await ExecuteWithTimeoutAsync(context);
                
                // After execute
                await OnAfterExecuteAsync(context, result);
                
                IsCompleted = result == PipelineStepResult.Success;
                
                Debug.Log($"[{Name}] Step executed with result: {result}");
                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[{Name}] Step execution was cancelled");
                return PipelineStepResult.Stop;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Name}] Step execution failed: {ex.Message}");
                context.LastException = ex;
                
                // Error handling
                var handled = await OnErrorAsync(context, ex);
                if (handled)
                {
                    return PipelineStepResult.Retry;
                }
                
                return PipelineStepResult.Failed;
            }
            finally
            {
                IsExecuting = false;
            }
        }
        
        /// <summary>
        /// Timeout ile step execution
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step sonucu</returns>
        private async Task<PipelineStepResult> ExecuteWithTimeoutAsync(PipelineContext context)
        {
            if (TimeoutMs <= 0)
            {
                return await OnExecuteAsync(context);
            }
            
            using (var timeoutCts = new System.Threading.CancellationTokenSource(TimeoutMs))
            using (var combinedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, timeoutCts.Token))
            {
                var originalToken = context.CancellationToken;
                context.CancellationToken = combinedCts.Token;
                
                try
                {
                    return await OnExecuteAsync(context);
                }
                finally
                {
                    context.CancellationToken = originalToken;
                }
            }
        }
        
        /// <summary>
        /// Step execution implementasyonu (abstract)
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step sonucu</returns>
        protected abstract Task<PipelineStepResult> OnExecuteAsync(PipelineContext context);
        
        /// <summary>
        /// Step'i validate et
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Validation başarılı mı</returns>
        public virtual async Task<bool> ValidateAsync(PipelineContext context)
        {
            await Task.CompletedTask;
            return true;
        }
        
        /// <summary>
        /// Step başlamadan önce çağrılır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        public virtual async Task OnBeforeExecuteAsync(PipelineContext context)
        {
            await Task.CompletedTask;
            Debug.Log($"[{Name}] Step starting...");
        }
        
        /// <summary>
        /// Step bittikten sonra çağrılır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <param name="result">Step sonucu</param>
        public virtual async Task OnAfterExecuteAsync(PipelineContext context, PipelineStepResult result)
        {
            await Task.CompletedTask;
            Debug.Log($"[{Name}] Step completed with result: {result}");
        }
        
        /// <summary>
        /// Step hata verdiğinde çağrılır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <param name="exception">Hata</param>
        /// <returns>Hata handle edildi mi</returns>
        public virtual async Task<bool> OnErrorAsync(PipelineContext context, Exception exception)
        {
            await Task.CompletedTask;
            Debug.LogError($"[{Name}] Step error: {exception.Message}");
            return false; // Default: hata handle edilmedi
        }
        
        /// <summary>
        /// Step'i reset et
        /// </summary>
        public virtual void Reset()
        {
            IsExecuting = false;
            IsCompleted = false;
            LastExecutionTime = default;
        }
        
        /// <summary>
        /// Step bilgilerini string olarak döndür
        /// </summary>
        /// <returns>Step bilgileri</returns>
        public override string ToString()
        {
            return $"{Name} (Priority: {Priority}, CanExecute: {CanExecute}, MaxRetries: {MaxRetries})";
        }
    }

    /// <summary>
    /// Generic temel pipeline step implementasyonu
    /// </summary>
    /// <typeparam name="TInput">Input tipi</typeparam>
    /// <typeparam name="TOutput">Output tipi</typeparam>
    public abstract class BasePipelineStep<TInput, TOutput> : BasePipelineStep, IPipelineStep<TInput, TOutput>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Step adı</param>
        /// <param name="description">Step açıklaması</param>
        /// <param name="priority">Step önceliği</param>
        protected BasePipelineStep(string name, string description = null, int priority = 0)
            : base(name, description, priority)
        {
        }
        
        /// <summary>
        /// Step'i çalıştır (typed)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Output data ve step sonucu</returns>
        public async Task<(TOutput Output, PipelineStepResult Result)> ExecuteAsync(TInput input, PipelineContext context)
        {
            // Context'e input'u kaydet
            context.Data = input;
            
            var result = await ExecuteAsync(context);
            
            // Output'u context'ten al
            var output = context.Data is TOutput typedOutput ? typedOutput : default(TOutput);
            
            return (output, result);
        }
        
        /// <summary>
        /// Base execution implementasyonu
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step sonucu</returns>
        protected override async Task<PipelineStepResult> OnExecuteAsync(PipelineContext context)
        {
            var input = context.Data is TInput typedInput ? typedInput : default(TInput);
            
            var (output, result) = await OnExecuteAsync(input, context);
            
            // Output'u context'e kaydet
            context.Data = output;
            
            return result;
        }
        
        /// <summary>
        /// Typed step execution implementasyonu (abstract)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="context">Pipeline context</param>
        /// <returns>Output data ve step sonucu</returns>
        protected abstract Task<(TOutput Output, PipelineStepResult Result)> OnExecuteAsync(TInput input, PipelineContext context);
    }

    /// <summary>
    /// Conditional pipeline step temel implementasyonu
    /// </summary>
    public abstract class ConditionalPipelineStep : BasePipelineStep, IConditionalPipelineStep
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Step adı</param>
        /// <param name="description">Step açıklaması</param>
        /// <param name="priority">Step önceliği</param>
        protected ConditionalPipelineStep(string name, string description = null, int priority = 0)
            : base(name, description, priority)
        {
        }
        
        /// <summary>
        /// Step çalıştırılacak mı kontrol et (abstract)
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Step çalıştırılsın mı</returns>
        public abstract Task<bool> ShouldExecuteAsync(PipelineContext context);
    }

    /// <summary>
    /// Parallel pipeline step temel implementasyonu
    /// </summary>
    public abstract class ParallelPipelineStep : BasePipelineStep, IParallelPipelineStep
    {
        /// <summary>
        /// Paralel çalıştırılabilir mi
        /// </summary>
        public virtual bool CanRunInParallel { get; protected set; } = true;
        
        /// <summary>
        /// Paralel execution için dependency'ler
        /// </summary>
        public virtual string[] Dependencies { get; protected set; } = new string[0];
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Step adı</param>
        /// <param name="description">Step açıklaması</param>
        /// <param name="priority">Step önceliği</param>
        /// <param name="dependencies">Dependency'ler</param>
        protected ParallelPipelineStep(string name, string description = null, int priority = 0, params string[] dependencies)
            : base(name, description, priority)
        {
            Dependencies = dependencies ?? new string[0];
        }
    }
}