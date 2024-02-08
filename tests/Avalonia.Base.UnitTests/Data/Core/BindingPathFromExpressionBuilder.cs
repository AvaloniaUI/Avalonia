using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Avalonia.Data.Core;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

internal class BindingPathFromExpressionBuilder : ExpressionVisitor
{
    private static readonly PropertyInfo AvaloniaObjectIndexer;
    private static readonly string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private readonly LambdaExpression _rootExpression;
    private readonly StringBuilder _path = new();
    private Expression? _head;
    private int _negationCount;
    private TypeResolver? _resolver;

    public BindingPathFromExpressionBuilder(LambdaExpression expression)
    {
        _rootExpression = expression;
    }

    static BindingPathFromExpressionBuilder()
    {
        AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", new[] { typeof(AvaloniaProperty) })!;
    }

    public static (string, Func<string?, string, Type>?) Build<TIn, TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var visitor = new BindingPathFromExpressionBuilder(expression);
        visitor.Visit(expression);
        visitor._path.Insert(0, new string('!', visitor._negationCount));

        Func<string?, string, Type>? resolver = null;

        if (visitor._resolver is not null)
            resolver = visitor._resolver.Resolve;

        return (visitor._path.ToString(), resolver);
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
            var name = property.Name;

            if (property.IsAttached)
            {
                _resolver ??= new();
                _resolver.Add(property.OwnerType.Name, property.OwnerType);
                name = $"({property.OwnerType.Name}.{property.Name})";
            }

            return Add(node.Object, node, name, ".");
        }
        else
        {
            var indexes = string.Join(',', node.Arguments.Select(GetValue<object>));
            return Add(node.Object, node, $"[{indexes}]");
        }
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.MemberType != MemberTypes.Property)
            throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");

        var property = (PropertyInfo)node.Member;
        return Add(node.Expression, node, property.Name, ".");
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
            var indexes = string.Join(',', node.Arguments.Select(GetValue<int>));
            return Add(node.Object, node, $"[{indexes}]");
        }
        else if (method.Name.StartsWith(StreamBindingExtensions.StreamBindingName) &&
                 method.DeclaringType == typeof(StreamBindingExtensions) &&
                 method.GetGenericArguments() is [Type genericArg])
        {
            var instance = node.Method.IsStatic ? node.Arguments[0] : node.Object;
            return Add(instance, node, "^");
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
            ++_negationCount;
            return _head = base.VisitUnary(node);
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

    private Expression Add(Expression? instance, Expression expression, string pathSegment, string? separator = null)
    {
        var visited = Visit(instance);

        if (visited != _head)
            throw new ExpressionParseException(
                0,
                $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{visited}'.");

        if (_path.Length > 0 && !string.IsNullOrEmpty(separator))
            _path.Append(separator);

        _path.Append(pathSegment);
        return _head = expression;
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

    private class TypeResolver
    {
        private Dictionary<string, Type> _registered = new();
        public void Add(string name, Type type) => _registered.Add(name, type);
        public Type Resolve(string? ns, string name)
        {
            if (_registered.TryGetValue(name, out var type))
                return type;
            throw new Exception($"Unable to resolve type '{name}'.");
        }
    }
}
