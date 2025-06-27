using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core;

/// <summary>
/// Represents a path in a <see cref="CompiledBinding"/>.
/// </summary>
public partial class BindingPath
{
    private readonly List<ExpressionNode>? _nodes;
    private bool _nodesNeedClone;

    internal BindingPath() { }
    internal BindingPath(List<ExpressionNode> nodes) => _nodes = nodes;

    public static BindingPath Create<TSource, TValue>(Expression<Func<TSource, TValue>> expression)
    {
        throw new NotImplementedException();
    }

    internal List<ExpressionNode>? CreateExpressionNodes()
    {
        if (_nodesNeedClone)
        {
            throw new NotImplementedException();
        }
        else
        {
            _nodesNeedClone = true;
            return _nodes;
        }
    }
}
