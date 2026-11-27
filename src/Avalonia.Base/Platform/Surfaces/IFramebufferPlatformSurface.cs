using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces
{
    [Unstable]
    public interface IFramebufferPlatformSurface : IPlatformRenderSurface
    {
        IFramebufferRenderTarget CreateFramebufferRenderTarget();
    }
    
    
    [PrivateApi]
    public interface IFramebufferRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
    {
        /// <summary>
        /// Provides a framebuffer descriptor for drawing.
        /// </summary>
        /// <remarks>
        /// Contents should be drawn on actual window after disposing
        /// </remarks>
        ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties);

        bool RetainsFrameContents => false;

        /// <summary>
        /// Indicates that <see cref="Lock"/> provides <see cref="FramebufferLockProperties.PartialPresentation"/>.
        /// </summary>
        bool SupportsPartialPresentation => false;
    }
    
    [PrivateApi]
    public record struct FramebufferLockProperties(bool PreviousFrameIsRetained)
    {
        /// <summary>
        /// Partial presentation of the locked framebuffer. The framebuffer is only presented on disposal
        /// if <see cref="IPartialPresentationFeature.Succeeded"/> was called.
        /// </summary>
        public IPartialPresentationFeature? PartialPresentation { get; init; }
    }

    /// <summary>
    /// For simple cases when framebuffer is always available
    /// </summary>
    public class FuncFramebufferRenderTarget : IFramebufferRenderTarget
    {
        public delegate ILockedFramebuffer LockFramebufferDelegate(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties);
        private readonly LockFramebufferDelegate _lockFramebuffer;

        public FuncFramebufferRenderTarget(Func<ILockedFramebuffer> lockFramebuffer) :
            this((_, out properties) =>
            {
                properties = default;
                return lockFramebuffer();
            })
        {

        }
        

        public FuncFramebufferRenderTarget(LockFramebufferDelegate lockFramebuffer, bool retainsFrameContents = false)
        {
            _lockFramebuffer = lockFramebuffer;
            RetainsFrameContents = retainsFrameContents;
        }
        
        public void Dispose()
        {
            // No-op
        }

        public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo,
            out FramebufferLockProperties properties) => _lockFramebuffer(sceneInfo, out properties);

        public bool RetainsFrameContents { get; }
    }
    
}
