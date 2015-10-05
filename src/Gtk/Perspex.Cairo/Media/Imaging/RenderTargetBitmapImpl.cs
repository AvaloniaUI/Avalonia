// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Media;
using Perspex.Platform;
using Perspex.Rendering;

namespace Perspex.Cairo.Media.Imaging
{
    using Cairo = global::Cairo;

    public class RenderTargetBitmapImpl : IRenderTargetBitmapImpl
    {

        private readonly RenderTarget _renderTarget;
        public RenderTargetBitmapImpl(Cairo.ImageSurface surface)
        {
            Surface = surface;
            _renderTarget = new RenderTarget(Surface);
        }

        public int PixelWidth => Surface.Width;

        public int PixelHeight => Surface.Height;

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public Cairo.ImageSurface Surface
        {
            get;
        }

        public void Save(string fileName)
        {
            Surface.WriteToPng(fileName);
        }

        public Perspex.Media.DrawingContext CreateDrawingContext()
        {
            return _renderTarget.CreateDrawingContext();
        }

        void IRenderTarget.Resize(int width, int height)
        {
            throw new NotSupportedException();
        }
    }
}