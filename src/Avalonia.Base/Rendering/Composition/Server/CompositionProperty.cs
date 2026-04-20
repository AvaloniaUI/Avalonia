using System;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositionProperty
{
    private static int s_nextId = 1;
    private static readonly object _lock = new();

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
            if (!s_dynamicRegistry.TryGetValue(typeof(TOwner), out var list))
                s_dynamicRegistry[typeof(TOwner)] = list = [];
            list.Add(prop);
        }

        return prop;
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
