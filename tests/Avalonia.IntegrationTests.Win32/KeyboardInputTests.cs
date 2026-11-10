using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.IntegrationTests.Win32.Infrastructure;
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
    private const string GermanLayout = "00000407";
    private const byte VK_RETURN = 0x0D, VK_MENU = 0x12, VK_E = 0x45, VK_NUMPAD0 = 0x60, VK_OEM_5 = 0xDC;
    private const byte ScanReturn = 0x1C, ScanMenu = 0x38, ScanE = 0x12, ScanOem5 = 0x29;
    private static readonly byte[] s_numpadScanCodes = [0x52, 0x4F, 0x50, 0x51, 0x4B, 0x4C, 0x4D, 0x47, 0x48, 0x49];

    private readonly List<Window> _windows = new();
    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();
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

    [KeyboardLayoutFact(GermanLayout)]
    public async Task Dead_Key_Combines_Within_Window()
    {
        var log = await CreateWindowAsync(100);
        await ActivateAsync(log);
        await UseGermanLayoutAsync();

        Tap(VK_OEM_5, ScanOem5);
        Tap(VK_E, ScanE);

        await WaitForAsync(() => log.Text.Length > 0);
        Assert.Equal("ê", log.Text.ToString());
        Assert.Equal(Key.DeadCharProcessed, log.LastKeyDown?.Key);
    }

    [KeyboardLayoutFact(GermanLayout)]
    public async Task Dead_Key_Pressed_Twice_Reports_Dead_Char_As_Alternate_Key_Text()
    {
        var log = await CreateWindowAsync(100);
        await ActivateAsync(log);
        await UseGermanLayoutAsync();

        Tap(VK_OEM_5, ScanOem5);
        Tap(VK_OEM_5, ScanOem5);

        await WaitForAsync(() => log.Text.Length > 0);
        Assert.Equal("^^", log.LastKeyDown?.KeyText);
        Assert.Equal("^", log.LastKeyDown?.AlternateKeyText);
    }

    [KeyboardLayoutFact(GermanLayout)]
    public async Task Enter_Ends_Dead_Key()
    {
        var log = await CreateWindowAsync(100);
        await ActivateAsync(log);
        await UseGermanLayoutAsync();

        Tap(VK_OEM_5, ScanOem5);
        Tap(VK_RETURN, ScanReturn);
        Tap(VK_E, ScanE);

        await WaitForAsync(() => log.Text.ToString().EndsWith('e'));
        Assert.Equal(Key.E, log.LastKeyDown?.Key);
    }

    [KeyboardLayoutFact(GermanLayout)]
    public async Task Dead_Key_Does_Not_Cross_Focus_Change()
    {
        var first = await CreateWindowAsync(100);
        var second = await CreateWindowAsync(500);
        await ActivateAsync(first);
        await UseGermanLayoutAsync();

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

    /// <summary>
    /// Switches this process to the German layout. The input profile is managed by the Text Services Framework, and
    /// changing only the keyboard layout leaves an active IME (for example Pinyin) processing the keys. A layout that
    /// isn't installed has no profile, and then only the thread's layout is switched.
    /// </summary>
    private static async Task UseGermanLayoutAsync()
    {
        var layout = LoadKeyboardLayout(GermanLayout, KLF_NOTELLSHELL);
        Assert.NotEqual(IntPtr.Zero, layout);

        await ActivateInputProfileAsync(layout);
        ActivateKeyboardLayout(layout, 0);

        await WaitForAsync(() => GetKeyboardLayout(0) == layout);
        Assert.Equal(layout, GetKeyboardLayout(0));
    }

    private static Task<int> ActivateInputProfileAsync(IntPtr layout)
    {
        // TSF needs an STA thread, and the test UI thread isn't one. The profile is activated for the whole process.
        var result = new TaskCompletionSource<int>();
        var thread = new Thread(() =>
        {
            try
            {
                var manager = (ITfInputProcessorProfileMgr)Activator.CreateInstance(
                    Type.GetTypeFromCLSID(CLSID_TF_InputProcessorProfiles)!)!;
                var none = Guid.Empty;
                var languageId = (ushort)((ulong)layout.ToInt64() & 0xFFFF);
                result.SetResult(manager.ActivateProfile(TF_PROFILETYPE_KEYBOARDLAYOUT, languageId, ref none, ref none,
                    layout, TF_IPPMF_FORPROCESS | TF_IPPMF_DONTCARECURRENTINPUTLANGUAGE));
            }
            catch (Exception e)
            {
                result.SetException(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return result.Task;
    }

    private static IntPtr GetHandle(KeyLog log) => TopLevel.GetTopLevel(log)!.TryGetPlatformHandle()!.Handle;

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

        var handle = GetHandle(log);
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
