using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Controls.UnitTests
{
    public class CursorFactoryMock : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType)
        {
            return new MockCursorImpl();
        }

        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        {
            return new MockCursorImpl();
        }

        private class MockCursorImpl : ICursorImpl
        {
            public void Dispose()
            {
            }
        }
    }
}
