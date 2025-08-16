using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MDI.Patterns.Pipeline
{
    /// <summary>
    /// Pipeline builder implementasyonu
    /// </summary>
    public class PipelineBuilder : IPipelineBuilder
    {
        private readonly Pipeline _pipeline;
        private readonly List<IPipelineStep> _steps;
        private readonly Dictionary<string, object> _metadata;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        public PipelineBuilder(string name, string description = null)
        {
            _pipeline = new Pipeline(name, description);
            _steps = new List<IPipelineStep>();
            _metadata = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Pipeline oluşturur ve builder'ı döndürür (interface implementasyonu)
        /// </summary>
        public IPipelineBuilder CreatePipeline(string name, string description = null)
        {
            // Pipeline zaten constructor'da name ve description ile oluşturulmuş
            // Bu metod sadece builder'ı döndürür
            return this;
        }
        
        /// <summary>
        /// Step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddStep(IPipelineStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            
            _steps.Add(step);
            Debug.Log($"[PipelineBuilder] Added step: {step.Name}");
            return this;
        }
        
        /// <summary>
        /// Step ekle (generic - interface implementasyonu)
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddStep<T>() where T : class, IPipelineStep
        {
            return AddStep(Activator.CreateInstance<T>());
        }
        
        /// <summary>
        /// Step ekle (factory ile)
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <param name="factory">Step factory</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddStep<T>(Func<T> factory) where T : class, IPipelineStep
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            return AddStep(factory());
        }
        
        /// <summary>
        /// Step ekle (configuration ile)
        /// </summary>
        /// <typeparam name="T">Step tipi</typeparam>
        /// <param name="configure">Step configuration</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddStep<T>(Action<T> configure) where T : class, IPipelineStep, new()
        {
            var step = new T();
            configure?.Invoke(step);
            return AddStep(step);
        }
        
        /// <summary>
        /// Parallel step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddParallelStep(IPipelineStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            
            // Parallel step'i normal step olarak ekle, paralel çalışma Pipeline tarafından yönetilir
            _steps.Add(step);
            Debug.Log($"[PipelineBuilder] Added parallel step: {step.Name}");
            return this;
        }
        
        /// <summary>
        /// Conditional step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <param name="condition">Condition function</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddConditionalStep(IPipelineStep step, Func<PipelineContext, bool> condition)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            
            var conditionalStep = new ConditionalStepWrapper(step, condition);
            return AddStep(conditionalStep);
        }
        
        /// <summary>
        /// Async conditional step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <param name="condition">Async condition function</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddConditionalStep(IPipelineStep step, Func<PipelineContext, System.Threading.Tasks.Task<bool>> condition)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            
            var conditionalStep = new AsyncConditionalStepWrapper(step, condition);
            return AddStep(conditionalStep);
        }
        
        /// <summary>
        /// Parallel step ekle
        /// </summary>
        /// <param name="step">Pipeline step</param>
        /// <param name="dependencies">Dependencies</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddParallelStep(IPipelineStep step, params string[] dependencies)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            
            var parallelStep = new ParallelStepWrapper(step, dependencies);
            return AddStep(parallelStep);
        }
        
        /// <summary>
        /// Step'leri sırayla ekle
        /// </summary>
        /// <param name="steps">Step'ler</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddSteps(params IPipelineStep[] steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            
            foreach (var step in steps)
            {
                AddStep(step);
            }
            
            return this;
        }
        
        /// <summary>
        /// Step'leri sırayla ekle (collection)
        /// </summary>
        /// <param name="steps">Step collection</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder AddSteps(IEnumerable<IPipelineStep> steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            
            foreach (var step in steps)
            {
                AddStep(step);
            }
            
            return this;
        }
        
        /// <summary>
        /// Execution strategy ayarla
        /// </summary>
        /// <param name="strategy">Execution strategy</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder WithExecutionStrategy(PipelineExecutionStrategy strategy)
        {
            _pipeline.ExecutionStrategy = strategy;
            Debug.Log($"[PipelineBuilder] Set execution strategy: {strategy}");
            return this;
        }
        
        /// <summary>
        /// Maksimum paralel step sayısını ayarla
        /// </summary>
        /// <param name="maxParallelSteps">Maksimum paralel step sayısı</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder WithMaxParallelSteps(int maxParallelSteps)
        {
            if (maxParallelSteps <= 0)
                throw new ArgumentException("Max parallel steps must be greater than 0", nameof(maxParallelSteps));
            
            _pipeline.MaxParallelSteps = maxParallelSteps;
            Debug.Log($"[PipelineBuilder] Set max parallel steps: {maxParallelSteps}");
            return this;
        }
        
        /// <summary>
        /// Timeout ayarla
        /// </summary>
        /// <param name="timeoutMs">Timeout (milliseconds)</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder WithTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                throw new ArgumentException("Timeout must be greater than 0", nameof(timeoutMs));
            
            _pipeline.TimeoutMs = timeoutMs;
            Debug.Log($"[PipelineBuilder] Set timeout: {timeoutMs}ms");
            return this;
        }
        
        /// <summary>
        /// Timeout ayarla (TimeSpan)
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder WithTimeout(TimeSpan timeout)
        {
            return WithTimeout((int)timeout.TotalMilliseconds);
        }
        
        /// <summary>
        /// Metadata ekle
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder WithMetadata(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            _metadata[key] = value;
            Debug.Log($"[PipelineBuilder] Added metadata: {key} = {value}");
            return this;
        }
        
        /// <summary>
        /// Event handler ekle
        /// </summary>
        /// <param name="configure">Event configuration</param>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder WithEvents(Action<IPipelineEvents> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            
            configure(_pipeline);
            Debug.Log($"[PipelineBuilder] Configured events");
            return this;
        }
        
        /// <summary>
        /// Pipeline'ı build et
        /// </summary>
        /// <returns>Built pipeline</returns>
        public IPipeline Build()
        {
            // Step'leri pipeline'a ekle
            foreach (var step in _steps)
            {
                _pipeline.AddStep(step);
            }
            
            // Metadata'yı context'e ekle (eğer gerekirse)
            if (_metadata.Any())
            {
                Debug.Log($"[PipelineBuilder] Built pipeline with {_metadata.Count} metadata entries");
            }
            
            Debug.Log($"[PipelineBuilder] Built pipeline '{_pipeline.Name}' with {_steps.Count} steps");
            return _pipeline;
        }
        
        /// <summary>
        /// Generic pipeline build et
        /// </summary>
        /// <typeparam name="TInput">Input tipi</typeparam>
        /// <typeparam name="TOutput">Output tipi</typeparam>
        /// <returns>Built generic pipeline</returns>
        public IPipeline<TInput, TOutput> Build<TInput, TOutput>()
        {
            var genericPipeline = new Pipeline<TInput, TOutput>(_pipeline.Name, _pipeline.Description)
            {
                ExecutionStrategy = _pipeline.ExecutionStrategy,
                MaxParallelSteps = _pipeline.MaxParallelSteps,
                TimeoutMs = _pipeline.TimeoutMs
            };
            
            // Step'leri ekle
            foreach (var step in _steps)
            {
                genericPipeline.AddStep(step);
            }
            
            // Event'leri kopyala
            if (_pipeline is IPipelineEvents sourceEvents && genericPipeline is IPipelineEvents targetEvents)
            {
                // Event handler'ları kopyala (reflection ile veya manuel olarak)
                // Bu kısım implementation'a göre değişebilir
            }
            
            Debug.Log($"[PipelineBuilder] Built generic pipeline '{genericPipeline.Name}' with {_steps.Count} steps");
            return genericPipeline;
        }
        
        /// <summary>
        /// Builder'ı reset et
        /// </summary>
        /// <returns>Builder (fluent API)</returns>
        public IPipelineBuilder Reset()
        {
            _steps.Clear();
            _metadata.Clear();
            _pipeline.ClearSteps();
            _pipeline.ExecutionStrategy = PipelineExecutionStrategy.Sequential;
            _pipeline.MaxParallelSteps = Environment.ProcessorCount;
            _pipeline.TimeoutMs = 300000;
            
            Debug.Log($"[PipelineBuilder] Reset builder");
            return this;
        }
        
        /// <summary>
        /// Builder'ı klonla
        /// </summary>
        /// <returns>Klonlanmış builder</returns>
        public IPipelineBuilder Clone()
        {
            var clone = new PipelineBuilder(_pipeline.Name, _pipeline.Description)
                .WithExecutionStrategy(_pipeline.ExecutionStrategy)
                .WithMaxParallelSteps(_pipeline.MaxParallelSteps)
                .WithTimeout(_pipeline.TimeoutMs);
            
            // Step'leri kopyala
            foreach (var step in _steps)
            {
                clone.AddStep(step);
            }
            
            // Metadata'yı kopyala
            foreach (var metadata in _metadata)
            {
                clone.WithMetadata(metadata.Key, metadata.Value);
            }
            
            Debug.Log($"[PipelineBuilder] Cloned builder");
            return clone;
        }
    }
    
    /// <summary>
    /// Conditional step wrapper
    /// </summary>
    internal class ConditionalStepWrapper : BasePipelineStep, IConditionalPipelineStep
    {
        private readonly IPipelineStep _innerStep;
        private readonly Func<PipelineContext, bool> _condition;
        
        public ConditionalStepWrapper(IPipelineStep innerStep, Func<PipelineContext, bool> condition)
            : base(innerStep?.Name ?? "ConditionalStep", innerStep?.Description, innerStep?.Priority ?? 0)
        {
            _innerStep = innerStep ?? throw new ArgumentNullException(nameof(innerStep));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            
            MaxRetries = _innerStep.MaxRetries;
            TimeoutMs = _innerStep.TimeoutMs;
        }
        
        public async System.Threading.Tasks.Task<bool> ShouldExecuteAsync(PipelineContext context)
        {
            return await System.Threading.Tasks.Task.FromResult(_condition(context));
        }
        
        protected override async System.Threading.Tasks.Task<PipelineStepResult> OnExecuteAsync(PipelineContext context)
        {
            return await _innerStep.ExecuteAsync(context);
        }
    }
    
    /// <summary>
    /// Async conditional step wrapper
    /// </summary>
    internal class AsyncConditionalStepWrapper : BasePipelineStep, IConditionalPipelineStep
    {
        private readonly IPipelineStep _innerStep;
        private readonly Func<PipelineContext, System.Threading.Tasks.Task<bool>> _condition;
        
        public AsyncConditionalStepWrapper(IPipelineStep innerStep, Func<PipelineContext, System.Threading.Tasks.Task<bool>> condition)
            : base(innerStep?.Name ?? "AsyncConditionalStep", innerStep?.Description, innerStep?.Priority ?? 0)
        {
            _innerStep = innerStep ?? throw new ArgumentNullException(nameof(innerStep));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            
            MaxRetries = _innerStep.MaxRetries;
            TimeoutMs = _innerStep.TimeoutMs;
        }
        
        public async System.Threading.Tasks.Task<bool> ShouldExecuteAsync(PipelineContext context)
        {
            return await _condition(context);
        }
        
        protected override async System.Threading.Tasks.Task<PipelineStepResult> OnExecuteAsync(PipelineContext context)
        {
            return await _innerStep.ExecuteAsync(context);
        }
    }
    
    /// <summary>
    /// Parallel step wrapper
    /// </summary>
    internal class ParallelStepWrapper : BasePipelineStep, IParallelPipelineStep
    {
        private readonly IPipelineStep _innerStep;
        
        public ParallelStepWrapper(IPipelineStep innerStep, string[] dependencies)
            : base(innerStep?.Name ?? "ParallelStep", innerStep?.Description, innerStep?.Priority ?? 0)
        {
            _innerStep = innerStep ?? throw new ArgumentNullException(nameof(innerStep));
            Dependencies = dependencies ?? new string[0];
            
            MaxRetries = _innerStep.MaxRetries;
            TimeoutMs = _innerStep.TimeoutMs;
        }
        
        public bool CanRunInParallel => true;
        public string[] Dependencies { get; }
        
        protected override async System.Threading.Tasks.Task<PipelineStepResult> OnExecuteAsync(PipelineContext context)
        {
            return await _innerStep.ExecuteAsync(context);
        }
    }
    
    /// <summary>
    /// Static pipeline builder factory
    /// </summary>
    public static class PipelineFactory
    {
        /// <summary>
        /// Yeni pipeline builder oluştur
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        /// <returns>Pipeline builder</returns>
        public static IPipelineBuilder Create(string name, string description = null)
        {
            return new PipelineBuilder(name, description);
        }
        
        /// <summary>
        /// Sequential pipeline builder oluştur
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        /// <returns>Pipeline builder</returns>
        public static IPipelineBuilder CreateSequential(string name, string description = null)
        {
            return new PipelineBuilder(name, description)
                .WithExecutionStrategy(PipelineExecutionStrategy.Sequential);
        }
        
        /// <summary>
        /// Parallel pipeline builder oluştur
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        /// <param name="maxParallelSteps">Maksimum paralel step sayısı</param>
        /// <returns>Pipeline builder</returns>
        public static IPipelineBuilder CreateParallel(string name, string description = null, int maxParallelSteps = 0)
        {
            var builder = new PipelineBuilder(name, description)
                .WithExecutionStrategy(PipelineExecutionStrategy.Parallel);
            
            if (maxParallelSteps > 0)
            {
                builder.WithMaxParallelSteps(maxParallelSteps);
            }
            
            return builder;
        }
        
        /// <summary>
        /// Conditional pipeline builder oluştur
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        /// <returns>Pipeline builder</returns>
        public static IPipelineBuilder CreateConditional(string name, string description = null)
        {
            return new PipelineBuilder(name, description)
                .WithExecutionStrategy(PipelineExecutionStrategy.Conditional);
        }
        
        /// <summary>
        /// Hybrid pipeline builder oluştur
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="description">Pipeline açıklaması</param>
        /// <returns>Pipeline builder</returns>
        public static IPipelineBuilder CreateHybrid(string name, string description = null)
        {
            return new PipelineBuilder(name, description)
                .WithExecutionStrategy(PipelineExecutionStrategy.Hybrid);
        }
    }
}
