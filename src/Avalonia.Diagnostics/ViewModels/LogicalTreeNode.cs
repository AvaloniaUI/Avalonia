// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class LogicalTreeNode : TreeNode
    {
        public LogicalTreeNode(ILogical logical, TreeNode parent)
            : base((Control)logical, parent)
        {
            Children = logical.LogicalChildren.CreateDerivedList(x => new LogicalTreeNode(x, this));
        }

        public static LogicalTreeNode[] Create(object control)
        {
            return control is ILogical logical ? new[] { new LogicalTreeNode(logical, null) } : null;
        }
    }
}
