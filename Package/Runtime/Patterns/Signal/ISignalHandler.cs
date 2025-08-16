namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Generic signal handler interface'i
    /// </summary>
    /// <typeparam name="TSignal">Handle edilecek signal tipi</typeparam>
    public interface ISignalHandler<in TSignal> where TSignal : ISignal
    {
        /// <summary>
        /// Signal'ı handle eder
        /// </summary>
        /// <param name="signal">Handle edilecek signal</param>
        void Handle(TSignal signal);

        /// <summary>
        /// Handler'ın priority'si (yüksek sayı = yüksek priority)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Handler'ın aktif olup olmadığı
        /// </summary>
        bool IsActive { get; }
    }

    /// <summary>
    /// Non-generic signal handler interface'i
    /// </summary>
    public interface ISignalHandler
    {
        /// <summary>
        /// Signal'ı handle eder
        /// </summary>
        /// <param name="signal">Handle edilecek signal</param>
        void Handle(ISignal signal);

        /// <summary>
        /// Handler'ın priority'si
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Handler'ın aktif olup olmadığı
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Handler'ın handle edebileceği signal tipi
        /// </summary>
        System.Type SignalType { get; }
    }
}
