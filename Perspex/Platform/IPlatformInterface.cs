// -----------------------------------------------------------------------
// <copyright file="IPlatformFactory.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    public interface IPlatformInterface
    {
        IBitmapImpl CreateBitmap(int width, int height);

        IStreamGeometryImpl CreateStreamGeometry();

        IRenderer CreateRenderer(IntPtr handle, double width, double height);

        IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height);

        ITextService GetTextService();
    }
}
