// -----------------------------------------------------------------------
// <copyright file="IRenderRoot.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Rendering
{
    using Perspex.Platform;

    public interface IRenderRoot
    {
        IRenderer Renderer { get; }

        IRenderManager RenderManager { get; }
    }
}
