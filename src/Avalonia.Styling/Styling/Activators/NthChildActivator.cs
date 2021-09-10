#nullable enable
using System;

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
        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChangedHandler;

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

        private EventHandler<ChildIndexChangedEventArgs> ChildIndexChangedHandler => _childIndexChangedHandler ??= ChildIndexChanged;

        protected override void Initialize()
        {
            PublishNext(IsMatching());
            _provider.ChildIndexChanged += ChildIndexChangedHandler;
        }

        protected override void Deinitialize()
        {
            _provider.ChildIndexChanged -= ChildIndexChangedHandler;
        }

        private void ChildIndexChanged(object sender, ChildIndexChangedEventArgs e)
        {
            if (e.Child is null
                || e.Child == _control)
            {
                PublishNext(IsMatching());
            }
        }

        private bool IsMatching() => NthChildSelector.Evaluate(_control, _provider, _step, _offset, _reversed).IsMatch;
    }
}
