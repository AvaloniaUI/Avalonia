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
        private int _index = -1;

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

        protected override bool EvaluateIsActive()
        {
            var index = _index >= 0 ? _index : _provider.GetChildIndex(_control);
            return NthChildSelector.Evaluate(index, _provider, _step, _offset, _reversed).IsMatch;
        }

        protected override void Initialize()
        {
            _provider.ChildIndexChanged += ChildIndexChanged;
            _provider.TotalCountChanged += TotalCountChanged;
        }

        protected override void Deinitialize()
        {
            _provider.ChildIndexChanged -= ChildIndexChanged;
        }

        private void ChildIndexChanged(object? sender, ChildIndexChangedEventArgs e)
        {
            // Run matching again if:
            // 1. e.Child is null, when all children indices were changed.
            // 2. Subscribed child index was changed.
            if (e.Child is null || e.Child == _control)
            {
                _index = e.Index;
                ReevaluateIsActive();
            }
        }

        private void TotalCountChanged(object? sender, EventArgs e)
        {
            if (_reversed)
                ReevaluateIsActive();
        }
    }
}
