using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

/// <summary>
/// Runs the key resolution against a small keymap with libxkbcommon. Skipped when the library is missing.
/// </summary>
public class XkbKeyTransformTests : IDisposable
{
    // Group 1 "us", group 2 "ru". Mod5 selects level 3, and Control selects level 2 of <AD11>.
    private const string Keymap = """
        xkb_keymap {
            xkb_keycodes "test" {
                minimum = 8;
                maximum = 255;
                <AE01> = 10;
                <AD11> = 34;
                <AC01> = 38;
                <AB01> = 52;
            };
            xkb_types "test" {
                type "ONE_LEVEL" { modifiers = none; map[none] = Level1; };
                type "TWO_LEVEL" { modifiers = Shift; map[Shift] = Level2; };
                type "LEVEL3" { modifiers = Shift+Mod5; map[Shift] = Level2; map[Mod5] = Level3; };
                type "CTRL_LEVEL2" { modifiers = Control; map[Control] = Level2; };
            };
            xkb_compat "test" { };
            xkb_symbols "test" {
                key <AE01> { type = "TWO_LEVEL", symbols[Group1] = [ 1, exclam ], symbols[Group2] = [ 1, exclam ] };
                key <AD11> { type = "CTRL_LEVEL2", symbols[Group1] = [ bracketleft, braceleft ] };
                key <AC01> { type = "LEVEL3", symbols[Group1] = [ a, A, ae ],
                             symbols[Group2] = [ Cyrillic_ef, Cyrillic_EF, ae ] };
                key <AB01> { type = "TWO_LEVEL", symbols[Group1] = [ z, Z ],
                             symbols[Group2] = [ Cyrillic_ya, Cyrillic_YA ] };
            };
        };
        """;

    // Real modifier masks
    private const uint Shift = 1 << 0;
    private const uint Control = 1 << 2;
    private const uint Mod5 = 1 << 7;

    // evdev codes (XKB keycode - 8)
    private const uint Key1 = 2, KeyLeftBrace = 26, KeyA = 30, KeyZ = 44;

    private readonly XkbContext? _context;
    private readonly XkbCommonKeymap? _keymap;

    public XkbKeyTransformTests()
    {
        if (!NativeLibrary.TryLoad("libxkbcommon.so.0", out _))
            return;
        _context = new XkbContext();
        _keymap = new XkbCommonKeymap(_context, Encoding.UTF8.GetBytes(Keymap));
    }

    public void Dispose()
    {
        _keymap?.Dispose();
        _context?.Dispose();
    }

    private RawInputModifiers Consumed(uint evdev, uint mods)
    {
        if (_keymap is null)
            Assert.Skip("libxkbcommon is not available");
        _keymap.UpdateModifiers(mods, 0, 0, 0);
        return XkbKeyTransform.ModifiersFromXkbMask(_keymap.GetConsumedModifiers(evdev) & mods);
    }

    private (Key key, string? keySymbol, string? keyText, string? textIgnoringModifiers) Resolve(uint evdev, uint mods,
        uint group = 0)
    {
        if (_keymap is null)
            Assert.Skip("libxkbcommon is not available");
        _keymap.UpdateModifiers(mods, 0, 0, group);
        var (key, keySymbol, text, keysym) =
            XkbKeyTransform.ResolveKeyWithFallback(_keymap, evdev, XkbKeyTransform.PhysicalKeyFromEvdev(evdev));
        return (key, keySymbol, text, XkbCommonKeymap.KeysymToText(keysym));
    }

    [Fact]
    public void Ctrl_Letter_Gives_Control_Char_And_Plain_Letter()
    {
        var (key, keySymbol, keyText, textIgnoringModifiers) = Resolve(KeyA, Control);
        Assert.Equal(Key.A, key);
        Assert.Null(keySymbol);
        Assert.Equal("\x01", keyText);
        Assert.Equal("a", textIgnoringModifiers);
    }

    [Fact]
    public void Shifted_Punctuation_Keeps_Shift_Level()
    {
        var (_, _, keyText, textIgnoringModifiers) = Resolve(Key1, Shift | Control);
        Assert.Equal("!", keyText);
        Assert.Equal("!", textIgnoringModifiers);
    }

    [Fact]
    public void Level3_Is_Kept()
    {
        var (_, _, keyText, textIgnoringModifiers) = Resolve(KeyA, Mod5);
        Assert.Equal("æ", keyText);
        Assert.Equal("æ", textIgnoringModifiers);
    }

    [Fact]
    public void Ctrl_In_Non_Latin_Group_Uses_Latin_Fallback()
    {
        var (key, _, keyText, textIgnoringModifiers) = Resolve(KeyZ, Control, 1);
        Assert.Equal(Key.Z, key);
        Assert.Equal("\x1a", keyText);
        Assert.Equal("я", textIgnoringModifiers);
    }

    [Fact]
    public void Shift_On_Punctuation_Is_Consumed_And_Ctrl_Is_Not()
    {
        Assert.Equal(RawInputModifiers.Shift, Consumed(Key1, Shift | Control));
    }

    [Fact]
    public void Ctrl_On_Letter_Is_Not_Consumed()
    {
        Assert.Equal(RawInputModifiers.None, Consumed(KeyA, Control));
    }

    [Fact]
    public void Ctrl_That_Selects_A_Level_Is_Consumed()
    {
        Assert.Equal(RawInputModifiers.Control, Consumed(KeyLeftBrace, Control));
    }

    [Fact]
    public void Level_Selected_By_Ctrl_Is_Reflected()
    {
        // Documented limitation: the text follows the level that Ctrl selects
        var (_, _, _, textIgnoringModifiers) = Resolve(KeyLeftBrace, Control);
        Assert.Equal("{", textIgnoringModifiers);
    }
}
