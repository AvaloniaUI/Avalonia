// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Views
{
    public class TreePageView : UserControl
    {
        private Control _adorner;
        private TreeView _tree;

        public TreePageView()
        {
            InitializeComponent();
            _tree.ItemContainerGenerator.Index.Materialized += TreeViewItemMaterialized;
        }

        protected void AddAdorner(object sender, PointerEventArgs e)
        {
            var node = (TreeNode)((Control)sender).DataContext;
            var layer = AdornerLayer.GetAdornerLayer(node.Visual);

            if (layer != null)
            {
                if (_adorner != null)
                {
                    ((Panel)_adorner.Parent).Children.Remove(_adorner);
                    _adorner = null;
                }

                _adorner = new Rectangle
                {
                    Fill = new SolidColorBrush(0x80a0c5e8),
                    [AdornerLayer.AdornedElementProperty] = node.Visual
                };

                layer.Children.Add(_adorner);
            }
        }

        protected void RemoveAdorner(object sender, PointerEventArgs e)
        {
            if (_adorner != null)
            {
                ((Panel)_adorner.Parent).Children.Remove(_adorner);
                _adorner = null;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _tree = this.FindControl<TreeView>("tree");
        }

        private void TreeViewItemMaterialized(object sender, ItemContainerEventArgs e)
        {
            var item = (TreeViewItem)e.Containers[0].ContainerControl;
            item.TemplateApplied += TreeViewItemTemplateApplied;
        }

        private void TreeViewItemTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            var header = item.HeaderPresenter.Child;
            header.PointerEnter += AddAdorner;
            header.PointerLeave += RemoveAdorner;
            item.TemplateApplied -= TreeViewItemTemplateApplied;
        }
    }
}
