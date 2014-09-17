// -----------------------------------------------------------------------
// <copyright file="IRenderRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Rendering
{
    public interface IRenderRoot
    {
        IRenderManager RenderManager { get; }
    }
}
