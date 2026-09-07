using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Input;
using Xunit;
using NativeMethods = Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class ReconversionStringTests
{
    private static int HeaderSize => Marshal.SizeOf<NativeMethods.RECONVERTSTRING>();

    [Fact]
    public void Write_Returns_Required_Size_For_Size_Query()
    {
        const string text = "hello";

        var size = Imm32ReconversionHelper.Write(IntPtr.Zero, text, 0, 0);

        Assert.Equal(HeaderSize + text.Length * sizeof(char), size);
    }

    [Fact]
    public void Write_Lays_Out_Header_Comp_Range_And_String()
    {
        const string text = "abcdef";

        WithBuffer(text.Length, buffer =>
        {
            var size = Imm32ReconversionHelper.Write(buffer, text, 2, 3);

            Assert.Equal(HeaderSize + text.Length * sizeof(char), size);

            var reconv = Marshal.PtrToStructure<NativeMethods.RECONVERTSTRING>(buffer);
            Assert.Equal((uint)text.Length, reconv.dwStrLen);
            Assert.Equal((uint)HeaderSize, reconv.dwStrOffset);
            Assert.Equal(3u, reconv.dwCompStrLen);
            Assert.Equal((uint)(2 * sizeof(char)), reconv.dwCompStrOffset);
            Assert.Equal(3u, reconv.dwTargetStrLen);
            Assert.Equal((uint)(2 * sizeof(char)), reconv.dwTargetStrOffset);

            var copied = Marshal.PtrToStringUni(buffer + (int)reconv.dwStrOffset, (int)reconv.dwStrLen);
            Assert.Equal(text, copied);
        });
    }

    [Fact]
    public void Write_Keeps_Offsets_In_Utf16_Code_Units_Across_Surrogate_Pairs()
    {
        const string text = "a\U0001F600b"; // 'a', an emoji surrogate pair, 'b' -> 4 code units.
        Assert.Equal(4, text.Length);

        WithBuffer(text.Length, buffer =>
        {
            // Select the 'b' that follows the emoji, at code-unit index 3.
            Imm32ReconversionHelper.Write(buffer, text, 3, 1);

            var reconv = Marshal.PtrToStructure<NativeMethods.RECONVERTSTRING>(buffer);
            Assert.Equal(4u, reconv.dwStrLen);
            Assert.Equal((uint)(3 * sizeof(char)), reconv.dwCompStrOffset);
            Assert.Equal(1u, reconv.dwCompStrLen);

            var copied = Marshal.PtrToStringUni(buffer + (int)reconv.dwStrOffset, (int)reconv.dwStrLen);
            Assert.Equal(text, copied);
        });
    }

    [Fact]
    public void Write_Does_Not_Fill_When_Buffer_Too_Small()
    {
        const string text = "abcdef";
        var needed = Imm32ReconversionHelper.GetRequiredSize(text.Length);

        var buffer = Marshal.AllocHGlobal(needed);
        try
        {
            // Simulate an IME that only sized the header and is probing for the required size.
            Marshal.StructureToPtr(new NativeMethods.RECONVERTSTRING { dwSize = (uint)HeaderSize }, buffer, false);

            var size = Imm32ReconversionHelper.Write(buffer, text, 0, text.Length);

            Assert.Equal(needed, size);

            // The string fields must be left untouched.
            var reconv = Marshal.PtrToStructure<NativeMethods.RECONVERTSTRING>(buffer);
            Assert.Equal(0u, reconv.dwStrLen);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Fact]
    public void Write_Clamps_Comp_Range_To_Text_Length()
    {
        const string text = "abc";

        WithBuffer(text.Length, buffer =>
        {
            Imm32ReconversionHelper.Write(buffer, text, 5, 10);

            var reconv = Marshal.PtrToStructure<NativeMethods.RECONVERTSTRING>(buffer);
            Assert.Equal((uint)(text.Length * sizeof(char)), reconv.dwCompStrOffset);
            Assert.Equal(0u, reconv.dwCompStrLen);
        });
    }

    [Fact]
    public void ReadCompRange_Round_Trips_Written_Range()
    {
        const string text = "abcdef";

        WithBuffer(text.Length, buffer =>
        {
            Imm32ReconversionHelper.Write(buffer, text, 2, 3);

            Assert.True(Imm32ReconversionHelper.ReadCompRange(buffer, out var start, out var len));
            Assert.Equal(2, start);
            Assert.Equal(3, len);
        });
    }

    [Fact]
    public void ReadCompRange_Reads_Ime_Adjusted_Range()
    {
        var buffer = Marshal.AllocHGlobal(HeaderSize);
        try
        {
            var reconv = new NativeMethods.RECONVERTSTRING
            {
                dwSize = (uint)HeaderSize,
                dwStrLen = 6,
                dwStrOffset = (uint)HeaderSize,
                dwCompStrOffset = 1 * sizeof(char),
                dwCompStrLen = 4
            };
            Marshal.StructureToPtr(reconv, buffer, false);

            Assert.True(Imm32ReconversionHelper.ReadCompRange(buffer, out var start, out var len));
            Assert.Equal(1, start);
            Assert.Equal(4, len);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Fact]
    public void ReadCompRange_Rejects_Out_Of_Range_Comp_String()
    {
        var buffer = Marshal.AllocHGlobal(HeaderSize);
        try
        {
            var reconv = new NativeMethods.RECONVERTSTRING
            {
                dwSize = (uint)HeaderSize,
                dwStrLen = 4,
                dwStrOffset = (uint)HeaderSize,
                dwCompStrOffset = 2 * sizeof(char),
                dwCompStrLen = 5 // 2 + 5 runs past the 4-char string.
            };
            Marshal.StructureToPtr(reconv, buffer, false);

            Assert.False(Imm32ReconversionHelper.ReadCompRange(buffer, out _, out _));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Fact]
    public void ReadCompRange_Returns_False_For_Null_Buffer()
    {
        Assert.False(Imm32ReconversionHelper.ReadCompRange(IntPtr.Zero, out _, out _));
    }

    private static void WithBuffer(int textLength, Action<IntPtr> test)
    {
        var needed = Imm32ReconversionHelper.GetRequiredSize(textLength);
        var buffer = Marshal.AllocHGlobal(needed);
        try
        {
            // Zero the header and set dwSize the way an IME would before the fill pass.
            Marshal.StructureToPtr(new NativeMethods.RECONVERTSTRING { dwSize = (uint)needed }, buffer, false);

            test(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
