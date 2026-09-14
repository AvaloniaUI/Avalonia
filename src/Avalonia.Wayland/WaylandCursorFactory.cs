using System;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

class WaylandCursorFactory : ICursorFactory
{
    private readonly WaylandWorkerClient _client;

    public WaylandCursorFactory(WaylandWorkerClient client)
    {
        _client = client;
    }

    public ICursorImpl GetCursor(StandardCursorType cursorType)
        => new WaylandCursorImpl(_client.CreateStandardCursor(cursorType));

    public unsafe ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot)
    {
        // Extract the cursor pixels into a tightly-packed Bgra8888 premultiplied buffer on the UI
        // thread (CopyPixels transcodes format + alpha). The worker thread then uploads this raw
        // buffer into a wl_buffer without touching the UI-owned Bitmap.
        var size = cursor.PixelSize;
        var stride = size.Width * 4;
        var pixels = new byte[stride * size.Height];
        fixed (byte* ptr = pixels)
        {
            using var locked = new LockedFramebuffer((IntPtr)ptr, size, stride,
                new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul, null);
            cursor.CopyPixels(locked);
        }

        return new WaylandCursorImpl(_client.CreateBitmapCursor(pixels, size, hotSpot.X, hotSpot.Y));
    }
}

class WaylandCursorImpl : ICursorImpl
{
    /// <summary>UI-thread wrapper for the worker-side cursor (standard or custom bitmap).</summary>
    public WaylandCursorProxy Cursor { get; }

    public WaylandCursorImpl(WaylandCursorProxy cursor)
    {
        Cursor = cursor;
    }

    public void Dispose() => Cursor.Destroy();
}
