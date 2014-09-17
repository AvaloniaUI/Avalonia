// -----------------------------------------------------------------------
// <copyright file="IRenderTargetBitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    public interface IRenderTargetBitmapImpl : IBitmapImpl
    {
        void Render(IVisual visual);
    }
}
