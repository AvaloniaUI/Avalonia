namespace Avalonia.Diagnostics.Controls.VirtualizedTreeView;

internal class FlatTreeNode
{
    public ITreeNode Node { get; }
    public int Level { get; }
    
    public FlatTreeNode(ITreeNode node, int level)
    {
        Node = node;
        Level = level;
    }
}
