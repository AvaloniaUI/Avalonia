




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

        public Point TranslatePointToScreen(Point p)
        {
            throw new NotImplementedException();
        }
    }
}
