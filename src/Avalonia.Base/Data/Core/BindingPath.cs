using System;
using System.Collections.Generic;
using Avalonia.Data.Core.ExpressionNodes;

namespace Avalonia.Data.Core;

/// <summary>
/// Represents a path in a <see cref="CompiledBinding"/>.
/// </summary>
public partial class BindingPath
{
    private List<ExpressionNode>? _nodes;
    private readonly bool _isSelf;
    private bool _nodesNeedClone;

    internal BindingPath() { }

    internal BindingPath(bool isSelf, List<ExpressionNode>? nodes)
    {
        _isSelf = isSelf;
        _nodes = nodes;
    }

    private bool IsRooted => _isSelf || (_nodes?.Count > 0 && _nodes[0] is SourceNode);

    internal static List<ExpressionNode>? BuildExpressionNodes(
        BindingPath? path,
        object? source,
        AvaloniaProperty? targetProperty)
    {
        // If a path is provided, use the expression nodes from it.
        if (path is not null)
            return path.GetExpressionNodes(source, targetProperty);

        // If a source is provided, no nodes are needed as we simply select the source.
        if (source != AvaloniaProperty.UnsetValue)
            return null;

        // Otherwise we use the element's data context.
        return [new DataContextNode()];
    }

    private List<ExpressionNode>? GetExpressionNodes(object? source, AvaloniaProperty? targetProperty)
    {
        if (source == AvaloniaProperty.UnsetValue && !IsRooted)
        {
            ExpressionNode dataContextNode = targetProperty == StyledElement.DataContextProperty
                ? new ParentDataContextNode()
                : new DataContextNode();
            (_nodes ??= []).Insert(0, dataContextNode);
        }

        if (_nodesNeedClone)
        {
            // TODO: Is this ever needed? If so, implement cloning logic.
            throw new NotImplementedException();
        }
        else
        {
            _nodesNeedClone = true;
            return _nodes;
        }
    }
}
