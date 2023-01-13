using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core
{
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    public class StreamNode : ExpressionNode
    {
        private IStreamPlugin? _customPlugin = null;
        private IDisposable? _subscription;

        public override string Description => "^";

        public StreamNode() { }

        public StreamNode(IStreamPlugin customPlugin)
        {
            _customPlugin = customPlugin;
        }

        protected override void StartListeningCore(WeakReference<object?> reference)
        {
            _subscription = GetPlugin(reference)?.Start(reference).Subscribe(ValueChanged);
        }

        protected override void StopListeningCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private IStreamPlugin? GetPlugin(WeakReference<object?> reference)
        {
            if (_customPlugin != null)
            {
                return _customPlugin;
            }

            foreach (var plugin in ExpressionObserver.StreamHandlers)
            {
                if (plugin.Match(reference))
                {
                    return plugin;
                }
            }

            // TODO: Improve error
            ValueChanged(new BindingNotification(
                new MarkupBindingChainException("Stream operator applied to unsupported type", Description),
                BindingErrorType.Error));
            return null;
        }
    }
}
