using System;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Controls.VirtualizedTreeView;

internal sealed class VirtualizedTreeListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new VirtualizedTreeViewItem();
    }

    protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<VirtualizedTreeViewItem>(item, out recycleKey);
    }
}
