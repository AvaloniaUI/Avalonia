using Perspex.Controls;
using Perspex.Controls.Generators;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Diagnostics.ViewModels;
using Perspex.Input;
using Perspex.Markup.Xaml;
using Perspex.Media;

namespace Perspex.Diagnostics.Views
{
    public class TreePageView : UserControl
    {
        private Control _adorner;
        private TreeView _tree;

        public TreePageView()
        {
            this.InitializeComponent();
            _tree.ItemContainerGenerator.Index.Materialized += TreeViewItemMaterialized;
        }

        protected void AddAdorner(object sender, PointerEventArgs e)
        {
            var node = (TreeNode)((Control)sender).DataContext;
            var layer = AdornerLayer.GetAdornerLayer(node.Control);

            if (layer != null)
            {
                _adorner = new Rectangle
                {
                    Fill = new SolidColorBrush(0x80a0c5e8),
                    [AdornerLayer.AdornedElementProperty] = node.Control,
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
            PerspexXamlLoader.Load(this);
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
