using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
#if NET8_0_OR_GREATER
[RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
#endif
internal sealed class DynamicPluginStreamNode : ExpressionNode
{
    private IDisposable? _subscription;

    override public void BuildString(StringBuilder builder)
    {
        builder.Append('^');
    }

    public override ExpressionNode Clone() => new DynamicPluginStreamNode();

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        var reference = new WeakReference<object?>(source);

        if (GetPlugin(reference) is { } plugin &&
            plugin.Start(reference) is { } accessor)
        {
            _subscription = accessor.Subscribe(SetValue);
        }
        else
        {
            SetValue(null);
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private static IStreamPlugin? GetPlugin(WeakReference<object?> source)
    {
        if (source is null)
            return null;

        foreach (var plugin in BindingPlugins.s_streamHandlers)
        {
            if (plugin.Match(source))
                return plugin;
        }

        return null;
    }
}
