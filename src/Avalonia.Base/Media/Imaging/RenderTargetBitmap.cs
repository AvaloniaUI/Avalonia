using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// A bitmap that holds the rendering of a <see cref="Visual"/>.
    /// </summary>
    public class RenderTargetBitmap : Bitmap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTargetBitmap"/> class.
        /// </summary>
        /// <param name="pixelSize">The size of the bitmap.</param>
        public RenderTargetBitmap(PixelSize pixelSize)
           : this(pixelSize, new Vector(96, 96))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTargetBitmap"/> class.
        /// </summary>
        /// <param name="pixelSize">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        public RenderTargetBitmap(PixelSize pixelSize, Vector dpi)
           : this(RefCountable.Create(CreateImpl(pixelSize, dpi)))
        {
        }

        private RenderTargetBitmap(IRef<IRenderTargetBitmapImpl> impl) : base(impl)
        {
            PlatformImpl = impl;
        }

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        internal new IRef<IRenderTargetBitmapImpl> PlatformImpl { get; }

        /// <summary>
        /// Renders a visual to the <see cref="RenderTargetBitmap"/>.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        public void Render(Visual visual)
        {
            using (var ctx = CreateDrawingContext())
                ImmediateRenderer.Render(visual, ctx);
        }

        /// <summary>
        /// Creates a platform-specific implementation for a <see cref="RenderTargetBitmap"/>.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <returns>The platform-specific implementation.</returns>
        private static IRenderTargetBitmapImpl CreateImpl(PixelSize size, Vector dpi)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            return factory.CreateRenderTargetBitmap(size, dpi);
        }

        /// <summary>
        /// Creates a <see cref="DrawingContext"/> for drawing to the <see cref="RenderTargetBitmap"/>. 
        /// Clears the current image data to transparent.
        /// </summary>
        /// <returns>The drawing context.</returns>
        public DrawingContext CreateDrawingContext()
            => CreateDrawingContext(true);

        /// <summary>
        /// Creates a <see cref="DrawingContext"/> for drawing to the <see cref="RenderTargetBitmap"/>.
        /// </summary>
        /// <param name="clear">If true, clears the current image data to transparent, if false, leaves the image data unchanged.</param>
        /// <returns>The drawing context.</returns>
        public DrawingContext CreateDrawingContext(bool clear)
        {
            var platform = PlatformImpl.Item.CreateDrawingContext();
            if(clear)
                platform.Clear(Colors.Transparent);
            return new PlatformDrawingContext(platform);
        }

        public override void Dispose()
        {
            PlatformImpl.Dispose();
            base.Dispose();
        }
    }
}
