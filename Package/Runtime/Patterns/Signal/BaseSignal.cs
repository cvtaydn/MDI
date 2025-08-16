using System;

namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Tüm signal'lar için base class
    /// </summary>
    public abstract class BaseSignal : ISignal
    {
        /// <summary>
        /// Signal'ın unique identifier'ı
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Signal'ın timestamp'i
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        /// Signal'ın priority'si
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Signal'ın persistent olup olmadığı
        /// </summary>
        public bool IsPersistent { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Signal ID'si</param>
        /// <param name="priority">Priority (default: 0)</param>
        /// <param name="isPersistent">Persistent olup olmadığı (default: false)</param>
        protected BaseSignal(string id = null, int priority = 0, bool isPersistent = false)
        {
            Id = id ?? Guid.NewGuid().ToString();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Priority = priority;
            IsPersistent = isPersistent;
        }

        /// <summary>
        /// Signal'ın string representation'ı
        /// </summary>
        public override string ToString()
        {
            return $"{GetType().Name} [ID: {Id}, Priority: {Priority}, Timestamp: {Timestamp}]";
        }
    }
}
