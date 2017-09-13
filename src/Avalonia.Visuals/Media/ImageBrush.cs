// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IImage"/>.
    /// </summary>
    public class ImageBrush : TileBrush, IImageBrush, IMutableBrush
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<IImage> SourceProperty =
            AvaloniaProperty.Register<ImageBrush, IImage>(nameof(Source));
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        public ImageBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        /// <param name="source">The image to draw.</param>
        public ImageBrush(IImage source)
        {
            Source = source;
        }

        /// <summary>
        /// Gets or sets the image to draw.
        /// </summary>
        public IImage Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <inheritdoc/>
        IBrush IMutableBrush.ToImmutable()
        {
            return new Immutable.ImmutableImageBrush(this);
        }
    }
}
