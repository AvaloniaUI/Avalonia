using System;

namespace Avalonia.Data.Core;

public class ClrPropertyInfo<TSource, TValue> : IPropertyInfo<TSource, TValue>
    where TSource : class
{
    private readonly Func<TSource, TValue>? _getter;
    private readonly Action<TSource, TValue>? _setter;

    public ClrPropertyInfo(string name, Func<TSource, TValue>? getter, Action<TSource, TValue>? setter)
    {
        _getter = getter;
        _setter = setter;
        PropertyType = typeof(TValue);
        Name = name;
    }

    public bool CanSet => _setter != null;
    public bool CanGet => _getter != null;
    public string Name { get; }
    public Type PropertyType { get; }

    public TValue Get(TSource target)
    {
        if (_getter == null)
            throw new NotSupportedException("Property " + Name + " doesn't have a getter");
        return _getter(target);
    }

    public void Set(TSource target, TValue value)
    {
        if (_setter == null)
            throw new NotSupportedException("Property " + Name + " doesn't have a setter");
        _setter(target, value);
    }

    object? IPropertyInfo.Get(object target) => Get((TSource)target);
    void IPropertyInfo.Set(object target, object? value) => Set((TSource)target, (TValue)value!);
}
