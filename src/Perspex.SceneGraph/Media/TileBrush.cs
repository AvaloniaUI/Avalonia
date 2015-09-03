// -----------------------------------------------------------------------
// <copyright file="TileBrush.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Base class for brushes which display repeating images.
    /// </summary>
    public abstract class TileBrush : Brush
    {
        /// <summary>
        /// Defines the <see cref="AlignmentX"/> property.
        /// </summary>
        public static readonly PerspexProperty<AlignmentX> AlignmentXProperty =
            PerspexProperty.Register<TileBrush, AlignmentX>("ALignmentX", AlignmentX.Center);

        /// <summary>
        /// Defines the <see cref="AlignmentY"/> property.
        /// </summary>
        public static readonly PerspexProperty<AlignmentY> AlignmentYProperty =
            PerspexProperty.Register<TileBrush, AlignmentY>("ALignmentY", AlignmentY.Center);

        /// <summary>
        /// Defines the <see cref="DestinationRect"/> property.
        /// </summary>
        public static readonly PerspexProperty<RelativeRect> DestinationRectProperty =
            PerspexProperty.Register<TileBrush, RelativeRect>("DestinationRect", RelativeRect.Fill);

        /// <summary>
        /// Defines the <see cref="SourceRect"/> property.
        /// </summary>
        public static readonly PerspexProperty<RelativeRect> SourceRectProperty =
            PerspexProperty.Register<TileBrush, RelativeRect>("SourceRect", RelativeRect.Fill);

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly PerspexProperty<Stretch> StretchProperty =
            PerspexProperty.Register<TileBrush, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Gets or sets the horizontal alignment of a tile in the destination.
        /// </summary>
        public AlignmentX AlignmentX
        {
            get { return this.GetValue(AlignmentXProperty); }
            set { this.SetValue(AlignmentXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of a tile in the destination.
        /// </summary>
        public AlignmentY AlignmentY
        {
            get { return this.GetValue(AlignmentYProperty); }
            set { this.SetValue(AlignmentYProperty, value); }
        }

        /// <summary>
        /// Gets or sets the rectangle on the destination in which to paint a tile.
        /// </summary>
        public RelativeRect DestinationRect
        {
            get { return this.GetValue(DestinationRectProperty); }
            set { this.SetValue(DestinationRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the rectangle of the source image that will be displayed.
        /// </summary>
        public RelativeRect SourceRect
        {
            get { return this.GetValue(SourceRectProperty); }
            set { this.SetValue(SourceRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value controlling how the source rectangle will be stretched to fill
        /// the destination rect.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)this.GetValue(StretchProperty); }
            set { this.SetValue(StretchProperty, value); }
        }
    }
}
