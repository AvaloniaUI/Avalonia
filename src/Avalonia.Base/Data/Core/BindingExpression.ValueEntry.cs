using System;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Data.Core;

internal partial class BindingExpression : IValueEntry, ISetterInstance
{
    bool IValueEntry.HasValue
    {
        get
        {
            Start(produceValue: false);
            return _value is not null;
        }
    }

    AvaloniaProperty IValueEntry.Property => _targetProperty ?? throw new Exception();

    bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
    {
        GetDataValidationState(out state, out error);
        return IsDataValidationEnabled;
    }

    object? IValueEntry.GetValue()
    {
        Start(produceValue: false);
        return GetValueOrDefault();        
    }
    
    void IValueEntry.Unsubscribe() => Stop();
}
