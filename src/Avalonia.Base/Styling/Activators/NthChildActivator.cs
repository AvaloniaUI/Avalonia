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
        private int? _index;

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
            var index = _index ?? _provider.GetChildIndex(_control);
            return NthChildSelector.Evaluate(index, _provider, _step, _offset, _reversed).IsMatch;
        }

        protected override void Initialize()
        {
            _provider.ChildIndexChanged += ChildIndexChanged;
        }

        protected override void Deinitialize()
        {
            _provider.ChildIndexChanged -= ChildIndexChanged;
        }

        private void ChildIndexChanged(object? sender, ChildIndexChangedEventArgs e)
        {
            // Run matching again if:
            // 1. Subscribed child index was changed
            // 2. Child indexes were reset
            // 3. We're a reversed (nth-last-child) selector and total count has changed
            switch (e.Action)
            {
                // We're using the _index field to pass the index of the child to EvaluateIsActive
                // *only* when the active state is re-evaluated via this event handler. The docs
                // for EvaluateIsActive say:
                //
                // > This method should read directly from its inputs and not rely on any
                // > subscriptions to fire in order to be up-to-date.
                //
                // Which is good advice in general, however in this case we need to break the rule
                // and use the value from the event subscription instead of calling
                // IChildIndexProvider.GetChildIndex. This is because this event can be fired during
                // the process of realizing an element of a virtualized list; in this case calling
                // GetChildIndex may not return the correct index as the element isn't yet realized.
                case ChildIndexChangedAction.ChildIndexChanged when e.Child == _control:
                    _index = e.Index;
                    ReevaluateIsActive();
                    _index = null;
                    break;
                case ChildIndexChangedAction.ChildIndexesReset:
                case ChildIndexChangedAction.TotalCountChanged when _reversed:
                    _index = null;
                    ReevaluateIsActive();
                    break;
            }
        }
    }
}
