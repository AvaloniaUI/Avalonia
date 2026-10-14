using System;
using static Avalonia.X11.XLib;

namespace Avalonia.X11;

/// <summary>
/// Owns the size of the render child window of an <see cref="X11Window"/>. The child is resized only from here,
/// on the render thread over the deferred display, to the scene that is about to be drawn, so the GL/Vulkan
/// surface is used at that exact size without involving the UI thread.
/// </summary>
internal sealed class X11RenderWindowResizer
{
    private readonly IntPtr _display;
    private readonly IntPtr _window;

    public X11RenderWindowResizer(IntPtr display, IntPtr window, PixelSize initialSize)
    {
        _display = display;
        _window = window;
        Size = initialSize;
    }

    /// <summary>
    /// The child's current size: the size it was created with until the first frame, then the last scene size.
    /// </summary>
    public PixelSize Size { get; private set; }

    public void Resize(PixelSize size)
    {
        if (Size == size)
            return;

        XLockDisplay(_display);
        try
        {
            XConfigureResizeWindow(_display, _window, size.Width, size.Height);
            // Make sure the server has applied the new size before the GL/Vulkan driver queries the drawable.
            XSync(_display, false);
        }
        finally
        {
            XUnlockDisplay(_display);
        }

        Size = size;
    }
}
