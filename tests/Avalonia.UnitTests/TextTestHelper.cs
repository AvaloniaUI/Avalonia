using System;
using System.Runtime.InteropServices;

namespace Avalonia.UnitTests
{
    public static class TextTestHelper
    {
        public static int GetStartCharIndex(ReadOnlyMemory<char> text)
        {
            if (!MemoryMarshal.TryGetString(text, out _, out var start, out _))
                throw new InvalidOperationException("text memory should have been a string");
            return start;
        }
    }
}
