using System;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class StreamNode : ExpressionNode, IObserver<object?>
{
    private readonly bool _acceptsNull;
    private IStreamPlugin _plugin;
    private IDisposable? _subscription;

    public StreamNode(IStreamPlugin plugin, bool acceptsNull)
    {
        _plugin = plugin;
        _acceptsNull = acceptsNull;
    }

    public override void BuildString(StringBuilder builder)
    {
        builder.Append('^');
    }

    void IObserver<object?>.OnCompleted() { }
    void IObserver<object?>.OnError(Exception error) { }
    void IObserver<object?>.OnNext(object? value) => SetValue(value);

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

        if (_plugin.Start(new(source)) is { } accessor)
        {
            _subscription = accessor.Subscribe(this);
        }
        else
        {
            ClearValue();
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
