using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

internal class CompiledBindingPathFromExpressionBuilder : ExpressionVisitor
{
    private static readonly PropertyInfo AvaloniaObjectIndexer;
    private static readonly MethodInfo CreateDelegateMethod;
    private static readonly string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private readonly bool _enableDataValidation;
    private readonly LambdaExpression _rootExpression;
    private readonly CompiledBindingPathBuilder _builder = new();
    private Expression? _head;

    public CompiledBindingPathFromExpressionBuilder(LambdaExpression expression, bool enableDataValidation)
    {
        _rootExpression = expression;
        _enableDataValidation = enableDataValidation;
    }

    static CompiledBindingPathFromExpressionBuilder()
    {
        AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", new[] { typeof(AvaloniaProperty) })!;
        CreateDelegateMethod = typeof(MethodInfo).GetMethod("CreateDelegate", new[] { typeof(Type), typeof(object) })!;
    }

    public static CompiledBindingPath Build<TIn, TOut>(Expression<Func<TIn, TOut>> expression, bool enableDataValidation)
    {
        var visitor = new CompiledBindingPathFromExpressionBuilder(expression, enableDataValidation);
        visitor.Visit(expression);
        return visitor._builder.Build();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Indexers require more work since the compiler doesn't generate IndexExpressions:
        // they weren't in System.Linq.Expressions v1 and so must be generated manually.
        if (node.NodeType == ExpressionType.ArrayIndex)
            return Visit(Expression.MakeIndex(node.Left, null, new[] { node.Right }));

        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Indexer == AvaloniaObjectIndexer)
        {
            var property = GetValue<AvaloniaProperty>(node.Arguments[0]);
            return Add(node.Object, node, x => x.Property(property, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor));
        }
        else if (node.Object?.Type.IsArray == true)
        {
            var indexes = node.Arguments.Select(GetValue<int>).ToArray();
            return Add(node.Object, node, x => x.ArrayElement(indexes, node.Type));
        }
        else if (node.Indexer?.GetMethod is not null && 
            node.Arguments.Count == 1 &&
            node.Arguments[0].Type == typeof(int))
        {
            var getMethod = node.Indexer.GetMethod;
            var setMethod = node.Indexer.SetMethod;
            var index = GetValue<int>(node.Arguments[0]);
            var info = new ClrPropertyInfo(
                CommonPropertyNames.IndexerName,
                x => getMethod.Invoke(x, new object[] { index }),
                setMethod is not null ? (o, v) => setMethod.Invoke(o, new[] { v }) : null,
                getMethod.ReturnType);
            return Add(node.Object, node, x => x.Property(
                info, 
                (x, i) => PropertyInfoAccessorFactory.CreateIndexerPropertyAccessor(x, i, index)));
        }
        else if (node.Indexer?.GetMethod is not null)
        {
            var getMethod = node.Indexer.GetMethod;
            var setMethod = node.Indexer?.SetMethod;
            var indexes = node.Arguments.Select(GetValue<object>).ToArray();
            var info = new ClrPropertyInfo(
                CommonPropertyNames.IndexerName,
                x => getMethod.Invoke(x, indexes),
                setMethod is not null ? (o, v) => setMethod.Invoke(o, indexes.Append(v).ToArray()) : null,
                getMethod.ReturnType);
            return Add(node.Object, node, x => x.Property(
                info,
                PropertyInfoAccessorFactory.CreateInpcPropertyAccessor));
        }

