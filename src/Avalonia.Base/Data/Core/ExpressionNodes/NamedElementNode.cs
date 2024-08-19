using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class NamedElementNode : SourceNode
{
    private readonly WeakReference<INameScope?> _nameScope;
    private readonly string _name;
    private IDisposable? _subscription;

    public NamedElementNode(INameScope? nameScope, string name)
    {
        _nameScope = new(nameScope);
        _name = name;
    }

    public override void BuildString(StringBuilder builder)
    {
        builder.Append('#');
        builder.Append(_name);
    }

    public override bool ShouldLogErrors(object target)
    {
        // We don't log errors when the target element isn't rooted.
        return target is not ILogical logical || logical.IsAttachedToLogicalTree;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (_nameScope.TryGetTarget(out var scope))
            _subscription = NameScopeLocator.Track(scope, _name).Subscribe(SetValue);
        else
            SetError("NameScope not found.");
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
