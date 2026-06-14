using System;
using System.Collections.Concurrent;
using global::Avalonia;
using global::Avalonia.Input;
using global::Avalonia.Media.Imaging;
using global::Avalonia.Platform;
using Microsoft.UI.Input;

namespace Avalonia.WinUI;

/// <summary>
/// Wraps a WinUI <see cref="InputCursor"/> so it can flow through Avalonia's
/// platform-agnostic cursor APIs.
/// </summary>
internal sealed class WinUICursorImpl : ICursorImpl
{
    public WinUICursorImpl(InputCursor? cursor)
    {
        Cursor = cursor;
    }

    public InputCursor? Cursor { get; }

    public void Dispose()
    {
        // InputSystemCursor instances are cached — never disposed.
    }
}

internal sealed class WinUICursorFactory : ICursorFactory
{
    public static WinUICursorFactory Instance { get; } = new();

    private readonly ConcurrentDictionary<StandardCursorType, WinUICursorImpl> _cache = new();

    private WinUICursorFactory() { }

    public ICursorImpl GetCursor(StandardCursorType cursorType)
    {
        return _cache.GetOrAdd(cursorType, static t =>
        {
            var shape = MapShape(t);
            InputCursor? cursor = shape is { } s ? InputSystemCursor.Create(s) : null;
            return new WinUICursorImpl(cursor);
        });
    }

    public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot)
    {
        // WinUI 3 exposes no public API to build an InputCursor from an
        // in-memory image (only InputSystemCursor.Create(shape) and
        // InputDesktopResourceCursor.CreateFromResource(fileName) are
        // available). Fall back to the default arrow so the caller still
        // gets a usable cursor.

        // TODO: Custom bitmap cursors...
        return _cache.GetOrAdd(StandardCursorType.Arrow, static t =>
            new WinUICursorImpl(InputSystemCursor.Create(InputSystemCursorShape.Arrow)));
    }

    private static InputSystemCursorShape? MapShape(StandardCursorType t) => t switch
    {
        StandardCursorType.None => null,
        StandardCursorType.Arrow => InputSystemCursorShape.Arrow,
        StandardCursorType.Ibeam => InputSystemCursorShape.IBeam,
        StandardCursorType.Wait => InputSystemCursorShape.Wait,
        StandardCursorType.AppStarting => InputSystemCursorShape.Wait,
        StandardCursorType.Cross => InputSystemCursorShape.Cross,
        StandardCursorType.Help => InputSystemCursorShape.Help,
        StandardCursorType.Hand => InputSystemCursorShape.Hand,
        StandardCursorType.No => InputSystemCursorShape.UniversalNo,
        StandardCursorType.SizeAll => InputSystemCursorShape.SizeAll,
        StandardCursorType.SizeNorthSouth => InputSystemCursorShape.SizeNorthSouth,
        StandardCursorType.SizeWestEast => InputSystemCursorShape.SizeWestEast,
        StandardCursorType.UpArrow => InputSystemCursorShape.UpArrow,
        StandardCursorType.TopSide => InputSystemCursorShape.SizeNorthSouth,
        StandardCursorType.BottomSide => InputSystemCursorShape.SizeNorthSouth,
        StandardCursorType.LeftSide => InputSystemCursorShape.SizeWestEast,
        StandardCursorType.RightSide => InputSystemCursorShape.SizeWestEast,
        StandardCursorType.TopLeftCorner => InputSystemCursorShape.SizeNorthwestSoutheast,
        StandardCursorType.BottomRightCorner => InputSystemCursorShape.SizeNorthwestSoutheast,
        StandardCursorType.TopRightCorner => InputSystemCursorShape.SizeNortheastSouthwest,
        StandardCursorType.BottomLeftCorner => InputSystemCursorShape.SizeNortheastSouthwest,
        // No built-in drag cursors — fall back to Arrow; OLE drag operations on
        // Win32 traditionally use ole32.dll resources we can't reach from here.
        StandardCursorType.DragMove => InputSystemCursorShape.Arrow,
        StandardCursorType.DragCopy => InputSystemCursorShape.Arrow,
        StandardCursorType.DragLink => InputSystemCursorShape.Arrow,
        _ => InputSystemCursorShape.Arrow,
    };
}
