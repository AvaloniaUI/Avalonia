// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;

namespace Perspex.Cairo.Media.Imaging
{
    using Cairo = global::Cairo;

    public class RenderTargetBitmapImpl : IRenderTargetBitmapImpl
    {
        public RenderTargetBitmapImpl(Cairo.ImageSurface surface)
        {
            Surface = surface;
            renderer = new Renderer(Surface);
        }

        public int PixelWidth => Surface.Width;

        public int PixelHeight => Surface.Height;

        public void Dispose()
        {
            renderer.Dispose();
        }

        public Cairo.ImageSurface Surface
        {
            get;
        }

        private Renderer renderer;
        public void Render(IVisual visual)
        {
            renderer.Render(visual, new PlatformHandle(IntPtr.Zero, "RTB"));
        }

        public void Save(string fileName)
        {
            Surface.WriteToPng(fileName);
        }
    }
}