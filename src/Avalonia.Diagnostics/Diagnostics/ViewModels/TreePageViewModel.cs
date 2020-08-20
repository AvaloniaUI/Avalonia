using System;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ViewModelBase, IDisposable
    {
        private TreeNode _selectedNode;
        private ControlDetailsViewModel _details;
        private string _propertyFilter = string.Empty;
        private bool _useRegexFilter;

        public TreePageViewModel(TreeNode[] nodes)
        {
            Nodes = nodes;
            Selection = new SelectionModel
            { 
                SingleSelect = true,
                Source = Nodes 
            };

            Selection.SelectionChanged += (s, e) =>
            {
                SelectedNode = (TreeNode)Selection.SelectedItem;
            };
       }

        public TreeNode[] Nodes { get; protected set; }

        public SelectionModel Selection { get; }

        public TreeNode SelectedNode
        {
            get => _selectedNode;
            private set
            {
                if (RaiseAndSetIfChanged(ref _selectedNode, value))
                {
                    Details = value != null ?
                        new ControlDetailsViewModel(this, value.Visual) :
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

        public string PropertyFilter
        {
            get => _propertyFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _propertyFilter, value))
                {
                    Details.PropertiesView.Refresh();
                }
            }
        }

        public bool UseRegexFilter
        {
            get => _useRegexFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _useRegexFilter, value))
                {
                    Details.PropertiesView.Refresh();
                }
            }
        }

        public void Dispose()
        {
            foreach (var node in Nodes)
            {
                node.Dispose();
            }

            _details?.Dispose();
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
                ExpandNode(node.Parent);
                Selection.SelectedIndex = node.Index;
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
