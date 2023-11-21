using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ViewModelBase, IDisposable
    {
        private TreeNode? _selectedNode;
        private ControlDetailsViewModel? _details;
        private readonly ISet<string> _pinnedProperties;

        public TreePageViewModel(MainViewModel mainView, TreeNode[] nodes, ISet<string> pinnedProperties)
        {
            MainView = mainView;
            Nodes = nodes;
            _pinnedProperties = pinnedProperties;
            PropertiesFilter = new FilterViewModel();
            PropertiesFilter.RefreshFilter += (s, e) => Details?.PropertiesView?.Refresh();

            SettersFilter = new FilterViewModel();
            SettersFilter.RefreshFilter += (s, e) => Details?.UpdateStyleFilters();
        }

        public event EventHandler<string>? ClipboardCopyRequested;
        
        public MainViewModel MainView { get; }

        public FilterViewModel PropertiesFilter { get; }

        public FilterViewModel SettersFilter { get; }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (RaiseAndSetIfChanged(ref _selectedNode, value))
                {
                    Details = value != null ?
                        new ControlDetailsViewModel(this, value.Visual, _pinnedProperties) :
                        null;
                    Details?.UpdatePropertiesView(MainView.ShowImplementedInterfaces);
                    Details?.UpdateStyleFilters();
                }
            }
        }

        public ControlDetailsViewModel? Details
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

        public void Dispose()
        {
            foreach (var node in Nodes)
            {
                node.Dispose();
            }

            _details?.Dispose();
        }

        public TreeNode? FindNode(Control control)
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

        public void SelectControl(Control control)
        {
            var node = default(TreeNode);
            Control? c = control;

            while (node == null && c != null)
            {
                node = FindNode(c);

                if (node == null)
                {
                    c = c.GetVisualParent<Control>();
                }
            }

            if (node != null)
            {
                SelectedNode = node;
                ExpandNode(node.Parent);
            }
        }

        public void CopySelector()
        {
            var currentVisual = SelectedNode?.Visual as Visual;
            if (currentVisual is not null)
            {
                var selector = GetVisualSelector(currentVisual);
                
                ClipboardCopyRequested?.Invoke(this, selector);
            }
        }
        
        public void CopySelectorFromTemplateParent()
        {
            var parts = new List<string>();

            var currentVisual = SelectedNode?.Visual as Visual;
            while (currentVisual is not null)
            {
                parts.Add(GetVisualSelector(currentVisual));
                
                currentVisual = currentVisual.TemplatedParent as Visual;
            }

            if (parts.Any())
            {
                parts.Reverse();
                var selector = string.Join(" /template/ ", parts);

                ClipboardCopyRequested?.Invoke(this, selector);
            }
        }

        public void ExpandRecursively()
        {
            if (SelectedNode is { } selectedNode)
            {
                ExpandNode(selectedNode);
                
                var stack = new Stack<TreeNode>();
                stack.Push(selectedNode);

                while (stack.Count > 0)
                {
                    var item = stack.Pop();
                    item.IsExpanded = true;
                    foreach (var child in item.Children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        public void CollapseChildren()
        {
            if (SelectedNode is { } selectedNode)
            {
                var stack = new Stack<TreeNode>();
                stack.Push(selectedNode);

                while (stack.Count > 0)
                {
                    var item = stack.Pop();
                    item.IsExpanded = false;
                    foreach (var child in item.Children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        public void CaptureNodeScreenshot()
        {
            MainView.Shot(null);
        }

        public void BringIntoView()
        {
            (SelectedNode?.Visual as Control)?.BringIntoView();
        }
        
        
        public void Focus()
        {
            (SelectedNode?.Visual as Control)?.Focus();
        }

        private static string GetVisualSelector(Visual visual)
        {
            var name = string.IsNullOrEmpty(visual.Name) ? "" : $"#{visual.Name}";
            var classes = string.Concat(visual.Classes
                .Where(c => !c.StartsWith(":"))
                .Select(c => '.' + c));
            var typeName = StyledElement.GetStyleKey(visual);

            return $"{typeName}{name}{classes}";
        } 

        private void ExpandNode(TreeNode? node)
        {
            if (node != null)
            {
                node.IsExpanded = true;
                ExpandNode(node.Parent);
            }
        }

        private TreeNode? FindNode(TreeNode node, Control control)
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

        internal void UpdatePropertiesView()
        {
            Details?.UpdatePropertiesView(MainView?.ShowImplementedInterfaces ?? true);
        }
    }
}
