





namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using Perspex.Controls;
    using ReactiveUI;

    internal class LogicalTreeNode : TreeNode
    {
        public LogicalTreeNode(ILogical logical)
            : base((Control)logical)
        {
            this.Children = logical.LogicalChildren.CreateDerivedCollection(x => new LogicalTreeNode(x));
        }

        public static LogicalTreeNode[] Create(object control)
        {
            var logical = control as ILogical;
            return logical != null ? new[] { new LogicalTreeNode(logical) } : null;
        }
    }
}
