using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositionProperty
{
    private static int s_nextId = 1;
    private static readonly object _lock = new();

    private static Dictionary<Type, List<CompositionProperty>> s_dynamicRegistry = new();

    class ReadOnlyRegistry : Dictionary<Type, IReadOnlyDictionary<string, CompositionProperty>>
    {

    }

    private static volatile ReadOnlyRegistry? s_ReadOnlyRegistry;

    public CompositionProperty(int id, string name, Type owner, Func<SimpleServerObject, ExpressionVariant>? getVariant)
    {
        Id = id;
        Name = name;
        Owner = owner;
        GetVariant = getVariant;
    }

    public int Id { get; }
    public string Name { get;  }
    public Type Owner { get; }
    public Func<SimpleServerObject, ExpressionVariant>? GetVariant { get; }

    public static CompositionProperty<TField> Register<TOwner, TField>(string name, Func<SimpleServerObject, TField> getField, Action<SimpleServerObject, TField> setField, 
        Func<SimpleServerObject, ExpressionVariant>? getVariant)
    {
        CompositionProperty<TField> prop;
        lock (_lock)
        {
            var id = s_nextId++;
            prop = new CompositionProperty<TField>(id, name, typeof(TOwner), getField, setField, getVariant);
        }

        s_ReadOnlyRegistry = null;
        return prop;
    }

    static void PopulatePropertiesForType(Type type, List<CompositionProperty> l)
    {
        Type? t = type;
        while (t != null && t != typeof(object))
        {
            if (s_dynamicRegistry.TryGetValue(t, out var lst))
                l.AddRange(lst);
            t = t.BaseType;
        }
    }

    static ReadOnlyRegistry Build()
    {
        var reg = new ReadOnlyRegistry();
        foreach (var type in s_dynamicRegistry.Keys)
        {
            var lst = new List<CompositionProperty>();
            PopulatePropertiesForType(type, lst);
            reg[type] = lst.ToDictionary(x => x.Name);
        }

        return reg;
    }
    
    public static IReadOnlyDictionary<string, CompositionProperty>? TryGetPropertiesForType(Type t)
    {
        GetRegistry().TryGetValue(t, out var rv);
        return rv;
    }

    public static CompositionProperty? Find(Type owner, string name)
    {
        if (TryGetPropertiesForType(owner)?.TryGetValue(name, out var prop) == true)
            return prop;
        return null;
    }

    static ReadOnlyRegistry GetRegistry()
    {
        var reg = s_ReadOnlyRegistry;
        if (reg != null)
            return reg;
        lock (_lock)
        {
            // ReSharper disable once NonAtomicCompoundOperator
            // This is the only line ever that would set the field to a not-null value, and we are inside of a lock
            return s_ReadOnlyRegistry ??= Build();
        }
    }
}

internal class CompositionProperty<T> : CompositionProperty
{
    public Func<SimpleServerObject, T> GetField { get; }
    public Action<SimpleServerObject, T> SetField { get; }

    public CompositionProperty(int id, string name, Type owner,
        Func<SimpleServerObject, T> getField,
        Action<SimpleServerObject, T> setField,
        Func<SimpleServerObject, ExpressionVariant>? getVariant)
        : base(id, name, owner, getVariant)
    {
        GetField = getField;
        SetField = setField;
    }
}