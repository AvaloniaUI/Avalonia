using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class SkiaOptionsTests
    {
        // Stencil buffers let Skia choose multisample-based path rendering, which quantizes edge
        // coverage and visibly degraded vector geometry anti-aliasing on GPU backends in 12.1.0.
        // They are a performance opt-in, so anything other than an explicit `true` must avoid them.
        [Theory]
        [InlineData(null, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void Stencil_Buffers_Are_Avoided_Unless_Explicitly_Enabled(bool? useStencilBuffers, bool expected)
        {
            Assert.Equal(expected, SkiaOptions.ShouldAvoidStencilBuffers(useStencilBuffers));
        }
    }
}
