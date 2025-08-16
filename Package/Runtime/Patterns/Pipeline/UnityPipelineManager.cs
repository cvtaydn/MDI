using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Pipeline
{
    /// <summary>
    /// Unity için Pipeline Manager
    /// </summary>
    public class UnityPipelineManager : MonoBehaviour
    {
        [Header("Pipeline Settings")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private int maxConcurrentPipelines = 5;
        [SerializeField] private int defaultTimeoutSeconds = 300;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxHistorySize = 100;
        [SerializeField] private bool autoCleanupHistory = true;
        [SerializeField] private float historyCleanupInterval = 60f;
        
        // Singleton instance
        private static UnityPipelineManager _instance;
        public static UnityPipelineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UnityPipelineManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[UnityPipelineManager]");
                        _instance = go.AddComponent<UnityPipelineManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        // Pipeline management
        private readonly Dictionary<string, IPipeline> _registeredPipelines = new Dictionary<string, IPipeline>();
        private readonly Dictionary<string, Task<PipelineExecutionResult>> _runningPipelines = new Dictionary<string, Task<PipelineExecutionResult>>();
        private readonly List<PipelineExecutionResult> _executionHistory = new List<PipelineExecutionResult>();
        private SemaphoreSlim _concurrencyLimiter;
        
        // Events
        public event Action<string, PipelineExecutionResult> PipelineCompleted;
        public event Action<string, Exception> PipelineFailed;
        public event Action<string> PipelineStarted;
        public event Action<string> PipelineCancelled;
        
        // Properties
        public int RunningPipelineCount => _runningPipelines.Count;
        public int RegisteredPipelineCount => _registeredPipelines.Count;
        public IReadOnlyList<PipelineExecutionResult> ExecutionHistory => _executionHistory.AsReadOnly();
        public bool IsInitialized { get; private set; }
        
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
        
        private void Initialize()
        {
            _concurrencyLimiter?.Dispose();
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentPipelines, maxConcurrentPipelines);
            
            if (autoCleanupHistory)
            {
                InvokeRepeating(nameof(CleanupHistory), historyCleanupInterval, historyCleanupInterval);
            }
            
            IsInitialized = true;
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Initialized with max {maxConcurrentPipelines} concurrent pipelines");
            }
        }
        
        private void OnDestroy()
        {
            CancelAllPipelines();
            _concurrencyLimiter?.Dispose();
        }
        
        /// <summary>
        /// Pipeline register et
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="pipeline">Pipeline instance</param>
        public void RegisterPipeline(string name, IPipeline pipeline)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Pipeline name cannot be null or empty", nameof(name));
            
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            
            _registeredPipelines[name] = pipeline;
            
            // Pipeline event'lerini dinle
            if (pipeline is IPipelineEvents events)
            {
                events.PipelineCompleted += (p, result) => OnPipelineCompleted(name, result);
                events.PipelineFailed += (p, ex) => OnPipelineFailed(name, ex);
                events.PipelineStarted += (p, ctx) => OnPipelineStarted(name);
                events.PipelineCancelled += (p) => OnPipelineCancelled(name);
            }
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Registered pipeline: {name}");
            }
        }
        
        /// <summary>
        /// Pipeline register et (builder ile)
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="builder">Pipeline builder</param>
        public void RegisterPipeline(string name, IPipelineBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            
            var pipeline = builder.Build();
            RegisterPipeline(name, pipeline);
        }
        
        /// <summary>
        /// Pipeline register et (factory ile)
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="factory">Pipeline factory</param>
        public void RegisterPipeline(string name, Func<IPipeline> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            var pipeline = factory();
            RegisterPipeline(name, pipeline);
        }
        
        /// <summary>
        /// Pipeline'ı çalıştır
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution task</returns>
        public async Task<PipelineExecutionResult> ExecutePipelineAsync(string name, object input = null, CancellationToken cancellationToken = default)
        {
            if (!_registeredPipelines.TryGetValue(name, out var pipeline))
            {
                throw new ArgumentException($"Pipeline '{name}' is not registered", nameof(name));
            }
            
            if (_runningPipelines.ContainsKey(name))
            {
                throw new InvalidOperationException($"Pipeline '{name}' is already running");
            }
            
            await _concurrencyLimiter.WaitAsync(cancellationToken);
            
            try
            {
                var executionTask = ExecutePipelineInternal(name, pipeline, input, cancellationToken);
                _runningPipelines[name] = executionTask;
                
                var result = await executionTask;
                return result;
            }
            finally
            {
                _runningPipelines.Remove(name);
                _concurrencyLimiter.Release();
            }
        }
        
        /// <summary>
        /// Pipeline'ı fire-and-forget olarak çalıştır
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <param name="input">Input data</param>
        /// <param name="onCompleted">Completion callback</param>
        /// <param name="onFailed">Failure callback</param>
        public void ExecutePipelineFireAndForget(string name, object input = null, 
            Action<PipelineExecutionResult> onCompleted = null, 
            Action<Exception> onFailed = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await ExecutePipelineAsync(name, input);
                    onCompleted?.Invoke(result);
                }
                catch (Exception ex)
                {
                    onFailed?.Invoke(ex);
                    
                    if (enableLogging)
                    {
                        Debug.LogError($"[UnityPipelineManager] Pipeline '{name}' failed: {ex.Message}");
                    }
                }
            });
        }
        
        /// <summary>
        /// Birden fazla pipeline'ı sırayla çalıştır
        /// </summary>
        /// <param name="pipelineNames">Pipeline adları</param>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution sonuçları</returns>
        public async Task<List<PipelineExecutionResult>> ExecutePipelinesSequentiallyAsync(
            IEnumerable<string> pipelineNames, 
            object input = null, 
            CancellationToken cancellationToken = default)
        {
            var results = new List<PipelineExecutionResult>();
            var currentInput = input;
            
            foreach (var name in pipelineNames)
            {
                var result = await ExecutePipelineAsync(name, currentInput, cancellationToken);
                results.Add(result);
                
                // Bir sonraki pipeline için output'u input olarak kullan
                currentInput = result.Result;
                
                // Eğer pipeline başarısız olduysa dur
                if (!result.IsSuccess)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[UnityPipelineManager] Sequential execution stopped at '{name}' due to failure");
                    }
                    break;
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Birden fazla pipeline'ı paralel çalıştır
        /// </summary>
        /// <param name="pipelineNames">Pipeline adları</param>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution sonuçları</returns>
        public async Task<List<PipelineExecutionResult>> ExecutePipelinesParallelAsync(
            IEnumerable<string> pipelineNames, 
            object input = null, 
            CancellationToken cancellationToken = default)
        {
            var tasks = pipelineNames.Select(name => ExecutePipelineAsync(name, input, cancellationToken));
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
        
        /// <summary>
        /// Pipeline'ı iptal et
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        public void CancelPipeline(string name)
        {
            if (_registeredPipelines.TryGetValue(name, out var pipeline))
            {
                pipeline.Cancel();
                
                if (enableLogging)
                {
                    Debug.Log($"[UnityPipelineManager] Cancelled pipeline: {name}");
                }
            }
        }
        
        /// <summary>
        /// Tüm pipeline'ları iptal et
        /// </summary>
        public void CancelAllPipelines()
        {
            foreach (var pipeline in _registeredPipelines.Values)
            {
                pipeline.Cancel();
            }
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Cancelled all pipelines");
            }
        }
        
        /// <summary>
        /// Pipeline'ın çalışıp çalışmadığını kontrol et
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <returns>Çalışıyor mu</returns>
        public bool IsPipelineRunning(string name)
        {
            return _runningPipelines.ContainsKey(name);
        }
        
        /// <summary>
        /// Pipeline'ı unregister et
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        public void UnregisterPipeline(string name)
        {
            if (_runningPipelines.ContainsKey(name))
            {
                CancelPipeline(name);
            }
            
            _registeredPipelines.Remove(name);
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Unregistered pipeline: {name}");
            }
        }
        
        /// <summary>
        /// Tüm pipeline'ları unregister et
        /// </summary>
        public void UnregisterAllPipelines()
        {
            CancelAllPipelines();
            _registeredPipelines.Clear();
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Unregistered all pipelines");
            }
        }
        
        /// <summary>
        /// Execution history'yi temizle
        /// </summary>
        public void ClearHistory()
        {
            _executionHistory.Clear();
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Cleared execution history");
            }
        }
        
        /// <summary>
        /// Pipeline istatistiklerini al
        /// </summary>
        /// <param name="name">Pipeline adı</param>
        /// <returns>İstatistikler</returns>
        public PipelineStatistics GetPipelineStatistics(string name)
        {
            var executions = _executionHistory.Where(x => x.Context?.GetMetadata<string>("pipelineName") == name).ToList();
            
            return new PipelineStatistics
            {
                PipelineName = name,
                TotalExecutions = executions.Count,
                SuccessfulExecutions = executions.Count(x => x.IsSuccess),
                FailedExecutions = executions.Count(x => !x.IsSuccess),
                AverageExecutionTime = executions.Any() ? executions.Average(x => x.ExecutionTime.TotalMilliseconds) : 0,
                LastExecutionTime = executions.LastOrDefault()?.ExecutionTime ?? TimeSpan.Zero,
                LastExecutedAt = executions.LastOrDefault()?.Context?.StartTime ?? DateTime.MinValue
            };
        }
        
        /// <summary>
        /// Tüm pipeline istatistiklerini al
        /// </summary>
        /// <returns>İstatistikler listesi</returns>
        public List<PipelineStatistics> GetAllPipelineStatistics()
        {
            return _registeredPipelines.Keys.Select(GetPipelineStatistics).ToList();
        }
        
        /// <summary>
        /// Pipeline internal execution
        /// </summary>
        private async Task<PipelineExecutionResult> ExecutePipelineInternal(string name, IPipeline pipeline, object input, CancellationToken cancellationToken)
        {
            var context = new PipelineContext(input)
            {
                CancellationToken = cancellationToken
            };
            
            context.SetMetadata("pipelineName", name);
            context.SetMetadata("executionId", Guid.NewGuid().ToString());
            
            var result = await pipeline.ExecuteAsync(context);
            
            // History'ye ekle
            AddToHistory(result);
            
            return result;
        }
        
        /// <summary>
        /// History'ye execution sonucu ekle
        /// </summary>
        private void AddToHistory(PipelineExecutionResult result)
        {
            _executionHistory.Add(result);
            
            // History boyutunu kontrol et
            if (_executionHistory.Count > maxHistorySize)
            {
                var removeCount = _executionHistory.Count - maxHistorySize;
                _executionHistory.RemoveRange(0, removeCount);
            }
        }
        
        /// <summary>
        /// History cleanup
        /// </summary>
        private void CleanupHistory()
        {
            if (_executionHistory.Count > maxHistorySize)
            {
                var removeCount = _executionHistory.Count - maxHistorySize;
                _executionHistory.RemoveRange(0, removeCount);
                
                if (enableLogging)
                {
                    Debug.Log($"[UnityPipelineManager] Cleaned up {removeCount} old execution records");
                }
            }
        }
        
        // Event handlers
        private void OnPipelineCompleted(string name, PipelineExecutionResult result)
        {
            PipelineCompleted?.Invoke(name, result);
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Pipeline '{name}' completed in {result.ExecutionTime.TotalMilliseconds:F2}ms");
            }
        }
        
        private void OnPipelineFailed(string name, Exception exception)
        {
            PipelineFailed?.Invoke(name, exception);
            
            if (enableLogging)
            {
                Debug.LogError($"[UnityPipelineManager] Pipeline '{name}' failed: {exception.Message}");
            }
        }
        
        private void OnPipelineStarted(string name)
        {
            PipelineStarted?.Invoke(name);
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Pipeline '{name}' started");
            }
        }
        
        private void OnPipelineCancelled(string name)
        {
            PipelineCancelled?.Invoke(name);
            
            if (enableLogging)
            {
                Debug.Log($"[UnityPipelineManager] Pipeline '{name}' cancelled");
            }
        }
    }
    
    /// <summary>
    /// Pipeline istatistikleri
    /// </summary>
    [Serializable]
    public class PipelineStatistics
    {
        public string PipelineName { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double AverageExecutionTime { get; set; }
        public TimeSpan LastExecutionTime { get; set; }
        public DateTime LastExecutedAt { get; set; }
        
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions * 100 : 0;
    }
}