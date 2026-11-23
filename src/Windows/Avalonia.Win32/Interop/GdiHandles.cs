using System;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Interop;

/// <summary>
/// A device context. Dispose releases it with the function that matches the way it was created.
/// </summary>
internal readonly struct GdiDC : IDisposable
{
    private static readonly Action<IntPtr> s_releaseScreenDC = dc => ReleaseDC(IntPtr.Zero, dc);
    private static readonly Action<IntPtr> s_deleteDC = dc => DeleteDC(dc);

    private readonly Action<IntPtr>? _release;

    private GdiDC(IntPtr handle, Action<IntPtr> release)
    {
        Handle = handle;
        _release = release;
    }

    public IntPtr Handle { get; }

    public bool IsInvalid => Handle == IntPtr.Zero;

    /// <summary>
    /// Gets the device context of <paramref name="hwnd"/>, or of the screen if it is zero.
    /// </summary>
    public static GdiDC Get(IntPtr hwnd = default) =>
        new(GetDC(hwnd), hwnd == IntPtr.Zero ? s_releaseScreenDC : dc => ReleaseDC(hwnd, dc));

    /// <summary>
    /// Creates a memory device context that is compatible with <paramref name="dc"/>.
    /// </summary>
    public static GdiDC CreateCompatible(IntPtr dc) => new(CreateCompatibleDC(dc), s_deleteDC);

    /// <summary>
    /// Selects <paramref name="gdiObject"/> into this device context until the result is disposed.
    /// </summary>
    public SelectedGdiObject Select(IntPtr gdiObject) => new(Handle, SelectObject(Handle, gdiObject));

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            _release?.Invoke(Handle);
    }

    public static implicit operator IntPtr(GdiDC dc) => dc.Handle;
}

/// <summary>
/// Puts the previous object back into a device context on dispose.
/// GDI cannot delete an object that is still selected into a device context.
/// </summary>
internal readonly struct SelectedGdiObject : IDisposable
{
    private readonly IntPtr _dc;
    private readonly IntPtr _previous;

    public SelectedGdiObject(IntPtr dc, IntPtr previous)
    {
        _dc = dc;
        _previous = previous;
    }

    public void Dispose()
    {
        if (_dc != IntPtr.Zero && _previous != IntPtr.Zero)
            SelectObject(_dc, _previous);
    }
}

/// <summary>
/// A GDI object, such as a bitmap. Dispose deletes it.
/// </summary>
internal readonly struct GdiObject : IDisposable
{
    public GdiObject(IntPtr handle) => Handle = handle;

    public IntPtr Handle { get; }

    public bool IsInvalid => Handle == IntPtr.Zero;

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            DeleteObject(Handle);
    }

    public static implicit operator IntPtr(GdiObject gdiObject) => gdiObject.Handle;
}
