using System;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Benchmarks
{
    internal class NullCursorFactory : ICursorFactory
    {
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new NullCursorImpl();
        ICursorImpl ICursorFactory.GetCursor(StandardCursorType cursorType) => new NullCursorImpl();

        private class NullCursorImpl : ICursorImpl
        {
            public void Dispose() { }
        }
    }
}
