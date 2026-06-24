using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;

namespace Avalonia.Base.UnitTests.Data.Core.Parsers;

/// <summary>
/// Test extensions for BindingExpressionVisitor tests.
/// </summary>
internal static class BindingExpressionVisitorExtensions
{
    /// <summary>
    /// Builds a list of binding expression nodes from a lambda expression.
    /// This is a test helper method - production code should use BuildPath() instead.
    /// </summary>
    public static List<ExpressionNode> BuildNodes<TIn, TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var path = BindingExpressionVisitor<TIn>.BuildPath(expression);
        var nodes = new List<ExpressionNode>();
        path.BuildExpression(nodes, out var _);
        return nodes;
    }
}
