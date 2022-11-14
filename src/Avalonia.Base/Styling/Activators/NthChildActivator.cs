#nullable enable
using Avalonia.LogicalTree;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An <see cref="IStyleActivator"/> which is active when control's index was changed.
    /// </summary>
    internal sealed class NthChildActivator : StyleActivatorBase
    {
        private readonly ILogical _control;
        private readonly IChildIndexProvider _provider;
        private readonly int _step;
        private readonly int _offset;
        private readonly bool _reversed;

        public NthChildActivator(
            ILogical control,
            IChildIndexProvider provider,
            int step, int offset, bool reversed)
        {
            _control = control;
            _provider = provider;
            _step = step;
            _offset = offset;
            _reversed = reversed;
        }

        protected override void Initialize()
        {
            PublishNext(IsMatching());
            _provider.ChildIndexChanged += ChildIndexChanged;
        }

        protected override void Deinitialize()
        {
            _provider.ChildIndexChanged -= ChildIndexChanged;
        }

        private void ChildIndexChanged(object? sender, ChildIndexChangedEventArgs e)
        {
            // Run matching again if:
            // 1. Selector is reversed, so other item insertion/deletion might affect total count without changing subscribed item index.
            // 2. e.Child is null, when all children indeces were changed.
            // 3. Subscribed child index was changed.
            if (_reversed
                || e.Child is null                
                || e.Child == _control)
            {
                PublishNext(IsMatching());
            }
        }

        private bool IsMatching() => NthChildSelector.Evaluate(_control, _provider, _step, _offset, _reversed).IsMatch;
    }
}
