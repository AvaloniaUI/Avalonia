﻿using System;

namespace Avalonia.Data.Core;

internal class UntypedObservableBindingExpression : UntypedBindingExpressionBase, IObserver<object?>
{
    private readonly IObservable<object?> _observable;
    private IDisposable? _subscription;

    public UntypedObservableBindingExpression(
        IObservable<object?> observable,
        BindingPriority priority)
        : base(priority)
    {
        _observable = observable;
    }

    public override string Description => "Observable";

    protected override void StartCore()
    {
        _subscription = _observable.Subscribe(this);
    }

    protected override void StopCore()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    void IObserver<object?>.OnCompleted() { }
    void IObserver<object?>.OnError(Exception error) { }
    
    void IObserver<object?>.OnNext(object? value)
    {
        if (value is BindingNotification n)
        {
            var v = n.Value;
            var e = n.Error is not null ? new BindingError(n.Error, n.ErrorType) : null;
            PublishValue(v, e);
        }
        else
        {
            PublishValue(value);
        }
    }
}
