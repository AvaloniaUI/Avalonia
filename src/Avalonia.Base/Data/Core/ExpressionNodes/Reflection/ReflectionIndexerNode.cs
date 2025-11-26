using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Input;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
internal sealed class ReflectionIndexerNode : CollectionNodeBase, ISettableNode
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
    private MethodInfo? _getter;
    private MethodInfo? _setter;
    private object?[]? _indexes;

    public ReflectionIndexerNode(IList arguments)
    {
        Arguments = arguments;
    }

    public IList Arguments { get; }
    public Type? ValueType => _getter?.ReturnType;

    public override void BuildString(StringBuilder builder)
    {
        builder.Append('[');
        for (var i = 0; i < Arguments.Count; i++)
        {
            builder.Append(Arguments[i]);
            if (i != Arguments.Count - 1)
                builder.Append(',');
        }
        builder.Append(']');
    }

    public override ExpressionNode Clone() => new ReflectionIndexerNode(Arguments);

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (Source is null || _setter is null)
            return false;

        var args = new object?[_indexes!.Length + 1];
        _indexes.CopyTo(args, 0);
        args[_indexes.Length] = value;
        _setter.Invoke(Source, args);
        return true;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        _indexes = null;

        if (GetIndexer(source.GetType(), out _getter, out _setter))
        {
            var parameters = _getter.GetParameters();
            
            if (parameters.Length != Arguments.Count)
            {
                SetError($"Wrong number of arguments for indexer: expected {parameters.Length}, got {Arguments.Count}.");
                return;
            }
            
            _indexes = ConvertIndexes(parameters, Arguments);
            base.OnSourceChanged(source, dataValidationError);
        }
        else
        {
            SetError($"Type '{source.GetType()}' does not have an indexer.");
        }
    }

    protected override bool ShouldUpdate(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is null || e.PropertyName is null)
            return false;
        var typeInfo = sender.GetType().GetTypeInfo();
        return typeInfo.GetDeclaredProperty(e.PropertyName)?.GetIndexParameters().Any() ?? false;
    }

    protected override int? TryGetFirstArgumentAsInt()
    {
        if (TypeUtilities.TryConvert(typeof(int), Arguments[0], CultureInfo.InvariantCulture, out var value))
            return (int?)value;
        return null;
    }

    protected override void UpdateValue(object? source)
    {
        if (_getter is not null && _indexes is not null)
            SetValue(_getter.Invoke(source, _indexes));
        else
            ClearValue();
    }

    private static object?[] ConvertIndexes(ParameterInfo[] indexParameters, IList arguments)
    {
        var result = new List<object?>();

        for (var i = 0; i < indexParameters.Length; i++)
        {
            var type = indexParameters[i].ParameterType;
            var argument = arguments[i];

            if (TypeUtilities.TryConvert(type, argument, CultureInfo.InvariantCulture, out var value))
                result.Add(value);
            else
                throw new InvalidCastException(
                    $"Could not convert list index '{i}' of type '{argument}' to '{type}'.");
        }

        return result.ToArray();
    }

    private static bool GetIndexer(Type? type, [NotNullWhen(true)] out MethodInfo? getter, out MethodInfo? setter)
    {
        getter = setter = null;

        if (type is null)
            return false;

        if (type.IsArray)
        {
            getter = type.GetMethod("Get");
            setter = type.GetMethod("Set");
            return getter is not null;
        }

        for (; type != null; type = type.BaseType)
        {
            // check for the default indexer name first to make this faster.
            // this will only be false when a class in vb has a custom indexer name.
            if (type.GetProperty(CommonPropertyNames.IndexerName, InstanceFlags) is { } indexer)
            {
                getter = indexer.GetMethod;
                setter = indexer.SetMethod;
                return getter is not null;
            }

            foreach (var property in type.GetProperties(InstanceFlags))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    getter = property.GetMethod;
                    setter = property.SetMethod;
                    return getter is not null;
                }
            }
        }

        return false;
    }

}
