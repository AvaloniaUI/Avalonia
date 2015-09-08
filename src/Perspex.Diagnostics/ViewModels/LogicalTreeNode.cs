// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class LogicalTreeNode : TreeNode
    {
        public LogicalTreeNode(ILogical logical)
            : base((Control)logical)
        {
            Children = logical.LogicalChildren.CreateDerivedCollection(x => new LogicalTreeNode(x));
        }

        public static LogicalTreeNode[] Create(object control)
        {
            var logical = control as ILogical;
            return logical != null ? new[] { new LogicalTreeNode(logical) } : null;
        }
    }
}
