using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Avalonia.Data.Core.ExpressionNodes;

namespace Avalonia.Data.Core;

/// <summary>
/// Represents a path in a <see cref="CompiledBinding"/>.
/// </summary>
public partial class BindingPath
{
    private List<ExpressionNode>? _nodes;
    private bool _nodesNeedClone;

    internal BindingPath() { }
    internal BindingPath(List<ExpressionNode> nodes) => _nodes = nodes;

    private bool IsRooted => _nodes is not null && _nodes.Count > 0 && _nodes[0] is SourceNode;

    internal List<ExpressionNode>? CreateExpressionNodes(object? source)
    {
        if (source == AvaloniaProperty.UnsetValue && !IsRooted)
            (_nodes ??= []).Insert(0, new DataContextNode());

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
