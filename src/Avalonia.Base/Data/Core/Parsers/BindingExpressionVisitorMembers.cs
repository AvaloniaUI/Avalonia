using System;
using System.Reflection;

namespace Avalonia.Data.Core.Parsers;

/// <summary>
/// Stores reflection members used by <see cref="BindingExpressionVisitor{TIn}"/> outside of the
/// generic class to avoid duplication for each generic instantiation.
/// </summary>
internal static class BindingExpressionVisitorMembers
{
    static BindingExpressionVisitorMembers()
    {
        AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty(CommonPropertyNames.IndexerName, [typeof(AvaloniaProperty)])!;
        CreateDelegateMethod = typeof(MethodInfo).GetMethod(nameof(MethodInfo.CreateDelegate), [typeof(Type), typeof(object)])!;
    }

    public static readonly PropertyInfo AvaloniaObjectIndexer;
    public static readonly MethodInfo CreateDelegateMethod;
}
