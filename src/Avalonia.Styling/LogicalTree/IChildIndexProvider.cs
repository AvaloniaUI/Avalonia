#nullable enable
using System;

namespace Avalonia.LogicalTree
{
    public interface IChildIndexProvider
    {
        int GetChildIndex(ILogical child);

        int? TotalCount { get; }

        event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;
    }
}
