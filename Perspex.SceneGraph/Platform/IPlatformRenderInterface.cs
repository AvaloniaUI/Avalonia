// -----------------------------------------------------------------------
// <copyright file="IPlatformRenderInterface.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using Perspex.Threading;

    public interface IPlatformRenderInterface
    {
        ITextService TextService { get; }

        IBitmapImpl CreateBitmap(int width, int height);

        IStreamGeometryImpl CreateStreamGeometry();

        IRenderer CreateRenderer(IPlatformHandle handle, double width, double height);

        IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height);

        IBitmapImpl LoadBitmap(string fileName);
    }
}
