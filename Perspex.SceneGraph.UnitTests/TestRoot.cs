// -----------------------------------------------------------------------
// <copyright file="TestVisual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.SceneGraph.UnitTests
{
    using System;
    using Perspex.Platform;
    using Perspex.Rendering;

    public class TestRoot : TestVisual, IRenderRoot
    {
        public IRenderer Renderer
        {
            get { throw new NotImplementedException(); }
        }

        public IRenderManager RenderManager
        {
            get { throw new NotImplementedException(); }
        }
    }
}
