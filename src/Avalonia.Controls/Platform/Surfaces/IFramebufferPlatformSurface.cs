using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform.Surfaces
{
    [Unstable]
    public interface IFramebufferPlatformSurface
    {
        IFramebufferRenderTarget CreateFramebufferRenderTarget();
    }

    [Unstable]
    public interface IFramebufferRenderTarget : IDisposable
    {
        /// <summary>
        /// Provides a framebuffer descriptor for drawing.
        /// </summary>
        /// <remarks>
        /// Contents should be drawn on actual window after disposing
        /// </remarks>
        ILockedFramebuffer Lock();
    }
    
    [PrivateApi]
    public interface IFramebufferRenderTargetWithProperties : IFramebufferRenderTarget
    {
        /// <summary>
        /// Provides a framebuffer descriptor for drawing.
        /// </summary>
        /// <remarks>
        /// Contents should be drawn on actual window after disposing
        /// </remarks>
        ILockedFramebuffer Lock(out FramebufferLockProperties properties);
        
        bool RetainsFrameContents { get; }
    }
    
    [PrivateApi]
    public record struct FramebufferLockProperties(bool PreviousFrameIsRetained);

    /// <summary>
    /// For simple cases when framebuffer is always available
    /// </summary>
    public class FuncFramebufferRenderTarget : IFramebufferRenderTarget
    {
        private readonly Func<ILockedFramebuffer> _lockFramebuffer;

        public FuncFramebufferRenderTarget(Func<ILockedFramebuffer> lockFramebuffer)
        {
            _lockFramebuffer = lockFramebuffer;
        }
        
        public void Dispose()
        {
            // No-op
        }

        public ILockedFramebuffer Lock() => _lockFramebuffer();
    }
    
    internal class FuncRetainedFramebufferRenderTarget : IFramebufferRenderTargetWithProperties
    {
        public delegate ILockedFramebuffer LockDelegate(out FramebufferLockProperties properties);
        private readonly LockDelegate _lockFramebuffer;

        public FuncRetainedFramebufferRenderTarget(LockDelegate lockFramebuffer)
        {
            _lockFramebuffer = lockFramebuffer;
        }
        
        public void Dispose()
        {
            // No-op
        }

        public ILockedFramebuffer Lock() => _lockFramebuffer(out _);
        
        public ILockedFramebuffer Lock(out FramebufferLockProperties properties)
        {
            return _lockFramebuffer(out properties);
        }

        public bool RetainsFrameContents => true;
    }
}
