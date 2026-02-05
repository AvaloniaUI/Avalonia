using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins;

internal class IndeiDataValidator(INotifyDataErrorInfo source, string memberName) : 
    MemberDataValidator(source), 
    IWeakEventSubscriber<DataErrorsChangedEventArgs>
{
    private static readonly WeakEvent<INotifyDataErrorInfo, DataErrorsChangedEventArgs>
        s_errorsChangedWeakEvent = WeakEvent.Register<INotifyDataErrorInfo, DataErrorsChangedEventArgs>(
            (s, h) => s.ErrorsChanged += h,
            (s, h) => s.ErrorsChanged -= h
        );
    private readonly string _memberName = memberName;

    public override bool RaisesEvents => true;

    public override Exception? GetDataValidationError()
    {
        return TryGetSource<INotifyDataErrorInfo>(out var source) ?
            GenerateException(source.GetErrors(_memberName)) : null;
    }

    void IWeakEventSubscriber<DataErrorsChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, DataErrorsChangedEventArgs e)
    {
        RaiseDataValidationChanged();
    }

    protected override void Subscribe(object source)
    {
        s_errorsChangedWeakEvent.Subscribe((INotifyDataErrorInfo)source, this);
    }

    protected override void Unsubscribe(object source)
    {
        s_errorsChangedWeakEvent.Unsubscribe((INotifyDataErrorInfo)source, this);
    }

    private static Exception? GenerateException(IEnumerable enumerable)
    {
        Exception? single = null;
        List<Exception>? multiple = null;

        foreach (var item in enumerable)
        {
            var ex = item as Exception ?? new DataValidationException(item.ToString());

            if (multiple is not null)
                multiple.Add(ex);
            else if (single is null)
                single = ex;
            else
                multiple = [single, ex];
        }

        if (multiple is not null)
            return new AggregateException(multiple);
        else
            return single;
    }
}
