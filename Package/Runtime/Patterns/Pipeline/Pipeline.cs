using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Pipeline
{
    /// <summary>
    /// Pipeline implementasyonu
    /// </summary>
    public class Pipeline : IPipeline, IPipelineEvents
    {
        private readonly List<IPipelineStep> _steps;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockObject = new object();
        
        /// <summary>
        /// Pipeline adı
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Pipeline açıklaması
        /// </summary>
        public string Description { get; private set; }
        
        /// <summary>
        /// Pipeline ID
        /// </summary>
        public string Id { get; private set; }
        
        /// <summary>
        /// Execution stratejisi
        /// </summary>
        public PipelineExecutionStrategy ExecutionStrategy { get; set; }
        
        /// <summary>
        /// Pipeline step'leri
        /// </summary>
        public IReadOnlyList<IPipelineStep> Steps => _steps.AsReadOnly();
        
        /// <summary>
        /// Pipeline çalışıyor mu
        /// </summary>
        public bool IsRunning { get; private set; }
        
        /// <summary>
        /// Pipeline iptal edildi mi
        /// </summary>
        public bool IsCancelled { get; private set; }
        
        /// <summary>
        /// Pipeline tamamlandı mı
        /// </summary>
        public bool IsCompleted { get; private set; }
        
        /// <summary>
        /// Maksimum paralel step sayısı
        /// </summary>
        public int MaxParallelSteps { get; set; } = Environment.ProcessorCount;
        
        /// <summary>
        /// Global timeout (milliseconds)
        /// </summary>
        public int TimeoutMs { get; set; } = 300000; // 5 dakika
        
        /// <summary>
        /// Pipeline events
        /// </summary>
        public event Action<IPipeline, PipelineContext> PipelineStarted;
        public event Action<IPipeline, PipelineExecutionResult> PipelineCompleted;
        public event Action<IPipeline, Exception> PipelineFailed;
        public event Action<IPipeline> PipelineCancelled;
        public event Action<IPipelineStep, PipelineContext> StepStarted;
        public event Action<IPipelineStep, PipelineStepExecutionResult> StepCompleted;
        public event Action<IPipelineStep, Exception> StepFailed;
        public event Action<IPipelineStep, PipelineContext> StepSkipped;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        public Pipeline(string name, string description = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? name;
            Id = Guid.NewGuid().ToString();
            ExecutionStrategy = PipelineExecutionStrategy.Sequential;
            _steps = new List<IPipelineStep>();
        }
        
        /// <summary>
        /// Step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <returns>Pipeline (fluent API)</returns>
        public IPipeline AddStep(IPipelineStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
                
            if (IsRunning)
                throw new InvalidOperationException("Cannot add steps while pipeline is running");
            
            lock (_lockObject)
            {
                _steps.Add(step);
            }
            
            Debug.Log($"[{Name}] Added step: {step.Name}");
            return this;
        }
        
        /// <summary>
        /// Step ekle (generic)
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <returns>Pipeline (fluent API)</returns>
        public IPipeline AddStep<T>() where T : class, IPipelineStep, new()
        {
            return AddStep(new T());
        }
        
        /// <summary>
        /// Step'leri temizle
        /// </summary>
        /// <returns>Pipeline (fluent API)</returns>
        public IPipeline ClearSteps()
        {
            if (IsRunning)
                throw new InvalidOperationException("Cannot clear steps while pipeline is running");
            
            lock (_lockObject)
            {
                _steps.Clear();
            }
            
            Debug.Log($"[{Name}] Cleared all steps");
            return this;
        }
        
        /// <summary>
        /// Pipeline'ı çalıştır
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution sonucu</returns>
        public async Task<PipelineExecutionResult> ExecuteAsync(object input = null, CancellationToken cancellationToken = default)
        {
            var context = new PipelineContext(input)
            {
                CancellationToken = cancellationToken,
                TotalSteps = _steps.Count
            };
            
            return await ExecuteAsync(context);
        }
        
        /// <summary>
        /// Pipeline'ı context ile çalıştır
        /// </summary>
        /// <param name="context">Pipeline context</param>
        /// <returns>Execution sonucu</returns>
        public async Task<PipelineExecutionResult> ExecuteAsync(PipelineContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
                
            if (IsRunning)
                throw new InvalidOperationException("Pipeline is already running");
            
            if (_steps.Count == 0)
            {
                Debug.LogWarning($"[{Name}] No steps to execute");
                return PipelineExecutionResult.Success(null, context);
            }
            
            // Pipeline state'ini ayarla
            SetPipelineState(true, false, false);
            
            // Cancellation token setup
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            context.CancellationToken = _cancellationTokenSource.Token;
            
            var result = new PipelineExecutionResult
            {
                Context = context
            };
            
            try
            {
                // Pipeline validation
                if (!await ValidateAsync())
                {
                    throw new InvalidOperationException("Pipeline validation failed");
                }
                
                // Pipeline started event
                PipelineStarted?.Invoke(this, context);
                Debug.Log($"[{Name}] Pipeline execution started with {_steps.Count} steps");
                
                // Global timeout setup
                if (TimeoutMs > 0)
                {
                    _cancellationTokenSource.CancelAfter(TimeoutMs);
                }
                
                // Execute steps based on strategy
                switch (ExecutionStrategy)
                {
                    case PipelineExecutionStrategy.Sequential:
                        await ExecuteSequentialAsync(context, result);
                        break;
                        
                    case PipelineExecutionStrategy.Parallel:
                        await ExecuteParallelAsync(context, result);
                        break;
                        
                    case PipelineExecutionStrategy.Conditional:
                        await ExecuteConditionalAsync(context, result);
                        break;
                        
                    case PipelineExecutionStrategy.Hybrid:
                        await ExecuteHybridAsync(context, result);
                        break;
                        
                    default:
                        throw new NotSupportedException($"Execution strategy {ExecutionStrategy} is not supported");
                }
                
                result.IsSuccess = true;
                result.ExecutionTime = DateTime.UtcNow - context.StartTime;
                
                Debug.Log($"[{Name}] Pipeline execution completed successfully in {result.ExecutionTime.TotalMilliseconds:F2}ms");
                
                // Pipeline completed event
                PipelineCompleted?.Invoke(this, result);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                IsCancelled = true;
                result.IsSuccess = false;
                result.ExecutionTime = DateTime.UtcNow - context.StartTime;
                
                Debug.LogWarning($"[{Name}] Pipeline execution was cancelled");
                
                // Pipeline cancelled event
                PipelineCancelled?.Invoke(this);
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Exception = ex;
                result.ExecutionTime = DateTime.UtcNow - context.StartTime;
                
                Debug.LogError($"[{Name}] Pipeline execution failed: {ex.Message}");
                
                // Pipeline failed event
                PipelineFailed?.Invoke(this, ex);
                
                return result;
            }
            finally
            {
                SetPipelineState(false, IsCancelled, !IsCancelled && result.IsSuccess);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Sequential execution
        /// </summary>
        private async Task ExecuteSequentialAsync(PipelineContext context, PipelineExecutionResult result)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                context.CurrentStepIndex = i;
                var step = _steps[i];
                
                var stepResult = await ExecuteStepAsync(step, context);
                result.StepResults.Add(stepResult);
                
                UpdateStepCounters(result, stepResult.Result);
                
                // Step sonucuna göre akışı kontrol et
                if (stepResult.Result == PipelineStepResult.Failed)
                {
                    throw new InvalidOperationException($"Step '{step.Name}' failed");
                }
                
                if (stepResult.Result == PipelineStepResult.Stop)
                {
                    Debug.Log($"[{Name}] Pipeline stopped at step '{step.Name}'");
                    break;
                }
                
                context.CancellationToken.ThrowIfCancellationRequested();
            }
        }
        
        /// <summary>
        /// Parallel execution
        /// </summary>
        private async Task ExecuteParallelAsync(PipelineContext context, PipelineExecutionResult result)
        {
            var semaphore = new SemaphoreSlim(MaxParallelSteps, MaxParallelSteps);
            var tasks = new List<Task<PipelineStepExecutionResult>>();
            
            for (int i = 0; i < _steps.Count; i++)
            {
                var stepIndex = i;
                var step = _steps[i];
                var stepContext = context.Clone();
                stepContext.CurrentStepIndex = stepIndex;
                
                var task = ExecuteStepWithSemaphoreAsync(step, stepContext, semaphore);
                tasks.Add(task);
            }
            
            var stepResults = await Task.WhenAll(tasks);
            result.StepResults.AddRange(stepResults);
            
            foreach (var stepResult in stepResults)
            {
                UpdateStepCounters(result, stepResult.Result);
                
                if (stepResult.Result == PipelineStepResult.Failed)
                {
                    throw new InvalidOperationException($"Step '{stepResult.StepName}' failed");
                }
            }
        }
        
        /// <summary>
        /// Conditional execution
        /// </summary>
        private async Task ExecuteConditionalAsync(PipelineContext context, PipelineExecutionResult result)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                context.CurrentStepIndex = i;
                var step = _steps[i];
                
                // Conditional step kontrolü
                if (step is IConditionalPipelineStep conditionalStep)
                {
                    if (!await conditionalStep.ShouldExecuteAsync(context))
                    {
                        Debug.Log($"[{Name}] Skipping conditional step '{step.Name}'");
                        
                        var skippedResult = new PipelineStepExecutionResult
                        {
                            StepName = step.Name,
                            Result = PipelineStepResult.Skip,
                            StepIndex = i
                        };
                        
                        result.StepResults.Add(skippedResult);
                        UpdateStepCounters(result, PipelineStepResult.Skip);
                        
                        StepSkipped?.Invoke(step, context);
                        continue;
                    }
                }
                
                var stepResult = await ExecuteStepAsync(step, context);
                result.StepResults.Add(stepResult);
                
                UpdateStepCounters(result, stepResult.Result);
                
                if (stepResult.Result == PipelineStepResult.Failed)
                {
                    throw new InvalidOperationException($"Step '{step.Name}' failed");
                }
                
                if (stepResult.Result == PipelineStepResult.Stop)
                {
                    break;
                }
                
                context.CancellationToken.ThrowIfCancellationRequested();
            }
        }
        
        /// <summary>
        /// Hybrid execution (parallel + sequential)
        /// </summary>
        private async Task ExecuteHybridAsync(PipelineContext context, PipelineExecutionResult result)
        {
            var parallelSteps = _steps.OfType<IParallelPipelineStep>().ToList();
            var sequentialSteps = _steps.Except(parallelSteps.Cast<IPipelineStep>()).ToList();
            
            // Önce parallel step'leri çalıştır
            if (parallelSteps.Any())
            {
                var parallelContext = context.Clone();
                var parallelResult = new PipelineExecutionResult();
                
                await ExecuteParallelStepsAsync(parallelSteps.Cast<IPipelineStep>().ToList(), parallelContext, parallelResult);
                result.StepResults.AddRange(parallelResult.StepResults);
                
                foreach (var stepResult in parallelResult.StepResults)
                {
                    UpdateStepCounters(result, stepResult.Result);
                }
            }
            
            // Sonra sequential step'leri çalıştır
            if (sequentialSteps.Any())
            {
                var sequentialContext = context.Clone();
                var sequentialResult = new PipelineExecutionResult();
                
                await ExecuteSequentialStepsAsync(sequentialSteps, sequentialContext, sequentialResult);
                result.StepResults.AddRange(sequentialResult.StepResults);
                
                foreach (var stepResult in sequentialResult.StepResults)
                {
                    UpdateStepCounters(result, stepResult.Result);
                }
            }
        }
        
        /// <summary>
        /// Parallel step'leri çalıştır
        /// </summary>
        private async Task ExecuteParallelStepsAsync(List<IPipelineStep> steps, PipelineContext context, PipelineExecutionResult result)
        {
            var semaphore = new SemaphoreSlim(MaxParallelSteps, MaxParallelSteps);
            var tasks = new List<Task<PipelineStepExecutionResult>>();
            
            for (int i = 0; i < steps.Count; i++)
            {
                var stepIndex = i;
                var step = steps[i];
                var stepContext = context.Clone();
                stepContext.CurrentStepIndex = stepIndex;
                
                var task = ExecuteStepWithSemaphoreAsync(step, stepContext, semaphore);
                tasks.Add(task);
            }
            
            var stepResults = await Task.WhenAll(tasks);
            result.StepResults.AddRange(stepResults);
        }
        
        /// <summary>
        /// Sequential step'leri çalıştır
        /// </summary>
        private async Task ExecuteSequentialStepsAsync(List<IPipelineStep> steps, PipelineContext context, PipelineExecutionResult result)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                context.CurrentStepIndex = i;
                var step = steps[i];
                
                var stepResult = await ExecuteStepAsync(step, context);
                result.StepResults.Add(stepResult);
                
                if (stepResult.Result == PipelineStepResult.Failed)
                {
                    throw new InvalidOperationException($"Step '{step.Name}' failed");
                }
                
                if (stepResult.Result == PipelineStepResult.Stop)
                {
                    break;
                }
                
                context.CancellationToken.ThrowIfCancellationRequested();
            }
        }
        
        /// <summary>
        /// Semaphore ile step çalıştır
        /// </summary>
        private async Task<PipelineStepExecutionResult> ExecuteStepWithSemaphoreAsync(IPipelineStep step, PipelineContext context, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync(context.CancellationToken);
            
            try
            {
                return await ExecuteStepAsync(step, context);
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        /// <summary>
        /// Tek step çalıştır
        /// </summary>
        private async Task<PipelineStepExecutionResult> ExecuteStepAsync(IPipelineStep step, PipelineContext context)
        {
            var stepResult = new PipelineStepExecutionResult
            {
                StepName = step.Name,
                StepIndex = context.CurrentStepIndex,
                Input = context.Data
            };
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                StepStarted?.Invoke(step, context);
                
                // Retry logic
                var retryCount = 0;
                PipelineStepResult result;
                
                do
                {
                    context.RetryCount = retryCount;
                    result = await step.ExecuteAsync(context);
                    
                    if (result == PipelineStepResult.Retry && retryCount < step.MaxRetries)
                    {
                        retryCount++;
                        Debug.LogWarning($"[{Name}] Retrying step '{step.Name}' (Attempt {retryCount}/{step.MaxRetries})");
                        await Task.Delay(1000 * retryCount, context.CancellationToken); // Exponential backoff
                    }
                    else
                    {
                        break;
                    }
                } while (retryCount <= step.MaxRetries);
                
                stepResult.Result = result;
                stepResult.RetryCount = retryCount;
                stepResult.Output = context.Data;
                stepResult.ExecutionTime = DateTime.UtcNow - startTime;
                
                StepCompleted?.Invoke(step, stepResult);
                
                return stepResult;
            }
            catch (Exception ex)
            {
                stepResult.Exception = ex;
                stepResult.Result = PipelineStepResult.Failed;
                stepResult.ExecutionTime = DateTime.UtcNow - startTime;
                
                StepFailed?.Invoke(step, ex);
                
                return stepResult;
            }
        }
        
        /// <summary>
        /// Step counter'larını güncelle
        /// </summary>
        private void UpdateStepCounters(PipelineExecutionResult result, PipelineStepResult stepResult)
        {
            switch (stepResult)
            {
                case PipelineStepResult.Success:
                    result.ExecutedSteps++;
                    break;
                case PipelineStepResult.Skip:
                    result.SkippedSteps++;
                    break;
                case PipelineStepResult.Failed:
                    result.FailedSteps++;
                    break;
            }
        }
        
        /// <summary>
        /// Pipeline state'ini ayarla
        /// </summary>
        private void SetPipelineState(bool isRunning, bool isCancelled, bool isCompleted)
        {
            lock (_lockObject)
            {
                IsRunning = isRunning;
                IsCancelled = isCancelled;
                IsCompleted = isCompleted;
            }
        }
        
        /// <summary>
        /// Pipeline'ı iptal et
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            Debug.Log($"[{Name}] Pipeline cancellation requested");
        }
        
        /// <summary>
        /// Pipeline'ı validate et
        /// </summary>
        /// <returns>Validation başarılı mı</returns>
        public async Task<bool> ValidateAsync()
        {
            if (_steps.Count == 0)
            {
                Debug.LogWarning($"[{Name}] Pipeline has no steps");
                return false;
            }
            
            // Her step'i validate et
            var context = new PipelineContext();
            
            foreach (var step in _steps)
            {
                if (!await step.ValidateAsync(context))
                {
                    Debug.LogError($"[{Name}] Step '{step.Name}' validation failed");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Pipeline klonla
        /// </summary>
        /// <returns>Klonlanmış pipeline</returns>
        public IPipeline Clone()
        {
            var clone = new Pipeline(Name, Description)
            {
                ExecutionStrategy = ExecutionStrategy,
                MaxParallelSteps = MaxParallelSteps,
                TimeoutMs = TimeoutMs
            };
            
            foreach (var step in _steps)
            {
                clone.AddStep(step);
            }
            
            return clone;
        }
        
        /// <summary>
        /// Pipeline bilgilerini string olarak döndür
        /// </summary>
        /// <returns>Pipeline bilgileri</returns>
        public override string ToString()
        {
            return $"{Name} ({_steps.Count} steps, Strategy: {ExecutionStrategy}, Running: {IsRunning})";
        }
    }

    /// <summary>
    /// Generic pipeline implementasyonu
    /// </summary>
    /// <typeparam name="TInput">Input tipi</typeparam>
    /// <typeparam name="TOutput">Output tipi</typeparam>
    public class Pipeline<TInput, TOutput> : Pipeline, IPipeline<TInput, TOutput>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        public Pipeline(string name, string description = null) : base(name, description)
        {
        }
        
        /// <summary>
        /// Pipeline'ı çalıştır (typed)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution sonucu</returns>
        public async Task<PipelineExecutionResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var result = await ExecuteAsync((object)input, cancellationToken);
            
            var typedResult = new PipelineExecutionResult<TOutput>
            {
                IsSuccess = result.IsSuccess,
                Exception = result.Exception,
                ExecutionTime = result.ExecutionTime,
                ExecutedSteps = result.ExecutedSteps,
                SkippedSteps = result.SkippedSteps,
                FailedSteps = result.FailedSteps,
                Context = result.Context
            };
            
            typedResult.StepResults.AddRange(result.StepResults);
            
            if (result.IsSuccess && result.Result is TOutput typedOutput)
            {
                typedResult.Result = typedOutput;
            }
            
            return typedResult;
        }
    }
}