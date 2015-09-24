// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Diagnostics.ViewModels;
using Perspex.Input;
using Perspex.Media;

namespace Perspex.Diagnostics.Views
{
    internal class TreePage : UserControl
    {
        private Control _adorner;

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
    }
}
