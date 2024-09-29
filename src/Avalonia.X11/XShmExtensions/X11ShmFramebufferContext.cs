using System;
using System.Collections.Generic;

using ShmSeg = System.UInt64;

namespace Avalonia.X11.XShmExtensions;

class X11ShmFramebufferContext
{
    public X11ShmFramebufferContext(X11Window x11Window, IntPtr display, IntPtr windowXId, IntPtr visual, int depth, bool shouldRenderOnUiThread)
    {
        X11Window = x11Window;
        Display = display;
        WindowXId = windowXId;
        Visual = visual;
        Depth = depth;
        ShouldRenderOnUiThread = shouldRenderOnUiThread;
    }

    public X11Window X11Window { get; }

    public IntPtr Display { get; }

    public IntPtr WindowXId { get; }

    public IntPtr Visual { get; }
    public int Depth { get; }
    public bool ShouldRenderOnUiThread { get; }

    /// <summary>
    /// The maximum number of XShmSwapchain frame count.
    /// </summary>
    public int MaxXShmSwapchainFrameCount => 2;

    public void OnXShmCompletion(ShmSeg shmseg)
    {
        X11ShmDebugLogger.WriteLine($"[X11ShmFramebufferContext] OnXShmCompletion");
        if (_shmImageDictionary.Remove(shmseg, out var image))
        {
            image.ShmImageManager.OnXShmCompletion(image);
        }
        else
        {
            // Unexpected case, all the X11ShmImage should be registered in the dictionary
            X11ShmDebugLogger.WriteLine($"[X11ShmFramebufferContext][OnXShmCompletion] [Warn] Can not find shmseg={shmseg} in Dictionary!!!");
        }
    }

    public void RegisterX11ShmImage(X11ShmImage image)
    {
        _shmImageDictionary[image.ShmSeg] = image;
    }

    private readonly Dictionary<ShmSeg, X11ShmImage> _shmImageDictionary = new Dictionary<ShmSeg, X11ShmImage>();
}
