





namespace Perspex.Diagnostics.Views
{
    using Perspex.Controls;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Input;
    using Perspex.Media;

    internal class TreePage : UserControl
    {
        private Control adorner;

        protected void AddAdorner(object sender, PointerEventArgs e)
        {
            var node = (TreeNode)((Control)sender).DataContext;
            var layer = AdornerLayer.GetAdornerLayer(node.Control);

            if (layer != null)
            {
                this.adorner = new Rectangle
                {
                    Fill = new SolidColorBrush(0x80a0c5e8),
                    [AdornerLayer.AdornedElementProperty] = node.Control,
                };

                layer.Children.Add(this.adorner);
            }
        }

        protected void RemoveAdorner(object sender, PointerEventArgs e)
        {
            if (this.adorner != null)
            {
                ((Panel)this.adorner.Parent).Children.Remove(this.adorner);
                this.adorner = null;
            }
        }
    }
}
