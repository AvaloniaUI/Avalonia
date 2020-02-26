#nullable enable

namespace Avalonia.Styling.Activators
{
    internal class NotActivator : StyleActivatorBase, IStyleActivatorSink
    {
        private readonly IStyleActivator _source;
        public NotActivator(IStyleActivator source) => _source = source;
        void IStyleActivatorSink.OnNext(bool value, int tag) => PublishNext(!value);
        protected override void Initialize() => _source.Subscribe(this, 0);
        protected override void Deinitialize() => _source.Unsubscribe(this);
    }
}
