// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia platform initializer.
    /// </summary>
    public static class SkiaPlatform
    {
        /// <summary>
        /// Initialize Skia platform.
        /// </summary>
        public static void Initialize()
        {
            var renderInterface = new PlatformRenderInterface();
            
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface);
        }

        /// <summary>
        /// Default DPI.
        /// </summary>
        public static Vector DefaultDpi => new Vector(96.0f, 96.0f);

        /// <summary>
        /// Creates a ForeignBitmap from SKImage
        /// </summary>
        /// <param name="image">SKImage with bitmap contents</param>
        /// <param name="dpi">Image DPI (e. g. 96:96)</param>
        /// <param name="ownsImage">If this parameter is true, SKImage will be disposed with the ForeignBitmap instance</param>
        /// <returns>ForeignBitmap instance associated with <see cref="image"/></returns>
        public static ForeignBitmap CreateForeignBitmap(SKImage image, Vector dpi, bool ownsImage)
        {
            return new ForeignBitmap(new ForeignBitmapImpl(image, dpi, ownsImage));
        }
    }
}
