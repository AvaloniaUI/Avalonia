// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;

namespace Perspex.Cairo.Media.Imaging
{
    using Cairo = global::Cairo;

    public class BitmapImpl : IBitmapImpl
    {
        public BitmapImpl(Cairo.ImageSurface surface)
        {
            Surface = surface;
        }

        public int PixelWidth
        {
            get { return Surface.Width; }
        }

        public int PixelHeight
        {
            get { return Surface.Height; }
        }

        public Cairo.ImageSurface Surface
        {
            get; }

        public void Save(string fileName)
        {
            Surface.WriteToPng(fileName);
        }
    }
}
