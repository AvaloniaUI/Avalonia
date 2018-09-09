using System;
using Avalonia.Data.Core;
using Avalonia.LogicalTree;

namespace Avalonia.Markup.Parsers.Nodes
{
    internal class ElementNameNode : ExpressionNode
    {
        private readonly string _name;
        private IDisposable _subscription;

        public ElementNameNode(string name)
        {
            _name = name;
        }

        public override string Description => $"#{_name}";

        protected override void StartListeningCore(WeakReference reference)
        {
            if (reference.Target is ILogical logical)
            {
                _subscription = ControlLocator.Track(logical, _name).Subscribe(ValueChanged);
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
