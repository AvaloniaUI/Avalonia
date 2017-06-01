using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class Cursor : IPlatformHandle
    {

        public NSCursor Native { get; }

        public IntPtr Handle => Native.Handle;

        public string HandleDescriptor => "NSCursor";

        public Cursor(NSCursor native)
        {
            Native = native;
        }
    }

    class CursorFactoryStub : IStandardCursorFactory
    {
        Dictionary<StandardCursorType, NSCursor> _cache;

        public CursorFactoryStub()
        {
            //TODO: Load diagonal cursors from webkit
            //See https://stackoverflow.com/q/10733228
            _cache = new Dictionary<StandardCursorType, NSCursor>()
            {
                [StandardCursorType.Arrow] = NSCursor.ArrowCursor,
                [StandardCursorType.AppStarting] = NSCursor.ArrowCursor, //TODO
                [StandardCursorType.BottomLeftCorner] = NSCursor.CrosshairCursor, //TODO
                [StandardCursorType.BottomRightCorner] = NSCursor.CrosshairCursor, //TODO
                [StandardCursorType.BottomSize] = NSCursor.ResizeDownCursor,
                [StandardCursorType.Cross] = NSCursor.CrosshairCursor,
                [StandardCursorType.Hand] = NSCursor.PointingHandCursor,
                [StandardCursorType.Help] = NSCursor.ContextualMenuCursor,
                [StandardCursorType.Ibeam] = NSCursor.IBeamCursor,
                [StandardCursorType.LeftSide] = NSCursor.ResizeLeftCursor,
                [StandardCursorType.No] = NSCursor.OperationNotAllowedCursor,
                [StandardCursorType.RightSide] = NSCursor.ResizeRightCursor,
                [StandardCursorType.SizeAll] = NSCursor.CrosshairCursor, //TODO
                [StandardCursorType.SizeNorthSouth] = NSCursor.ResizeUpDownCursor,
                [StandardCursorType.SizeWestEast] = NSCursor.ResizeLeftRightCursor,
                [StandardCursorType.TopLeftCorner] = NSCursor.CrosshairCursor, //TODO
                [StandardCursorType.TopRightCorner] = NSCursor.CrosshairCursor, //TODO
                [StandardCursorType.TopSide] = NSCursor.ResizeUpCursor,
                [StandardCursorType.UpArrow] = NSCursor.ResizeUpCursor,
                [StandardCursorType.Wait] = NSCursor.ArrowCursor, //TODO
            };
        }

        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new Cursor(_cache[cursorType]);
        }
    }
}