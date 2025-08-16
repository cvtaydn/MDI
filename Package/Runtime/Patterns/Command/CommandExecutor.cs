using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Command executor implementasyonu
    /// </summary>
    public class CommandExecutor : ICommandExecutor
    {
        private readonly List<IBaseCommand> _executionHistory;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Command execution event'i
        /// </summary>
        public event Action<IBaseCommand, CommandResult> CommandExecuted;

        /// <summary>
        /// Command undo event'i
        /// </summary>
        public event Action<IBaseCommand, CommandResult> CommandUndone;

        /// <summary>
        /// Command execution history'si
        /// </summary>
        public IReadOnlyList<IBaseCommand> ExecutionHistory => _executionHistory.AsReadOnly();

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandExecutor()
        {
            _executionHistory = new List<IBaseCommand>();
        }

        /// <summary>
        /// Tek bir command'ı çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Execution result</returns>
        public CommandResult Execute(IBaseCommand command)
        {
            if (command == null)
                return CommandResult.Failure("Command cannot be null");

            if (!command.CanExecute)
                return CommandResult.Failure($"Command '{command.Name}' cannot be executed");

            var stopwatch = Stopwatch.StartNew();
            CommandResult result;

            try
            {
                command.Execute();
                stopwatch.Stop();
                result = CommandResult.Success(executionTimeMs: stopwatch.ElapsedMilliseconds);
                
                lock (_lockObject)
                {
                    _executionHistory.Add(command);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = CommandResult.Failure(ex, stopwatch.ElapsedMilliseconds);
            }

            CommandExecuted?.Invoke(command, result);
            return result;
        }

        /// <summary>
        /// Tek bir command'ı asenkron olarak çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Async execution result</returns>
        public async Task<CommandResult> ExecuteAsync(IBaseCommand command)
        {
            if (command == null)
                return CommandResult.Failure("Command cannot be null");

            if (!command.CanExecute)
                return CommandResult.Failure($"Command '{command.Name}' cannot be executed");

            var stopwatch = Stopwatch.StartNew();
            CommandResult result;

            try
            {
                await command.ExecuteAsync();
                stopwatch.Stop();
                result = CommandResult.Success(executionTimeMs: stopwatch.ElapsedMilliseconds);
                
                lock (_lockObject)
                {
                    _executionHistory.Add(command);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = CommandResult.Failure(ex, stopwatch.ElapsedMilliseconds);
            }

            CommandExecuted?.Invoke(command, result);
            return result;
        }

        /// <summary>
        /// Birden fazla command'ı belirtilen strateji ile çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <param name="strategy">Execution stratejisi</param>
        /// <returns>Execution results</returns>
        public IEnumerable<CommandResult> Execute(IEnumerable<IBaseCommand> commands, ExecutionStrategy strategy = ExecutionStrategy.Sequential)
        {
            if (commands == null)
                return new[] { CommandResult.Failure("Commands cannot be null") };

            var commandList = commands.ToList();
            if (!commandList.Any())
                return Enumerable.Empty<CommandResult>();

            return strategy switch
            {
                ExecutionStrategy.Sequential => ExecuteSequentiallyInternal(commandList),
                ExecutionStrategy.Parallel => ExecuteParallelInternal(commandList).Result,
                ExecutionStrategy.Conditional => ExecuteConditionalInternal(commandList),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
            };
        }

        /// <summary>
        /// Birden fazla command'ı asenkron olarak belirtilen strateji ile çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <param name="strategy">Execution stratejisi</param>
        /// <returns>Async execution results</returns>
        public async Task<IEnumerable<CommandResult>> ExecuteAsync(IEnumerable<IBaseCommand> commands, ExecutionStrategy strategy = ExecutionStrategy.Sequential)
        {
            if (commands == null)
                return new[] { CommandResult.Failure("Commands cannot be null") };

            var commandList = commands.ToList();
            if (!commandList.Any())
                return Enumerable.Empty<CommandResult>();

            return strategy switch
            {
                ExecutionStrategy.Sequential => await ExecuteSequentiallyAsyncInternal(commandList),
                ExecutionStrategy.Parallel => await ExecuteParallelInternal(commandList),
                ExecutionStrategy.Conditional => await ExecuteConditionalAsyncInternal(commandList),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
            };
        }

        /// <summary>
        /// Command'ları sıralı olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        public IEnumerable<CommandResult> ExecuteSequentially(params IBaseCommand[] commands)
        {
            return Execute(commands, ExecutionStrategy.Sequential);
        }

        /// <summary>
        /// Command'ları paralel olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        public async Task<IEnumerable<CommandResult>> ExecuteParallel(params IBaseCommand[] commands)
        {
            return await ExecuteAsync(commands, ExecutionStrategy.Parallel);
        }

        /// <summary>
        /// Command'ları koşullu olarak çalıştırır (önceki başarılıysa devam eder)
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        public IEnumerable<CommandResult> ExecuteConditional(params IBaseCommand[] commands)
        {
            return Execute(commands, ExecutionStrategy.Conditional);
        }

        /// <summary>
        /// Command'ı undo eder
        /// </summary>
        /// <param name="command">Undo edilecek command</param>
        /// <returns>Undo result</returns>
        public CommandResult Undo(IBaseCommand command)
        {
            if (command == null)
                return CommandResult.Failure("Command cannot be null");

            if (!command.CanUndo)
                return CommandResult.Failure($"Command '{command.Name}' cannot be undone");

            var stopwatch = Stopwatch.StartNew();
            CommandResult result;

            try
            {
                command.Undo();
                stopwatch.Stop();
                result = CommandResult.Success(executionTimeMs: stopwatch.ElapsedMilliseconds);
                
                lock (_lockObject)
                {
                    _executionHistory.Remove(command);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = CommandResult.Failure(ex, stopwatch.ElapsedMilliseconds);
            }

            CommandUndone?.Invoke(command, result);
            return result;
        }

        /// <summary>
        /// Son çalıştırılan command'ı undo eder
        /// </summary>
        /// <returns>Undo result</returns>
        public CommandResult UndoLast()
        {
            lock (_lockObject)
            {
                if (!_executionHistory.Any())
                    return CommandResult.Failure("No commands to undo");

                var lastCommand = _executionHistory.Last();
                return Undo(lastCommand);
            }
        }

        /// <summary>
        /// Tüm command history'sini temizler
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _executionHistory.Clear();
            }
        }

        #region Private Methods

        private IEnumerable<CommandResult> ExecuteSequentiallyInternal(IList<IBaseCommand> commands)
        {
            var results = new List<CommandResult>();
            
            foreach (var command in commands)
            {
                var result = Execute(command);
                results.Add(result);
            }

            return results;
        }

        private async Task<IEnumerable<CommandResult>> ExecuteSequentiallyAsyncInternal(IList<IBaseCommand> commands)
        {
            var results = new List<CommandResult>();
            
            foreach (var command in commands)
            {
                var result = await ExecuteAsync(command);
                results.Add(result);
            }

            return results;
        }

        private async Task<IEnumerable<CommandResult>> ExecuteParallelInternal(IList<IBaseCommand> commands)
        {
            var tasks = commands.Select(ExecuteAsync);
            var results = await Task.WhenAll(tasks);
            return results;
        }

        private IEnumerable<CommandResult> ExecuteConditionalInternal(IList<IBaseCommand> commands)
        {
            var results = new List<CommandResult>();
            
            foreach (var command in commands)
            {
                var result = Execute(command);
                results.Add(result);
                
                // Eğer command başarısız olduysa, sonraki command'ları çalıştırma
                if (!result.IsSuccess)
                    break;
            }

            return results;
        }

        private async Task<IEnumerable<CommandResult>> ExecuteConditionalAsyncInternal(IList<IBaseCommand> commands)
        {
            var results = new List<CommandResult>();
            
            foreach (var command in commands)
            {
                var result = await ExecuteAsync(command);
                results.Add(result);
                
                // Eğer command başarısız olduysa, sonraki command'ları çalıştırma
                if (!result.IsSuccess)
                    break;
            }

            return results;
        }

        #endregion
    }
}