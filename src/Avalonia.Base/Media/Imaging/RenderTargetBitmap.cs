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
    public class RenderTargetBitmap : Bitmap, IDisposable, IRenderTarget
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
        public new IRef<IRenderTargetBitmapImpl> PlatformImpl { get; }

        /// <summary>
        /// Renders a visual to the <see cref="RenderTargetBitmap"/>.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        public void Render(Visual visual) => ImmediateRenderer.Render(visual, this);

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

        /// <inheritdoc/>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer? vbr) => PlatformImpl.Item.CreateDrawingContext(vbr);

        /// <inheritdoc/>
        bool IRenderTarget.IsCorrupted => false;

        /// <summary>
        /// Overrides the <see cref="Bitmap"/> implementation 
        /// skipping the <see cref="Format"/> null check
        /// </summary>
        public override void CopyPixels(PixelRect sourceRect, nint buffer, int bufferSize, int stride)
        {
            // only check if we have a readable bitmap and if the formats are identical
            // this also allows for both formats being null
            if (PlatformImpl.Item is not IReadableBitmapImpl readable
                || Format != readable.Format
            )
                throw new NotSupportedException("CopyPixels is not supported for this bitmap type");

            using (var fb = readable.Lock())
                CopyPixelsCore(sourceRect, buffer, bufferSize, stride, fb);
        }

    }
}
