namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// Base class implementation of <see cref="IStyleActivator"/>.
    /// </summary>
    internal abstract class StyleActivatorBase : IStyleActivator
    {
        private IStyleActivatorSink? _sink;
        private bool? _value;

        public bool GetIsActive()
        {
            var value = EvaluateIsActive();
            _value ??= value;
            return value;
        }

        public bool IsSubscribed => _sink is not null;

        public void Subscribe(IStyleActivatorSink sink)
        {
            if (_sink is null)
            {
                Initialize();
                _sink = sink;
            }
            else
            {
                throw new AvaloniaInternalException("StyleActivator is already subscribed.");
            }
        }

        public void Unsubscribe(IStyleActivatorSink sink)
        {
            if (_sink is null)
                return;

            if (_sink != sink)
            {
                throw new AvaloniaInternalException("StyleActivatorSink is not subscribed.");
            }

            _sink = null;
            Deinitialize();
        }

        public void Dispose()
        {
            _sink = null;
            Deinitialize();
        }

        /// <summary>
        /// Evaluates the activation state.
        /// </summary>
        /// <remarks>
        /// This method should read directly from its inputs and not rely on any subscriptions to
        /// fire in order to be up-to-date.
        /// </remarks>
        protected abstract bool EvaluateIsActive();

        /// <summary>
        /// Called from a derived class when the activation state should be re-evaluated and the 
        /// subscriber notified of any change.
        /// </summary>
        /// <returns>
        /// The evaluated active state;
        /// </returns>
        protected bool ReevaluateIsActive()
        {
            var value = GetIsActive();

            if (value != _value)
            {
                _value = value;
                _sink?.OnNext(value);
            }

            return value;
        }

        /// <summary>
        /// Called in response to a <see cref="Subscribe(IStyleActivatorSink)"/> to allow the
        /// derived class to set up any necessary subscriptions.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Called in response to an <see cref="Unsubscribe(IStyleActivatorSink)"/> or
        /// <see cref="Dispose"/> to allow the derived class to dispose any active subscriptions.
        /// </summary>
        protected abstract void Deinitialize();
    }
}
