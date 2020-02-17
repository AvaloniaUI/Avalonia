using System;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ViewModelBase, IDisposable
    {
        private TreeNode _selected;
        private ControlDetailsViewModel _details;
        private string _propertyFilter;

        public TreePageViewModel(TreeNode[] nodes)
        {
            Nodes = nodes;
        }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode SelectedNode
        {
            get => _selected;
            set
            {
                if (Details != null)
                {
                    _propertyFilter = Details.PropertyFilter;
                }

                if (RaiseAndSetIfChanged(ref _selected, value))
                {
                    Details = value != null ?
                        new ControlDetailsViewModel(value.Visual, _propertyFilter) :
                        null;
                }
            }
        }

        public ControlDetailsViewModel Details
        {
            get => _details;
            private set
            {
                var oldValue = _details;

                if (RaiseAndSetIfChanged(ref _details, value))
                {
                    oldValue?.Dispose();
                }
            }
        }

        public void Dispose() => _details?.Dispose();

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
