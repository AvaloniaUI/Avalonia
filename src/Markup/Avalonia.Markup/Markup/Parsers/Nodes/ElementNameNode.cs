using System;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.LogicalTree;

namespace Avalonia.Markup.Parsers.Nodes
{
    internal class ElementNameNode : ExpressionNode
    {
        private readonly WeakReference<INameScope> _nameScope;
        private readonly string _name;
        private IDisposable _subscription;

        public ElementNameNode(INameScope nameScope, string name)
        {
            _nameScope = new WeakReference<INameScope>(nameScope);
            _name = name;
        }

        public override string Description => $"#{_name}";

        protected override void StartListeningCore(WeakReference reference)
        {
            if (_nameScope.TryGetTarget(out var scope))
                _subscription = NameScopeLocator.Track(scope, _name).Subscribe(ValueChanged);
            else
                _subscription = null;
        }

        protected override void StopListeningCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
