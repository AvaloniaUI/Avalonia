using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Diagnostics.Controls.VirtualizedTreeView;

internal interface ITreeNode : INotifyPropertyChanged, INotifyCollectionChanged
{
    bool IsExpanded { get; set; }
    bool HasChildren { get; }
    IReadOnlyList<ITreeNode> Children { get; }
}
