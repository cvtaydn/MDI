namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Tüm signal'lar için temel interface
    /// </summary>
    public interface ISignal
    {
        /// <summary>
        /// Signal'ın unique identifier'ı
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Signal'ın timestamp'i
        /// </summary>
        long Timestamp { get; }

        /// <summary>
        /// Signal'ın priority'si (yüksek sayı = yüksek priority)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Signal'ın persistent olup olmadığı
        /// </summary>
        bool IsPersistent { get; }
    }
}
