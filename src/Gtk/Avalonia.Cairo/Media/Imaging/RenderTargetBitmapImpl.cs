// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Cairo.Media.Imaging
{
    using System.IO;
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

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return _renderTarget.CreateDrawingContext(visualBrushRenderer);
        }

        public void Save(Stream stream)
        {
            var tempFileName = Path.GetTempFileName();
            Surface.WriteToPng(tempFileName);
            using (var tempFile = new FileStream(tempFileName, FileMode.Create))
            {
                tempFile.CopyTo(stream);
            }
        }
    }
}