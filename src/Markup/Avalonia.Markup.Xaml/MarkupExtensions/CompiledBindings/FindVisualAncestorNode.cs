using System;
using Avalonia.Data.Core;
using Avalonia.VisualTree;
using Avalonia.Reactive;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class FindVisualAncestorNode : ExpressionNode
    {
        private readonly int _level;
        private readonly Type? _ancestorType;
        private IDisposable? _subscription;

        public FindVisualAncestorNode(Type? ancestorType, int level)
        {
            _level = level;
            _ancestorType = ancestorType;
        }

        public override string Description
        {
            get
            {
                if (_ancestorType == null)
                {
                    return FormattableString.Invariant($"$visualparent[{_level}]");
                }
                else
                {
                    return FormattableString.Invariant($"$visualparent[{_ancestorType.Name}, {_level}]");
                }
            }
        }

        protected override void StartListeningCore(WeakReference<object?> reference)
        {
            if (reference.TryGetTarget(out object? target) && target is Visual visual)
            {
                _subscription = VisualLocator.Track(visual, _level, _ancestorType).Subscribe(ValueChanged);
            }
            else
            {
                _subscription = null;
            }
        }

        protected override void StopListeningCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
