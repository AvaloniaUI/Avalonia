// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;

namespace Perspex.Cairo.Media.Imaging
{
    using Cairo = global::Cairo;

    public class RenderTargetBitmapImpl : BitmapImpl, IRenderTargetBitmapImpl
    {
        public RenderTargetBitmapImpl(
            Cairo.ImageSurface surface)
            : base(surface)
        {
        }

        public void Dispose()
        {
            this.Surface.Dispose();
        }

        public void Render(IVisual visual)
        {
            Renderer renderer = new Renderer(this.Surface);
            renderer.Render(visual, new PlatformHandle(IntPtr.Zero, "RTB"));
        }
    }
}