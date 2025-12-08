using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    internal sealed class FramebufferManager : IFramebufferPlatformSurface
    {
        private readonly TopLevelImpl _topLevel;

        public FramebufferManager(TopLevelImpl topLevel)
        {
            _topLevel = topLevel;
        }

        public ILockedFramebuffer Lock() => new AndroidFramebuffer(
            _topLevel.InternalView ?? throw new InvalidOperationException("TopLevel.InternalView was not expected to be null."),
            _topLevel.RenderScaling);

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
    }
}
