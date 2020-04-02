using System;
using System.Reactive.Linq;

namespace Avalonia.Data.Core
{
    public class StreamNode : ExpressionNode
    {
        private IDisposable _subscription;

        public override string Description => "^";

        protected override void StartListeningCore(WeakReference<object> reference)
        {
            foreach (var plugin in ExpressionObserver.StreamHandlers)
            {
                if (plugin.Match(reference))
                {
                    _subscription = plugin.Start(reference).Subscribe(ValueChanged);
                    return;
                }
            }

            // TODO: Improve error.
            ValueChanged(new BindingNotification(
                new MarkupBindingChainException("Stream operator applied to unsupported type", Description),
                BindingErrorType.Error));
        }

        protected override void StopListeningCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
