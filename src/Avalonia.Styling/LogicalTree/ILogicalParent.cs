namespace Avalonia.LogicalTree
{
    internal interface ILogicalParent
    {
        void AddChild(ILogical child);

        void RemoveChild(ILogical child);
    }
}
