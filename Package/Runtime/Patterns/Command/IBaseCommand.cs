using System;
using System.Threading.Tasks;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Tüm command'lar için temel arayüz
    /// </summary>
    public interface IBaseCommand
    {
        /// <summary>
        /// Command'ın unique identifier'ı
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Command'ın adı
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Command'ın açıklaması
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Command'ın priority'si (yüksek sayı = yüksek priority)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Command'ın çalıştırılabilir olup olmadığı
        /// </summary>
        bool CanExecute { get; }

        /// <summary>
        /// Command'ı çalıştırır
        /// </summary>
        /// <returns>Execution result</returns>
        CommandResult Execute();

        /// <summary>
        /// Command'ı asenkron olarak çalıştırır
        /// </summary>
        /// <returns>Async execution result</returns>
        Task<CommandResult> ExecuteAsync();

        /// <summary>
        /// Command'ı geri alır (undo)
        /// </summary>
        /// <returns>Undo result</returns>
        CommandResult Undo();

        /// <summary>
        /// Command'ın undo edilebilir olup olmadığı
        /// </summary>
        bool CanUndo { get; }
    }

    /// <summary>
    /// Generic command arayüzü
    /// </summary>
    /// <typeparam name="TResult">Command sonuç tipi</typeparam>
    public interface IBaseCommand<TResult> : IBaseCommand
    {
        /// <summary>
        /// Typed command execution
        /// </summary>
        /// <returns>Typed result</returns>
        new TResult Execute();

        /// <summary>
        /// Typed async command execution
        /// </summary>
        /// <returns>Typed async result</returns>
        new Task<TResult> ExecuteAsync();
    }
}