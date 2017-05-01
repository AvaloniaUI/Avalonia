using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Rendering;

namespace Avalonia.UnitTests
{
    public class MockRendererFactory : IRendererFactory
    {
        private readonly Func<IRenderRoot, IRenderLoop, IRenderer> _cb;

        public MockRendererFactory(Func<IRenderRoot, IRenderLoop, IRenderer> cb = null)
        {
            _cb = cb;
        }

        public MockRendererFactory(IRenderer renderer) : this((_, __) => renderer)
        {

        }

        public IRenderer CreateRenderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            return _cb?.Invoke(root, renderLoop);
        }
    }
}
