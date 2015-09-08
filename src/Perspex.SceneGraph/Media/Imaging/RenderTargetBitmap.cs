// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;
using Splat;

namespace Perspex.Media.Imaging
{
    /// <summary>
    /// A bitmap that holds the rendering of a <see cref="IVisual"/>.
    /// </summary>
    public class RenderTargetBitmap : Bitmap, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTargetBitmap"/> class.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        public RenderTargetBitmap(int width, int height)
            : base(CreateImpl(width, height))
        {
        }

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        public new IRenderTargetBitmapImpl PlatformImpl
        {
            get { return (IRenderTargetBitmapImpl)base.PlatformImpl; }
        }

        /// <summary>
        /// Renders an <see cref="IVisual"/> into the bitmap.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <remarks>
        /// Before calling this method, ensure that <paramref name="visual"/> has been measured.
        /// </remarks>
        public void Render(IVisual visual)
        {
            PlatformImpl.Render(visual);
        }

        /// <summary>
        /// Disposes of the bitmap.
        /// </summary>
        public void Dispose()
        {
            PlatformImpl.Dispose();
        }

        /// <summary>
        /// Creates a platform-specific imlementation for a <see cref="RenderTargetBitmap"/>.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <returns>The platform-specific implementation.</returns>
        private static IBitmapImpl CreateImpl(int width, int height)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            return factory.CreateRenderTargetBitmap(width, height);
        }
    }
}
