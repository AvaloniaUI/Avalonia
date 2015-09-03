// -----------------------------------------------------------------------
// <copyright file="VisualBrush.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IVisual"/>.
    /// </summary>
    public class VisualBrush : TileBrush
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly PerspexProperty<IVisual> VisualProperty =
            PerspexProperty.Register<VisualBrush, IVisual>("Visual");

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public VisualBrush(IVisual visual)
        {
            this.Visual = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public IVisual Visual
        {
            get { return this.GetValue(VisualProperty); }
            set { this.SetValue(VisualProperty, value); }
        }
    }
}
