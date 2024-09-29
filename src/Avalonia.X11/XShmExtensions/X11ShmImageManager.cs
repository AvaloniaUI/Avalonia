using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Logging;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmImageManager : IDisposable
{
    public X11ShmImageManager(X11ShmFramebufferContext context)
    {
        Context = context;
    }

    public X11ShmFramebufferContext Context { get; }

    private ConcurrentQueue<X11ShmImage> AvailableQueue { get; } = new();

    public PixelSize? LastSize { get; private set; }

    public X11ShmImage GetOrCreateImage(PixelSize size)
    {
        if (LastSize != size)
        {
            foreach (var x11ShmImage in AvailableQueue)
            {
                x11ShmImage.Dispose();
            }
#if NET6_0_OR_GREATER
            AvailableQueue.Clear();
#else
            while (AvailableQueue.TryDequeue(out _))
            {
            }
#endif
        }

        LastSize = size;

        if (Context.ShouldRenderOnUiThread)
        {
            // If the render thread and the UI thread are the same, then synchronous waiting cannot be performed here. This is because synchronous waiting would block the UI thread, preventing it from receiving subsequent render completion events, and ultimately causing the UI thread to become unresponsive.
        }
        else if (_presentationCount > Context.MaxXShmSwapchainFrameCount)
        {
            // Specifically, allowing one additional frame beyond the maximum render limit is beneficial. This is because at any given moment, one frame might be in the process of being returned, and another might be currently rendering. Therefore, adding an extra frame in preparation for rendering can maximize rendering efficiency.
            SpinWait.SpinUntil(() => _presentationCount <= Context.MaxXShmSwapchainFrameCount);
        }

#nullable enable
        X11ShmImage? image = null;
        while (AvailableQueue.TryDequeue(out image))
        {
            if (image.Size != size)
            {
                image.Dispose();
                image = null;
            }
            else
            {
                X11ShmDebugLogger.WriteLine($"[X11ShmImageManager][GetOrCreateImage] Get X11ShmImage from AvailableQueue.");
                break;
            }
        }

        if (image is null)
        {
            image = new X11ShmImage(size, this);
        }
#nullable disable

        var currentPresentationCount = Interlocked.Increment(ref _presentationCount);
        _ = currentPresentationCount;
        X11ShmDebugLogger.WriteLine($"[X11ShmImageManager][GetOrCreateImage] PresentationCount={currentPresentationCount}");

        return image;
    }

    private int _presentationCount;

    public void OnXShmCompletion(X11ShmImage image)
    {
        var currentPresentationCount = Interlocked.Decrement(ref _presentationCount);
        _ = currentPresentationCount;
        X11ShmDebugLogger.WriteLine($"[X11ShmImageManager][OnXShmCompletion] PresentationCount={currentPresentationCount}");

        if (_isDisposed)
        {
            image.Dispose();
            return;
        }

        if (image.Size != LastSize)
        {
            image.Dispose();
            return;
        }

        AvailableQueue.Enqueue(image);
    }

    public void Dispose()
    {
        _isDisposed = true;

        foreach (var x11ShmImage in AvailableQueue)
        {
            x11ShmImage.Dispose();
        }

#if NET6_0_OR_GREATER
        AvailableQueue.Clear();
#else
        while (AvailableQueue.TryDequeue(out _))
        {
        }
#endif
    }

    private bool _isDisposed;
}
