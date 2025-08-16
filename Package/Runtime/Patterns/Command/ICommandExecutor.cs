using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Command execution stratejileri
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>
        /// Sıralı execution - command'lar sırayla çalıştırılır
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Paralel execution - command'lar aynı anda çalıştırılır
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Koşullu execution - önceki command başarılıysa devam eder
        /// </summary>
        Conditional
    }

    /// <summary>
    /// Command executor arayüzü
    /// </summary>
    public interface ICommandExecutor
    {
        /// <summary>
        /// Tek bir command'ı çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Execution result</returns>
        CommandResult Execute(IBaseCommand command);

        /// <summary>
        /// Tek bir command'ı asenkron olarak çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Async execution result</returns>
        Task<CommandResult> ExecuteAsync(IBaseCommand command);

        /// <summary>
        /// Birden fazla command'ı belirtilen strateji ile çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <param name="strategy">Execution stratejisi</param>
        /// <returns>Execution results</returns>
        IEnumerable<CommandResult> Execute(IEnumerable<IBaseCommand> commands, ExecutionStrategy strategy = ExecutionStrategy.Sequential);

        /// <summary>
        /// Birden fazla command'ı asenkron olarak belirtilen strateji ile çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <param name="strategy">Execution stratejisi</param>
        /// <returns>Async execution results</returns>
        Task<IEnumerable<CommandResult>> ExecuteAsync(IEnumerable<IBaseCommand> commands, ExecutionStrategy strategy = ExecutionStrategy.Sequential);

        /// <summary>
        /// Command'ları sıralı olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        IEnumerable<CommandResult> ExecuteSequentially(params IBaseCommand[] commands);

        /// <summary>
        /// Command'ları paralel olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        Task<IEnumerable<CommandResult>> ExecuteParallel(params IBaseCommand[] commands);

        /// <summary>
        /// Command'ları koşullu olarak çalıştırır (önceki başarılıysa devam eder)
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        IEnumerable<CommandResult> ExecuteConditional(params IBaseCommand[] commands);

        /// <summary>
        /// Command'ı undo eder
        /// </summary>
        /// <param name="command">Undo edilecek command</param>
        /// <returns>Undo result</returns>
        CommandResult Undo(IBaseCommand command);

        /// <summary>
        /// Son çalıştırılan command'ı undo eder
        /// </summary>
        /// <returns>Undo result</returns>
        CommandResult UndoLast();

        /// <summary>
        /// Tüm command history'sini temizler
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Command execution history'si
        /// </summary>
        IReadOnlyList<IBaseCommand> ExecutionHistory { get; }

        /// <summary>
        /// Command execution event'i
        /// </summary>
        event Action<IBaseCommand, CommandResult> CommandExecuted;

        /// <summary>
        /// Command undo event'i
        /// </summary>
        event Action<IBaseCommand, CommandResult> CommandUndone;
    }
}