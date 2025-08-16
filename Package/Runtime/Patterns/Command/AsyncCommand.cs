using System;
using System.Threading;
using System.Threading.Tasks;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Asenkron command implementasyonu
    /// </summary>
    public abstract class AsyncCommand : BaseCommand
    {
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Command iptal edildi mi
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Command çalışıyor mu
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Command timeout süresi (millisecond)
        /// </summary>
        public int TimeoutMs { get; protected set; } = 30000; // 30 saniye default

        /// <summary>
        /// Constructor
        /// </summary>
        protected AsyncCommand() : base()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="name">Command adı</param>
        /// <param name="description">Command açıklaması</param>
        /// <param name="priority">Command önceliği</param>
        /// <param name="timeoutMs">Timeout süresi</param>
        protected AsyncCommand(string name, string description = null, int priority = 0, int timeoutMs = 30000) 
            : base(name, description, priority)
        {
            TimeoutMs = timeoutMs;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Sync execute - async execute'u çağırır
        /// </summary>
        protected override void OnExecute()
        {
            // Sync execute için async execute'u bekle
            OnExecuteAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Async execute implementasyonu
        /// </summary>
        protected override async Task OnExecuteAsync()
        {
            if (IsRunning)
                throw new InvalidOperationException($"Command '{Name}' is already running");

            IsRunning = true;
            IsCancelled = false;

            try
            {
                // Timeout ile birlikte çalıştır
                using var timeoutCts = new CancellationTokenSource(TimeoutMs);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, timeoutCts.Token);

                await OnExecuteAsyncCore(combinedCts.Token);
            }
            catch (OperationCanceledException)
            {
                IsCancelled = true;
                throw;
            }
            catch (TimeoutException)
            {
                IsCancelled = true;
                throw new TimeoutException($"Command '{Name}' timed out after {TimeoutMs}ms");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Async command core implementasyonu
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        protected abstract Task OnExecuteAsyncCore(CancellationToken cancellationToken);

        /// <summary>
        /// Command'ı iptal eder
        /// </summary>
        public virtual void Cancel()
        {
            if (!IsRunning)
                return;

            _cancellationTokenSource?.Cancel();
            IsCancelled = true;
        }

        /// <summary>
        /// Command'ı belirtilen süre sonra iptal eder
        /// </summary>
        /// <param name="delayMs">Gecikme süresi (millisecond)</param>
        public virtual void CancelAfter(int delayMs)
        {
            _cancellationTokenSource?.CancelAfter(delayMs);
        }

        /// <summary>
        /// Cancellation token'ı yeniler
        /// </summary>
        protected virtual void ResetCancellationToken()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            IsCancelled = false;
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~AsyncCommand()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Generic async command with result
    /// </summary>
    /// <typeparam name="TResult">Result tipi</typeparam>
    public abstract class AsyncCommand<TResult> : AsyncCommand, IBaseCommand<TResult>
    {
        /// <summary>
        /// Command result'ı
        /// </summary>
        public TResult Result { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected AsyncCommand() : base()
        {
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="name">Command adı</param>
        /// <param name="description">Command açıklaması</param>
        /// <param name="priority">Command önceliği</param>
        /// <param name="timeoutMs">Timeout süresi</param>
        protected AsyncCommand(string name, string description = null, int priority = 0, int timeoutMs = 30000) 
            : base(name, description, priority, timeoutMs)
        {
        }

        /// <summary>
        /// Command'ı çalıştırır ve result döner
        /// </summary>
        public new virtual TResult Execute()
        {
            var result = base.Execute();
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage);
            return Result;
        }

        /// <summary>
        /// Command'ı asenkron olarak çalıştırır ve result döner
        /// </summary>
        public new virtual async Task<TResult> ExecuteAsync()
        {
            var result = await base.ExecuteAsync();
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage);
            return Result;
        }

        /// <summary>
        /// Command'ı çalıştırır ve result döner
        /// </summary>
        public virtual TResult ExecuteWithResult()
        {
            return Execute();
        }

        /// <summary>
        /// Command'ı asenkron olarak çalıştırır ve result döner
        /// </summary>
        public virtual async Task<TResult> ExecuteWithResultAsync()
        {
            return await ExecuteAsync();
        }

        /// <summary>
        /// Generic async command core implementasyonu
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        protected abstract Task<TResult> OnExecuteAsyncCoreWithResult(CancellationToken cancellationToken);

        /// <summary>
        /// Base async core implementasyonu
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        protected override async Task OnExecuteAsyncCore(CancellationToken cancellationToken)
        {
            Result = await OnExecuteAsyncCoreWithResult(cancellationToken);
        }
    }
}