// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ViewModelBase, IDevToolViewModel
    {
        private TreeNode _selected;
        private ControlDetailsViewModel _details;

        public TreePageViewModel(TreeNode[] nodes, string name)
        {
            Nodes = nodes;
            Name = name;
        }

        public string Name { get; }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode SelectedNode
        {
            get => _selected;
            set
            {
                if (RaiseAndSetIfChanged(ref _selected, value))
                {
                    Details = value != null ? new ControlDetailsViewModel(value.Visual) : null;
                }
            }
        }

        public ControlDetailsViewModel Details
        {
            get => _details;
            private set => RaiseAndSetIfChanged(ref _details, value);
        }

        public TreeNode FindNode(IControl control)
        {
            foreach (var node in Nodes)
            {
                var result = FindNode(node, control);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public void SelectControl(IControl control)
        {
            var node = default(TreeNode);

            while (node == null && control != null)
            {
                node = FindNode(control);

                if (node == null)
                {
                    control = control.GetVisualParent<IControl>();
                }
            }

            if (node != null)
            {
                SelectedNode = node;
                ExpandNode(node.Parent);
            }
        }

        private void ExpandNode(TreeNode node)
        {
            if (node != null)
            {
                node.IsExpanded = true;
                ExpandNode(node.Parent);
            }
        }

        private TreeNode FindNode(TreeNode node, IControl control)
        {
            if (node.Visual == control)
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                var result = FindNode(child, control);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
