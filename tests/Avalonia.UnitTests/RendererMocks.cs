using Avalonia.Rendering;
using Moq;

namespace Avalonia.UnitTests
{
    public static class RendererMocks
    {
        public static Mock<IRenderer> CreateRenderer()
        {
            var renderer = new Mock<IRenderer>();
            renderer.SetupGet(r => r.Diagnostics).Returns(new RendererDiagnostics());
            return renderer;
        }
    }
}
