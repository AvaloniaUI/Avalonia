using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;

namespace Avalonia.Data.Core.Parsers;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class BindingExpressionVisitor<TIn> : ExpressionVisitor
{
    private static readonly PropertyInfo AvaloniaObjectIndexer;
    private static readonly MethodInfo CreateDelegateMethod;
    private static readonly string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private readonly bool _enableDataValidation;
    private readonly LambdaExpression _rootExpression;
    private readonly List<ExpressionNode> _nodes = new();
    private Expression? _head;

    public BindingExpressionVisitor(LambdaExpression expression, bool enableDataValidation)
    {
        _rootExpression = expression;
        _enableDataValidation = enableDataValidation;
    }

    static BindingExpressionVisitor()
    {
        AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", new[] { typeof(AvaloniaProperty) })!;
        CreateDelegateMethod = typeof(MethodInfo).GetMethod("CreateDelegate", new[] { typeof(Type), typeof(object) })!;
    }

    public static List<ExpressionNode> BuildNodes<TOut>(Expression<Func<TIn, TOut>> expression, bool enableDataValidation)
    {
        var visitor = new BindingExpressionVisitor<TIn>(expression, enableDataValidation);
        visitor.Visit(expression);
        return visitor._nodes;
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
            return Add(node.Object, node, new AvaloniaPropertyAccessorNode(property));
        }
        else
        {
            return Add(node.Object, node, new ExpressionTreeIndexerNode(node));
        }
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        switch (node.Member.MemberType)
        {
            case MemberTypes.Property:
                return Add(node.Expression, node, new DynamicPluginPropertyAccessorNode(node.Member.Name));
            default:
                throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
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
        else if (method == CreateDelegateMethod)
        {
            var accessor = new DynamicPluginPropertyAccessorNode(GetValue<MethodInfo>(node.Object!).Name);
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

    private Expression Add(Expression? instance, Expression expression, ExpressionNode node)
    {
        var visited = Visit(instance);
        if (visited != _head)
            throw new ExpressionParseException(
                0, 
                $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{visited}'.");
        _nodes.Add(node);
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
}
