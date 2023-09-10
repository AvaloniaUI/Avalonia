using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class NamedElementNode : ExpressionNode
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

    protected override void OnSourceChanged(object source)
    {
        if (_nameScope.TryGetTarget(out var scope))
            _subscription = NameScopeLocator.Track(scope, _name).Subscribe(SetValue);
    }

    protected override void Unsubscribe(object oldSource)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
