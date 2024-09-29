using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

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
            AvailableQueue.Clear();
        }

        if (AvailableQueue.TryDequeue(out var image))
        {
            if (image.Size != size)
            {
                image.Dispose();
                image = null;
            }
            else
            {
                X11ShmDebugLogger.WriteLine($"[X11ShmImageManager][GetOrCreateImage] Get X11ShmImage from AvailableQueue.");
            }
        }
        else
        {
            // Check presentationQueue.Count < swapchainSize ?
            image = null;
        }

        image ??= new X11ShmImage(size, this);

        LastSize = size;

        var currentPresentationCount = Interlocked.Increment(ref _presentationCount);
        X11ShmDebugLogger.WriteLine($"[X11ShmImageManager][GetOrCreateImage] PresentationCount={currentPresentationCount}");

        return image;
    }

    private int _presentationCount;

    public void OnXShmCompletion(X11ShmImage image)
    {
        var currentPresentationCount = Interlocked.Decrement(ref _presentationCount);
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
        AvailableQueue.Clear();
    }

    private bool _isDisposed;
}
