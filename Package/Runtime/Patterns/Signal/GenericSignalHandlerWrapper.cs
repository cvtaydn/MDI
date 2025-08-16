using System;

namespace MDI.Patterns.Signal
{
    /// <summary>
    /// Generic signal handler'ları non-generic interface ile uyumlu hale getiren wrapper
    /// </summary>
    /// <typeparam name="TSignal">Signal tipi</typeparam>
    internal class GenericSignalHandlerWrapper<TSignal> : ISignalHandler where TSignal : ISignal
    {
        private readonly ISignalHandler<TSignal> _handler;

        public GenericSignalHandlerWrapper(ISignalHandler<TSignal> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Handle(ISignal signal)
        {
            if (signal is TSignal typedSignal)
            {
                _handler.Handle(typedSignal);
            }
        }

        public int Priority => _handler.Priority;
        public bool IsActive => _handler.IsActive;
        public Type SignalType => typeof(TSignal);

        public override bool Equals(object obj)
        {
            if (obj is GenericSignalHandlerWrapper<TSignal> other)
            {
                return _handler.Equals(other._handler);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _handler.GetHashCode();
        }
    }
}
