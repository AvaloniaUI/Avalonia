using System;

namespace Avalonia.Data.Core;

/// <summary>
/// Creates property infos for compiled XAML from static accessor function pointers.
/// The generated accessor methods preserve the original property's dispatch behavior.
/// </summary>
public static class CompiledPropertyInfoFactory
{
    public static IPropertyInfo GetProperty<TValue>(
        ref IPropertyInfo? field, string name, IntPtr getter, IntPtr setter)
        => field ??= new ClrPropertyInfo(name, getter, setter, typeof(TValue));

    public static IPropertyInfo<TSource, TValue> GetTypedProperty<TSource, TValue>(
        ref IPropertyInfo<TSource, TValue>? field, string name, IntPtr getter, IntPtr setter)
        where TSource : class
        => field ??= new ClrPropertyInfo<TSource, TValue>(name, getter, setter);
}
