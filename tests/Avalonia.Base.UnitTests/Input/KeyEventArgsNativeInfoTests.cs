using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class KeyEventArgsNativeInfoTests : ScopedTestBase
    {
        [Theory]
        [InlineData(RawKeyEventType.KeyDown)]
        [InlineData(RawKeyEventType.KeyUp)]
        public void Raw_Key_Info_Should_Reach_KeyEventArgs(RawKeyEventType type)
        {
            var nativeKeyInfo = new NativeKeyInfo
            {
                Format = NativeKeyInfoFormat.Win32,
                ScanCode = 0x2C,
                VirtualKey = 0x5A,
                Modifiers = 0x8
            };

            var received = Send(type, Key.Z, RawInputModifiers.Control, PhysicalKey.Z, null, e =>
            {
                e.KeyText = "\x1a";
                e.KeyTextIgnoringModifiers = "z";
                e.ConsumedModifiers = RawInputModifiers.Shift;
                e.NativeKeyInfo = nativeKeyInfo;
            });

            Assert.Equal("\x1a", received.KeyText);
            Assert.Equal("z", received.KeyTextIgnoringModifiers);
            Assert.Equal(KeyModifiers.Shift, received.ConsumedModifiers);
            Assert.Same(nativeKeyInfo, received.NativeKeyInfo);
        }

        [Fact]
        public void KeyText_Should_Keep_Nul_Char()
        {
            var received = Send(RawKeyEventType.KeyDown, Key.Space, RawInputModifiers.Control, PhysicalKey.Space, " ",
                e => e.KeyText = "\0");

            Assert.Equal("\0", received.KeyText);
            // Not reported is different from nothing consumed
            Assert.Null(received.ConsumedModifiers);
        }

        [Fact]
        public void Native_Key_Code_Zero_Should_Differ_From_Unavailable()
        {
            // macOS reports the A key as keyCode 0
            var received = Send(RawKeyEventType.KeyDown, Key.A, RawInputModifiers.None, PhysicalKey.A, "a",
                e => e.NativeKeyInfo = new NativeKeyInfo { Format = NativeKeyInfoFormat.MacOS, ScanCode = 0, VirtualKey = 0 });

            Assert.Equal(0u, received.NativeKeyInfo!.ScanCode);
            Assert.Equal(0u, received.NativeKeyInfo.VirtualKey);
            Assert.Null(received.NativeKeyInfo.Modifiers);
        }

        private static KeyEventArgs Send(RawKeyEventType type, Key key, RawInputModifiers modifiers,
            PhysicalKey physicalKey, string? keySymbol, System.Action<RawKeyEventArgs> initialize)
        {
            var target = new KeyboardDevice();
            var focused = new Control();
            var root = new TestRoot();
            KeyEventArgs? received = null;

            target.SetFocusedElement(focused, NavigationMethod.Unspecified, KeyModifiers.None);
            focused.KeyDown += (_, e) => received = e;
            focused.KeyUp += (_, e) => received = e;

            var raw = new RawKeyEventArgs(target, 0, root, type, key, modifiers, physicalKey, keySymbol);
            initialize(raw);
            target.ProcessRawEvent(raw);

            Assert.NotNull(received);
            return received;
        }
    }
}
