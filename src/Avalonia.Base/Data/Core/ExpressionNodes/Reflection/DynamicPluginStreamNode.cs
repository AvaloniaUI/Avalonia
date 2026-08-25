using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
[RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
internal sealed class DynamicPluginStreamNode : ExpressionNode
{
    private readonly bool _acceptsNull;
    private IDisposable? _subscription;

    public DynamicPluginStreamNode(bool acceptsNull)
    {
        _acceptsNull = acceptsNull;
    }

    override public void BuildString(StringBuilder builder)
    {
        builder.Append('^');
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (source is null)
        {
            if (_acceptsNull)
                SetValue(null);
            else
                ValidateNonNullSource(source);
            return;
        }

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
