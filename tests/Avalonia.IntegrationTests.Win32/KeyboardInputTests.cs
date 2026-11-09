using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;
using static Avalonia.IntegrationTests.Win32.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

/// <summary>
/// Sends real keyboard input with keybd_event, so these tests need an interactive desktop that this process can
/// bring to the foreground.
/// </summary>
public sealed class KeyboardInputTests : IDisposable
{
    private const byte VK_RETURN = 0x0D, VK_MENU = 0x12, VK_E = 0x45, VK_NUMPAD0 = 0x60, VK_OEM_5 = 0xDC;
    private const byte ScanReturn = 0x1C, ScanMenu = 0x38, ScanE = 0x12, ScanOem5 = 0x29;
    private static readonly byte[] s_numpadScanCodes = [0x52, 0x4F, 0x50, 0x51, 0x4B, 0x4C, 0x4D, 0x47, 0x48, 0x49];

    private readonly List<Window> _windows = new();
    private IntPtr _previousLayout;

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();
        if (_previousLayout != IntPtr.Zero)
            ActivateKeyboardLayout(_previousLayout, 0);
    }

    [Fact]
    public async Task Alt_Numpad_Works_When_Focus_Changes_With_Alt_Pressed()
    {
        var first = await CreateWindowAsync(100);
        var second = await CreateWindowAsync(500);
        await ActivateAsync(first);

        keybd_event(VK_MENU, ScanMenu, 0, 0);
        try
        {
            // The focus change ends any pending dead key while Alt is down
            await ActivateAsync(second);
            foreach (var digit in "0233")
                Tap((byte)(VK_NUMPAD0 + digit - '0'), s_numpadScanCodes[digit - '0']);
        }
        finally
        {
            keybd_event(VK_MENU, ScanMenu, KEYEVENTF_KEYUP, 0);
        }

        await WaitForAsync(() => second.Text.Length > 0);
        Assert.Equal("é", second.Text.ToString());
        Assert.Equal("", first.Text.ToString());
    }

    [Fact]
    public async Task Dead_Key_Combines_Within_Window()
    {
        UseGermanLayout();
        var log = await CreateWindowAsync(100);
        await ActivateAsync(log);

        Tap(VK_OEM_5, ScanOem5);
        Tap(VK_E, ScanE);

        await WaitForAsync(() => log.Text.Length > 0);
        Assert.Equal("ê", log.Text.ToString());
        Assert.Equal(Key.DeadCharProcessed, log.LastKeyDown?.Key);
    }

    [Fact]
    public async Task Dead_Key_Pressed_Twice_Reports_Dead_Char_As_Text_Ignoring_Modifiers()
    {
        UseGermanLayout();
        var log = await CreateWindowAsync(100);
        await ActivateAsync(log);

        Tap(VK_OEM_5, ScanOem5);
        Tap(VK_OEM_5, ScanOem5);

        await WaitForAsync(() => log.Text.Length > 0);
        Assert.Equal("^^", log.LastKeyDown?.KeyText);
        Assert.Equal("^", log.LastKeyDown?.KeyTextIgnoringModifiers);
    }

    [Fact]
    public async Task Enter_Ends_Dead_Key()
    {
        UseGermanLayout();
        var log = await CreateWindowAsync(100);
        await ActivateAsync(log);

        Tap(VK_OEM_5, ScanOem5);
        Tap(VK_RETURN, ScanReturn);
        Tap(VK_E, ScanE);

        await WaitForAsync(() => log.Text.ToString().EndsWith('e'));
        Assert.Equal(Key.E, log.LastKeyDown?.Key);
    }

    [Fact]
    public async Task Dead_Key_Does_Not_Cross_Focus_Change()
    {
        UseGermanLayout();
        var first = await CreateWindowAsync(100);
        var second = await CreateWindowAsync(500);
        await ActivateAsync(first);

        Tap(VK_OEM_5, ScanOem5);
        // keybd_event only queues the input
        await WaitForAsync(() => first.LastKeyDown?.PhysicalKey == PhysicalKey.Backquote);
        Assert.Equal(PhysicalKey.Backquote, first.LastKeyDown?.PhysicalKey);
        await ActivateAsync(second);
        Tap(VK_E, ScanE);

        await WaitForAsync(() => second.Text.Length > 0);
        Assert.Equal("e", second.Text.ToString());
        Assert.Equal(Key.E, second.LastKeyDown?.Key);
    }

    private void UseGermanLayout()
    {
        _previousLayout = GetKeyboardLayout(0);
        Assert.NotEqual(IntPtr.Zero, LoadKeyboardLayout("00000407", KLF_ACTIVATE | KLF_NOTELLSHELL));
    }

    private async Task<KeyLog> CreateWindowAsync(int x)
    {
        var log = new KeyLog();
        var window = new Window
        {
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new PixelPoint(x, 100),
            Content = log
        };
        _windows.Add(window);
        window.Show();
        await window.WhenLoadedAsync();
        return log;
    }

    private static async Task ActivateAsync(KeyLog log)
    {
        var window = (Window)TopLevel.GetTopLevel(log)!;
        window.Activate();
        log.Focus();

        var handle = window.TryGetPlatformHandle()!.Handle;
        await WaitForAsync(() => GetForegroundWindow() == handle && log.IsFocused);
        if (GetForegroundWindow() != handle)
            Assert.Skip("The test window can't be brought to the foreground.");
    }

    private static void Tap(byte vk, byte scanCode)
    {
        keybd_event(vk, scanCode, 0, 0);
        keybd_event(vk, scanCode, KEYEVENTF_KEYUP, 0);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);
        // Let any remaining input arrive
        await Task.Delay(100);
    }

    private sealed class KeyLog : Border
    {
        public KeyLog()
        {
            Focusable = true;
            Background = Brushes.White;
        }

        public StringBuilder Text { get; } = new();

        public KeyEventArgs? LastKeyDown { get; private set; }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            LastKeyDown = e;
            base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            Text.Append(e.Text);
            base.OnTextInput(e);
        }
    }
}
