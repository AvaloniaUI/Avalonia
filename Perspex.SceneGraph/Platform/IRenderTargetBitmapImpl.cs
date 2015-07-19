// -----------------------------------------------------------------------
// <copyright file="IRenderTargetBitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    public interface IRenderTargetBitmapImpl : IBitmapImpl, IDisposable
    {
        void Render(IVisual visual);
    }
}
