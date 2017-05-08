// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ReactiveUI;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ReactiveObject
    {
        private TreeNode _selected;

        private readonly ObservableAsPropertyHelper<ControlDetailsViewModel> _details;

        public TreePageViewModel(TreeNode[] nodes)
        {
            Nodes = nodes;
            _details = this.WhenAnyValue(x => x.SelectedNode)
                .Select(x => x != null ? new ControlDetailsViewModel(x.Visual) : null)
                .ToProperty(this, x => x.Details);
        }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode SelectedNode
        {
            get { return _selected; }
            set { this.RaiseAndSetIfChanged(ref _selected, value); }
        }

        public ControlDetailsViewModel Details => _details.Value;

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
            else
            {
                foreach (var child in node.Children)
                {
                    var result = FindNode(child, control);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
