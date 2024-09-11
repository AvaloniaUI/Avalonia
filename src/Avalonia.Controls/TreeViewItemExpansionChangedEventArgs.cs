using Avalonia.Interactivity;

namespace Avalonia.Controls;

/// <summary>
/// 
/// </summary>
public class TreeViewItemExpansionChangedEventArgs: RoutedEventArgs
{
    public TreeViewItem Source { get; }
    public bool IsExpanded { get; }
    
    public TreeViewItemExpansionChangedEventArgs(TreeViewItem source, bool isExpanded)
    {
        Source = source;
        IsExpanded = isExpanded;
    }
    
    public TreeViewItemExpansionChangedEventArgs(RoutedEvent routedEvent, TreeViewItem source, bool isExpanded): base(routedEvent)
    {
        Source = source;
        IsExpanded = isExpanded;
    }
}
