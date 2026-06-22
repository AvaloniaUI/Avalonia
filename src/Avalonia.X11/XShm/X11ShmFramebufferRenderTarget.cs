using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.X11.XShm;

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
internal class X11ShmFramebufferRenderTarget : IFramebufferRenderTarget
{
    /// <summary>
    /// Maximum number of frames allowed to be in flight (submitted to the server, awaiting completion)
    /// before <see cref="Lock"/> blocks waiting for a slot to free up.
    /// </summary>
    private const int MaxInFlight = 2;

    public X11ShmFramebufferRenderTarget(IntPtr deferredDisplay, IntPtr windowXId, IntPtr visual, int depth,
        X11DeferredDisplayDispatcher dispatcher)
    {
        _deferredDisplay = deferredDisplay;
        _windowXId = windowXId;
        _visual = visual;
        _depth = depth;
        _dispatcher = dispatcher;
    }

    private readonly IntPtr _deferredDisplay;
    private readonly IntPtr _windowXId;
    private readonly IntPtr _visual;
    private readonly int _depth;
    private readonly X11DeferredDisplayDispatcher _dispatcher;

    private readonly Queue<X11ShmImage> _availableQueue = new();
    private PixelSize? _lastSize;
    private int _inFlight;
    private bool _isDisposed;

    public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
    {
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmFramebufferRenderTarget] Lock");
        properties = default;

        // (5a) Reclaim any buffers the server has finished with before deciding whether we must wait.
        _dispatcher.DrainPendingEvents();

        // (5b) Apply backpressure: block (pumping completions) until a slot frees up. A window resize does
        // not bypass this - frames submitted at the old size still count until their completion arrives.
        while (_inFlight >= MaxInFlight)
        {
            _dispatcher.DrainEventsBlockingAtMostOnce();
        }

        XLib.XGetGeometry(_deferredDisplay, _windowXId, out _, out _, out _, out var width, out var height,
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
                    "[X11ShmFramebufferRenderTarget] Reuse X11ShmImage from available queue");
                return available;
            }

            available.Dispose();
        }

        return new X11ShmImage(size, _deferredDisplay, _visual, _depth);
    }

    /// <summary>
    /// Called when a frame has been submitted to the server (XShmPutImage). Increments the in-flight count.
    /// </summary>
    private void OnImageSubmitted()
    {
        _inFlight++;
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
            "[X11ShmFramebufferRenderTarget] Submitted, InFlight={InFlight}", _inFlight);
    }

    /// <summary>
    /// Called by the dispatcher (via the completion callback) when the server signals completion for one of
    /// our images.
    /// </summary>
    private void OnXShmCompletion(X11ShmImage image)
    {
        _inFlight--;
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
            "[X11ShmFramebufferRenderTarget] Completion, InFlight={InFlight}", _inFlight);

        // Drop buffers that no longer match the current size (or belong to a disposed target) instead of
        // returning them to the reuse pool.
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
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmFramebufferRenderTarget] Dispose");
        _isDisposed = true;
        ClearAvailableQueue();
        // Images still in flight stay registered with the dispatcher and are disposed when their
        // completion is drained (see OnXShmCompletion).
    }

    private sealed class X11ShmLockedFramebuffer : ILockedFramebuffer
    {
        public X11ShmLockedFramebuffer(X11ShmFramebufferRenderTarget owner, X11ShmImage shmImage)
        {
            _owner = owner;
            _image = shmImage;
        }

        private readonly X11ShmFramebufferRenderTarget _owner;
        private readonly X11ShmImage _image;

        public IntPtr Address => _image.ShmAddr;
        public PixelSize Size => _image.Size;
        public int RowBytes => _image.Size.Width * X11ShmImage.ByteSizeOfPixel;
        public Vector Dpi => new Vector(96, 96);
        public PixelFormat Format => PixelFormat.Bgra8888;
        public AlphaFormat AlphaFormat => AlphaFormat.Premul;

        public void Dispose() => SendRender();

        private unsafe void SendRender()
        {
            var display = _owner._deferredDisplay;
            var xid = _owner._windowXId;

            var gc = XLib.XCreateGC(display, xid, 0, IntPtr.Zero);

            // The DeferredDisplay connection is owned by the rendering thread, so no XLockDisplay is needed -
            // XInitThreads already serializes individual Xlib calls.
            var status = XLib.XShmPutImage(display, xid, gc, _image.ShmImage, 0, 0, 0, 0, (uint)Size.Width,
                (uint)Size.Height, send_event: true);
            XLib.XFlush(display);
            XLib.XFreeGC(display, gc);

            if (status != 0)
            {
                // The request was accepted, so the server will emit a matching XShmCompletionEvent. Register a
                // completion callback and count the frame as in flight only now, so a failed put can never leave
                // the backpressure counter stuck (a frame that never completes would otherwise block Lock forever).
                var image = _image;
                _owner._dispatcher.RegisterForShmCompletion(image.ShmSeg, () => _owner.OnXShmCompletion(image));
                _owner.OnImageSubmitted();
                Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmLockedFramebuffer] SendRender XShmPutImage");
            }
            else
            {
                // No completion will arrive for a failed put; drop the image so it never counts toward backpressure.
                Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?.Log(this, "[X11ShmLockedFramebuffer] XShmPutImage failed, dropping image");
                _image.Dispose();
            }
        }
    }
}
