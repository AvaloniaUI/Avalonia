using System;
using System.Collections.Generic;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// An <see cref="ExpressionNode"/> that can write a value to its source.
/// </summary>
internal interface ISettableNode
{
    /// <summary>
    /// Gets the type of the value accepted by the node, or null if the node is not settable.
    /// </summary>
    Type? ValueType { get; }

    /// <summary>
    /// Tries to write the specified value to the source.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="nodes">The expression nodes in the binding.</param>
    /// <returns>True if the value was written sucessfully; otherwise false.</returns>
    bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes);
}
