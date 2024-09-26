using System;
using System.Collections.Generic;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmImageManager : IDisposable
{
    public X11ShmImageManager(X11ShmFramebufferContext context)
    {
        Context = context;
    }

    public X11ShmFramebufferContext Context { get; }

    public Queue<X11ShmImage> AvailableQueue = new();

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
        }
        else
        {
            // Check presentationQueue.Count < swapchainSize ?
            image = null;
        }

        image ??= new X11ShmImage(size, this);

        LastSize = size;

        _presentationCount++;

        return image;
    }

    private int _presentationCount;

    public void OnXShmCompletion(X11ShmImage image)
    {
        _presentationCount--;

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
    }

    private bool _isDisposed;
}