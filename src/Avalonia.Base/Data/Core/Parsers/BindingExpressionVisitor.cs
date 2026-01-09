using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;

namespace Avalonia.Data.Core.Parsers;

/// <summary>
/// Visits and processes a LINQ expression to build a chain of binding expression nodes.
/// </summary>
/// <typeparam name="TIn">The input parameter type for the binding expression.</typeparam>
/// <remarks>
/// This visitor traverses lambda expressions used in compiled bindings and converts them into a
/// list of <see cref="ExpressionNode"/> instances that represent the binding path. It supports
/// property access, indexers, AvaloniaProperty access, and stream bindings.
/// </remarks>
[RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class BindingExpressionVisitor<TIn>(LambdaExpression expression) : ExpressionVisitor
{
    private const string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private static readonly PropertyInfo s_avaloniaObjectIndexer;
    private static readonly MethodInfo s_createDelegateMethod;
    private readonly LambdaExpression _rootExpression = expression;
    private readonly List<ExpressionNode> _nodes = [];
    private Expression? _head;

    static BindingExpressionVisitor()
    {
        s_avaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", [typeof(AvaloniaProperty)])!;
        s_createDelegateMethod = typeof(MethodInfo).GetMethod("CreateDelegate", [typeof(Type), typeof(object)])!;
    }

    /// <summary>
    /// Builds a list of binding expression nodes from a lambda expression.
    /// </summary>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="expression">
    /// The lambda expression to parse and convert into expression nodes.
    /// </param>
    /// <returns>
    /// A list of <see cref="ExpressionNode"/> instances representing the binding path, ordered
    /// from the root to the target property.
    /// </returns>
    /// <exception cref="ExpressionParseException">
    /// Thrown when the expression contains unsupported operations or invalid syntax for binding
    /// expressions.
    /// </exception>
    public static List<ExpressionNode> BuildNodes<TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var visitor = new BindingExpressionVisitor<TIn>(expression);
        visitor.Visit(expression);
        return visitor._nodes;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Indexers require more work since the compiler doesn't generate IndexExpressions:
        // they weren't in System.Linq.Expressions v1 and so must be generated manually.
        if (node.NodeType == ExpressionType.ArrayIndex)
            return Visit(Expression.MakeIndex(node.Left, null, [node.Right]));

        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Indexer == s_avaloniaObjectIndexer)
        {
            var property = GetValue<AvaloniaProperty>(node.Arguments[0]);
            return Add(node.Object, node, new AvaloniaPropertyAccessorNode(property));
        }
        else
        {
            return Add(node.Object, node, new ExpressionTreeIndexerNode(node));
        }
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return node.Member.MemberType switch
        {
            MemberTypes.Property => AddPropertyNode(node),
            _ => throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}."),
        };
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
            var expression = Expression.MakeIndex(node.Object, null, node.Arguments);
            return Add(node.Object, node, new ExpressionTreeIndexerNode(expression));
        }
        else if (method.Name.StartsWith(StreamBindingExtensions.StreamBindingName) &&
                 method.DeclaringType == typeof(StreamBindingExtensions))
        {
            var instance = node.Method.IsStatic ? node.Arguments[0] : node.Object;
            Add(instance, node, new DynamicPluginStreamNode());
            return node;
        }
        else if (method == s_createDelegateMethod)
        {
            var accessor = new DynamicPluginPropertyAccessorNode(GetValue<MethodInfo>(node.Object!).Name, acceptsNull: false);
            return Add(node.Arguments[1], node, accessor);
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
            return Add(node.Operand, node, new LogicalNotNode());
        }
        else if (node.NodeType == ExpressionType.Convert)
        {
            if (node.Type.IsAssignableFrom(node.Operand.Type))
            {
                // Ignore inheritance casts (upcasts from derived to base)
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
        throw new ExpressionParseException(0, "Catch blocks are not allowed in binding expressions.");
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new ExpressionParseException(0, "Dynamic expressions are not allowed in binding expressions.");
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        throw new ExpressionParseException(0, "Element init expressions are not valid in a binding expression.");
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        throw new ExpressionParseException(0, "Goto expressions are not supported in binding expressions.");
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
        throw new ExpressionParseException(0, "Member assignments not supported in binding expressions.");
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

    private Expression Add(Expression? instance, Expression expression, ExpressionNode node)
    {
        var visited = Visit(instance);
        
        if (visited != _head)
        {
            throw new ExpressionParseException(
                0, 
                $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{visited}'.");
        }

        _nodes.Add(node);
        return _head = expression;
    }

    private Expression AddPropertyNode(MemberExpression property)
    {
        var node = new DynamicPluginPropertyAccessorNode(property.Member.Name, acceptsNull: false);
        return Add(property.Expression, property, node);
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
