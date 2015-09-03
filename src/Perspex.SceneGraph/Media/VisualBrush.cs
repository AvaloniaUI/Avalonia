// -----------------------------------------------------------------------
// <copyright file="VisualBrush.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public class VisualBrush : Brush
    {
        public static readonly PerspexProperty<IVisual> VisualProperty =
            PerspexProperty.Register<VisualBrush, IVisual>("Visual");

        public VisualBrush()
        {
        }

        public VisualBrush(IVisual visual)
        {
            this.Visual = visual;
        }

        public IVisual Visual
        {
            get { return this.GetValue(VisualProperty); }
            set { this.SetValue(VisualProperty, value); }
        }
    }
}
