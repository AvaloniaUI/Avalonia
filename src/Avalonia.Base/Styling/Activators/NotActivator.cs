namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An <see cref="IStyleActivator"/> which inverts the state of an input activator.
    /// </summary>
    internal class NotActivator : StyleActivatorBase, IStyleActivatorSink
    {
        private readonly IStyleActivator _source;
        public NotActivator(IStyleActivator source) => _source = source;
        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();
        protected override bool EvaluateIsActive() => !_source.GetIsActive();
        protected override void Initialize() => _source.Subscribe(this);
        protected override void Deinitialize() => _source.Unsubscribe(this);
    }
}
