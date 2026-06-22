using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.X11.XShmExtensions;

/// <summary>
/// Per-window XShm render target. Owns a small pool of reusable <see cref="X11ShmImage"/> buffers and
/// the in-flight counter used for backpressure. Completion events are routed back here by the shared
/// <see cref="X11DeferredDisplayDispatcher"/>.
/// </summary>
/// <remarks>
/// All members run on the rendering thread (the compositor render loop or, when rendering on the UI
/// thread, the UI thread). The same thread submits frames and drains their completions, so no locking is
/// required and the design is agnostic of which thread renders.
/// </remarks>
internal class X11ShmImageSwapchain : IFramebufferRenderTarget
{
    /// <summary>
    /// Maximum number of frames allowed to be in flight (submitted to the server, awaiting completion)
    /// before <see cref="Lock"/> blocks waiting for a slot to free up.
    /// </summary>
    private const int MaxInFlight = 2;

    public X11ShmImageSwapchain(IntPtr deferredDisplay, IntPtr windowXId, IntPtr visual, int depth,
        X11DeferredDisplayDispatcher dispatcher)
    {
        DeferredDisplay = deferredDisplay;
        WindowXId = windowXId;
        Visual = visual;
        Depth = depth;
        Dispatcher = dispatcher;
    }

    public IntPtr DeferredDisplay { get; }
    public IntPtr WindowXId { get; }
    public IntPtr Visual { get; }
    public int Depth { get; }
    public X11DeferredDisplayDispatcher Dispatcher { get; }

    private readonly Queue<X11ShmImage> _availableQueue = new();
    private PixelSize? _lastSize;
    private int _inFlight;
    private bool _isDisposed;

    public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
    {
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImageSwapchain] Lock");
        properties = default;

        // (5a) Reclaim any buffers the server has finished with before deciding whether we must wait.
        Dispatcher.DrainPendingEvents();

        // (5b) Apply backpressure: block (pumping completions) until a slot frees up. A window resize does
        // not bypass this - frames submitted at the old size still count until their completion arrives.
        while (_inFlight >= MaxInFlight)
        {
            Dispatcher.DrainEventsBlockingAtMostOnce();
        }

        XLib.XGetGeometry(DeferredDisplay, WindowXId, out _, out _, out _, out var width, out var height,
            out _, out _);
        var size = new PixelSize(width, height);

        var image = GetOrCreateImage(size);
        return new X11ShmLockedFramebuffer(this, image);
    }

    private X11ShmImage GetOrCreateImage(PixelSize size)
    {
        if (_lastSize != size)
        {
            ClearAvailableQueue();
        }

        _lastSize = size;

        while (_availableQueue.TryDequeue(out var available))
        {
            if (available.Size == size)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
                    "[X11ShmImageSwapchain] Reuse X11ShmImage from available queue");
                return available;
            }

            available.Dispose();
        }

        return new X11ShmImage(size, this);
    }

    /// <summary>
    /// Called when a frame has been submitted to the server (XShmPutImage). Increments the in-flight count.
    /// </summary>
    public void OnImageSubmitted(X11ShmImage image)
    {
        _inFlight++;
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
            "[X11ShmImageSwapchain] Submitted, InFlight={InFlight}", _inFlight);
    }

    /// <summary>
    /// Called when a frame could not be submitted (XShmPutImage failed). The image will never produce a
    /// completion, so it is discarded without touching the in-flight count.
    /// </summary>
    public void OnImageDropped(X11ShmImage image) => image.Dispose();

    /// <summary>
    /// Called by the dispatcher when the server signals completion for one of our images.
    /// </summary>
    public void OnXShmCompletion(X11ShmImage image)
    {
        _inFlight--;
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
            "[X11ShmImageSwapchain] Completion, InFlight={InFlight}", _inFlight);

        // Drop buffers that no longer match the current size (or belong to a disposed swapchain) instead
        // of returning them to the reuse pool.
        if (_isDisposed || image.Size != _lastSize)
        {
            image.Dispose();
            return;
        }

        _availableQueue.Enqueue(image);
    }

    private void ClearAvailableQueue()
    {
        while (_availableQueue.TryDequeue(out var image))
        {
            image.Dispose();
        }
    }

    public void Dispose()
    {
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImageSwapchain] Dispose");
        _isDisposed = true;
        ClearAvailableQueue();
        // Images still in flight stay registered with the dispatcher and are disposed when their
        // completion is drained (see OnXShmCompletion).
    }
}
