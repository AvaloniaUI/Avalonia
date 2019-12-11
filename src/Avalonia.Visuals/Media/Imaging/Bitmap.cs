// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a bitmap image.
    /// </summary>
    public class Bitmap : IBitmap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>
        public Bitmap(string fileName)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = RefCountable.Create(factory.LoadBitmap(fileName));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>
        public Bitmap(Stream stream)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = RefCountable.Create(factory.LoadBitmap(stream));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation.</param>
        public Bitmap(IRef<IBitmapImpl> impl)
        {
            PlatformImpl = impl.Clone();
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation. Bitmap class takes the ownership.</param>
        protected Bitmap(IBitmapImpl impl)
        {
            PlatformImpl = RefCountable.Create(impl);
        }
        
        /// <inheritdoc/>
        public virtual void Dispose()
        {
            PlatformImpl.Dispose();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="data">The pointer to the source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        public Bitmap(PixelFormat format, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            PlatformImpl = RefCountable.Create(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>()
                .LoadBitmap(format, data, size, dpi, stride));
        }

        /// <inheritdoc/>
        public Vector Dpi => PlatformImpl.Item.Dpi;

        /// <inheritdoc/>
        public Size Size => PlatformImpl.Item.PixelSize.ToSizeWithDpi(Dpi);

        /// <inheritdoc/>
        public PixelSize PixelSize => PlatformImpl.Item.PixelSize;

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        public IRef<IBitmapImpl> PlatformImpl { get; }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        public void Save(string fileName)
        {
            PlatformImpl.Item.Save(fileName);
        }

        public void Save(Stream stream)
        {
            PlatformImpl.Item.Save(stream);
        }
    }
}
