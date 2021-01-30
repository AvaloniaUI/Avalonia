namespace Avalonia.LogicalTree
{
    public static class LogicalTreeHelper
    {
        public static void AddLogicalChild(IAvaloniaObject parent, IAvaloniaObject child)
        {
            if (parent is ILogicalParent logicalParent)
            {
                if (child is ILogical logical)
                {
                    logicalParent.AddChild(logical);
                }
            }
        }

        public static void RemoveLogicalChild(IAvaloniaObject parent, IAvaloniaObject child)
        {
            if (parent is ILogicalParent logicalParent)
            {
                if (child is ILogical logical)
                {
                    logicalParent.RemoveChild(logical);
                }
            }
        }
    }
}
