// -----------------------------------------------------------------------
// <copyright file="IPlatformFactory.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    public interface IPlatformFactory
    {
        IStreamGeometryImpl CreateStreamGeometry();

        IRenderer CreateRenderer(IntPtr handle, double width, double height);

        ITextService GetTextService();
    }
}
