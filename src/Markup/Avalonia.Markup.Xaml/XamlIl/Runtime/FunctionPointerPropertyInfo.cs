using System;
using Avalonia.Data.Core;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

internal sealed unsafe class FunctionPointerPropertyInfo : IPropertyInfo
{
    private readonly delegate* managed<object, object?> _getter;
    private readonly delegate* managed<object, object?, void> _setter;
    // ALC unload keeps function pointers dangling (even managed ones), so we need to keep our compiler-generated
    // type referenced.
    private readonly Type _helperType;

    public FunctionPointerPropertyInfo(string name, delegate* managed<object, object?> getter,
        delegate* managed<object, object?, void> setter, Type propertyType, Type helperType)
    {
        Name = name;
        _getter = getter;
        _setter = setter;
        _helperType = helperType;
        PropertyType = propertyType;
    }

    public string Name { get; }
    public Type PropertyType { get; }
    public bool CanGet => _getter != null;
    public bool CanSet => _setter != null;

    public object? Get(object target)
    {
        if (_getter == null)
            throw new NotSupportedException("Property " + Name + " doesn't have a getter");
        return _getter(target);
    }

    public void Set(object target, object? value)
    {
        if (_setter == null)
            throw new NotSupportedException("Property " + Name + " doesn't have a setter");
        _setter(target, value);
    }
}

internal sealed unsafe class FunctionPointerPropertyInfo<TSource, TValue> : IPropertyInfo<TSource, TValue>
    where TSource : class
{
    private readonly delegate* managed<TSource, TValue> _getter;
    private readonly delegate* managed<TSource, TValue, void> _setter;
    private readonly Type _helperType;

    public FunctionPointerPropertyInfo(string name, delegate* managed<TSource, TValue> getter,
        delegate* managed<TSource, TValue, void> setter, Type helperType)
    {
        Name = name;
        _getter = getter;
        _setter = setter;
        _helperType = helperType;
    }

    public string Name { get; }
    public Type PropertyType => typeof(TValue);
    public bool CanGet => _getter != null;
    public bool CanSet => _setter != null;

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