        throw new ExpressionParseException(0, $"Invalid indexer in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.MemberType != MemberTypes.Property)
            throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");

        if (typeof(AvaloniaObject).IsAssignableFrom(node.Expression?.Type) &&
            AvaloniaPropertyRegistry.Instance.FindRegistered(node.Expression.Type, node.Member.Name) is { } avaloniaProperty)
        {
            return Add(
                node.Expression, 
                node, 
                x => x.Property(avaloniaProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor));
        }
        else
        {
            var property = (PropertyInfo)node.Member;
            var info = new ClrPropertyInfo(
                property.Name,
                CreateGetter(property),
                CreateSetter(property),
                property.PropertyType);
            return Add(node.Expression, node, x => x.Property(info, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor));
        }
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;

        if (method.Name == IndexerGetterName && node.Object is not null)
        {
            var property = TryGetPropertyFromMethod(method);
            return Visit(Expression.MakeIndex(node.Object, property, node.Arguments));
        }
        else if (method.Name == MultiDimensionalArrayGetterMethodName &&
                 node.Object is not null)
        {
            var indexes = node.Arguments.Select(GetValue<int>).ToArray();
            return Add(node.Object, node, x => x.ArrayElement(indexes, node.Type));
        }
        else if (method.Name.StartsWith(StreamBindingExtensions.StreamBindingName) &&
                 method.DeclaringType == typeof(StreamBindingExtensions) &&
                 method.GetGenericArguments() is [Type genericArg])
        {
            var instance = node.Method.IsStatic ? node.Arguments[0] : node.Object;

            if (typeof(Task<>).MakeGenericType(genericArg).IsAssignableFrom(instance?.Type))
            {
                var builderMethod = typeof(CompiledBindingPathBuilder)
                    .GetMethod(nameof(CompiledBindingPathBuilder.StreamTask))!
                    .MakeGenericMethod(genericArg);
                return Add(instance, node, x => builderMethod.Invoke(x, null));
            }
            else if (typeof(IObservable<>).MakeGenericType(genericArg).IsAssignableFrom(instance?.Type))
            {
                var builderMethod = typeof(CompiledBindingPathBuilder)
                    .GetMethod(nameof(CompiledBindingPathBuilder.StreamObservable))!
                    .MakeGenericMethod(genericArg);
                return Add(instance, node, x => builderMethod.Invoke(x, null));
            }
        }

        throw new ExpressionParseException(0, $"Invalid method call in binding expression: '{node.Method.DeclaringType}.{node.Method.Name}'.");
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _rootExpression.Parameters[0] && _head is null)
            _head = node;
        return base.VisitParameter(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not && node.Type == typeof(bool))
        {
            return Add(node.Operand, node, x => x.Not());
        }
        else if (node.NodeType == ExpressionType.Convert)
        {
            if (node.Operand.Type.IsAssignableFrom(node.Type))
            {
                // Ignore inheritance casts 
                return _head = base.VisitUnary(node);
            }
        }
        else if (node.NodeType == ExpressionType.TypeAs)
        {
            // Ignore as operator.
            return _head = base.VisitUnary(node);
        }

        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        throw new ExpressionParseException(0, $"Catch blocks are not allowed in binding expressions.");
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new ExpressionParseException(0, $"Dynamic expressions are not allowed in binding expressions.");
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        throw new ExpressionParseException(0, $"Element init expressions are not valid in a binding expression.");
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        throw new ExpressionParseException(0, $"Goto expressions not supported in binding expressions.");
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        throw new ExpressionParseException(0, $"Member assignments not supported in binding expressions.");
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitTry(TryExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    private Expression Add(Expression? instance, Expression expression, Action<CompiledBindingPathBuilder> build)
    {
        var visited = Visit(instance);
        if (visited != _head)
            throw new ExpressionParseException(
                0,
                $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{visited}'.");
        build(_builder);
        return _head = expression;
    }

    private static Func<object, object>? CreateGetter(PropertyInfo info)
    {
        if (info.GetMethod == null)
            return null;
        var target = Expression.Parameter(typeof(object), "target");
        return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(Expression.Convert(target, info.DeclaringType!), info.GetMethod),
                    typeof(object)),
                target)
            .Compile();
    }

    private static Action<object, object?>? CreateSetter(PropertyInfo info)
    {
        if (info.SetMethod == null)
            return null;
        var target = Expression.Parameter(typeof(object), "target");
        var value = Expression.Parameter(typeof(object), "value");
        return Expression.Lambda<Action<object, object?>>(
                Expression.Call(Expression.Convert(target, info.DeclaringType!), info.SetMethod,
                    Expression.Convert(value, info.SetMethod.GetParameters()[0].ParameterType)),
                target, value)
            .Compile();
    }

    private static T GetValue<T>(Expression expr)
    {
        if (expr is ConstantExpression constant)
            return (T)constant.Value!;
        return Expression.Lambda<Func<T>>(expr).Compile(preferInterpretation: true)();
    }

    private static PropertyInfo? TryGetPropertyFromMethod(MethodInfo method)
    {
        var type = method.DeclaringType;
        return type?.GetRuntimeProperties().FirstOrDefault(prop => prop.GetMethod == method);
    }
}
