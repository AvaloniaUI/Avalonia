using System;
using System.Collections.Generic;

namespace Avalonia.X11.XShmExtensions;

class X11ShmFramebufferContext
{
    public X11ShmFramebufferContext(X11Window x11Window, IntPtr display, IntPtr windowXId, IntPtr renderHandle, IntPtr visual, int depth)
    {
        X11Window = x11Window;
        Display = display;
        WindowXId = windowXId;
        RenderHandle = renderHandle;
        Visual = visual;
        Depth = depth;
    }

    public X11Window X11Window { get; }

    public IntPtr Display { get; }

    public IntPtr WindowXId { get; }

    public IntPtr RenderHandle { get; }
    public IntPtr Visual { get; }
    public int Depth { get; }

    public void OnXShmCompletion(UInt64 shmseg)
    {
        if (_shmImageDictionary.Remove(shmseg, out var image))
        {
            image.ShmImageManager.OnXShmCompletion(image);
        }
        else
        {
            // Unexpected case, all the X11ShmImage should be registered in the dictionary
        }
    }

    public void RegisterX11ShmImage(X11ShmImage image)
    {
        _shmImageDictionary[image.ShmSeg] = image;
    }

    private readonly Dictionary<UInt64, X11ShmImage> _shmImageDictionary = new Dictionary<UInt64, X11ShmImage>();
}