using System;
using System.Threading.Tasks;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Temel command implementasyonu
    /// </summary>
    public abstract class BaseCommand : IBaseCommand
    {
        private static int _nextId = 1;
        
        /// <summary>
        /// Command ID'si
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Command adı
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Command açıklaması
        /// </summary>
        public virtual string Description { get; protected set; }

        /// <summary>
        /// Command önceliği
        /// </summary>
        public virtual int Priority { get; protected set; }

        /// <summary>
        /// Command çalıştırılabilir mi
        /// </summary>
        public virtual bool CanExecute { get; protected set; } = true;

        /// <summary>
        /// Command geri alınabilir mi
        /// </summary>
        public virtual bool CanUndo { get; protected set; } = false;

        /// <summary>
        /// Command çalıştırıldı mı
        /// </summary>
        public bool IsExecuted { get; protected set; }

        /// <summary>
        /// Command execution zamanı
        /// </summary>
        public DateTime? ExecutedAt { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseCommand()
        {
            Id = $"cmd_{_nextId++}";
            Name = GetType().Name;
            Description = $"Command: {Name}";
            Priority = 0;
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="name">Command adı</param>
        /// <param name="description">Command açıklaması</param>
        /// <param name="priority">Command önceliği</param>
        protected BaseCommand(string name, string description = null, int priority = 0) : this()
        {
            Name = name ?? GetType().Name;
            Description = description ?? $"Command: {Name}";
            Priority = priority;
        }

        /// <summary>
        /// Command'ı çalıştırır
        /// </summary>
        public virtual CommandResult Execute()
        {
            if (!CanExecute)
                return CommandResult.Failure($"Command '{Name}' cannot be executed");

            if (IsExecuted)
                return CommandResult.Failure($"Command '{Name}' has already been executed");

            try
            {
                OnExecute();
                IsExecuted = true;
                ExecutedAt = DateTime.UtcNow;
                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                OnExecutionFailed(ex);
                return CommandResult.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Command'ı asenkron olarak çalıştırır
        /// </summary>
        public virtual async Task<CommandResult> ExecuteAsync()
        {
            if (!CanExecute)
                return CommandResult.Failure($"Command '{Name}' cannot be executed");

            if (IsExecuted)
                return CommandResult.Failure($"Command '{Name}' has already been executed");

            try
            {
                await OnExecuteAsync();
                IsExecuted = true;
                ExecutedAt = DateTime.UtcNow;
                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                OnExecutionFailed(ex);
                return CommandResult.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Command'ı geri alır
        /// </summary>
        public virtual CommandResult Undo()
        {
            if (!CanUndo)
                return CommandResult.Failure($"Command '{Name}' cannot be undone");

            if (!IsExecuted)
                return CommandResult.Failure($"Command '{Name}' has not been executed yet");

            try
            {
                OnUndo();
                IsExecuted = false;
                ExecutedAt = null;
                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                OnUndoFailed(ex);
                return CommandResult.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Command execution implementasyonu
        /// </summary>
        protected abstract void OnExecute();

        /// <summary>
        /// Async command execution implementasyonu
        /// </summary>
        protected virtual async Task OnExecuteAsync()
        {
            await Task.Run(OnExecute);
        }

        /// <summary>
        /// Command undo implementasyonu
        /// </summary>
        protected virtual void OnUndo()
        {
            // Default implementation - override if needed
        }

        /// <summary>
        /// Command execution başarısız olduğunda çağrılır
        /// </summary>
        /// <param name="exception">Oluşan exception</param>
        protected virtual void OnExecutionFailed(Exception exception)
        {
            // Default implementation - override if needed
        }

        /// <summary>
        /// Command undo başarısız olduğunda çağrılır
        /// </summary>
        /// <param name="exception">Oluşan exception</param>
        protected virtual void OnUndoFailed(Exception exception)
        {
            // Default implementation - override if needed
        }

        /// <summary>
        /// Command'ı string olarak temsil eder
        /// </summary>
        public override string ToString()
        {
            return $"[{Id}] {Name} - {Description} (Priority: {Priority}, Executed: {IsExecuted})";
        }
    }

    /// <summary>
    /// Generic base command with result
    /// </summary>
    /// <typeparam name="TResult">Result tipi</typeparam>
    public abstract class BaseCommand<TResult> : BaseCommand, IBaseCommand<TResult>
    {
        /// <summary>
        /// Command result'ı
        /// </summary>
        public TResult Result { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseCommand() : base()
        {
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="name">Command adı</param>
        /// <param name="description">Command açıklaması</param>
        /// <param name="priority">Command önceliği</param>
        protected BaseCommand(string name, string description = null, int priority = 0) 
            : base(name, description, priority)
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
        /// Generic command execution implementasyonu
        /// </summary>
        protected abstract TResult OnExecuteWithResult();

        /// <summary>
        /// Async generic command execution implementasyonu
        /// </summary>
        protected virtual async Task<TResult> OnExecuteWithResultAsync()
        {
            return await Task.FromResult(OnExecuteWithResult());
        }

        /// <summary>
        /// Base execute implementasyonu
        /// </summary>
        protected override void OnExecute()
        {
            Result = OnExecuteWithResult();
        }

        /// <summary>
        /// Base async execute implementasyonu
        /// </summary>
        protected override async Task OnExecuteAsync()
        {
            Result = await OnExecuteWithResultAsync();
        }
    }
}