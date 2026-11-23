using Avalonia.FreeDesktop.UbuntuTouch;
using Avalonia.FreeDesktop.DBusIme.Maliit;
using Avalonia.Input;
using Avalonia.Input.TextInput;

namespace Avalonia.FreeDesktop.UnitTests;

public class UbuntuTouchPlatformTests
{
    [Theory]
    [InlineData("ID=ubuntu\nVARIANT_ID=touch", "", "", true)]
    [InlineData("ID=ubports", "", "", true)]
    [InlineData("ID=ubuntu", "base: system-image.ubports.com\nchannel: 24.04-2.x/arm64/stable", "Lomiri", true)]
    [InlineData("ID=ubuntu", "", "Lomiri", false)]
    [InlineData("ID=ubuntu", "", "GNOME", false)]
    [InlineData("ID=debian", "channel: ubports/24.04", "GNOME", false)]
    public void DetectionRequiresDistributionOrImageEvidence(string os, string image, string desktop, bool expected)
        => Assert.Equal(expected, UbuntuTouchPlatform.Detect(os, image, desktop));

    [Theory]
    [InlineData("24", 3d)]
    [InlineData("8", 1d)]
    [InlineData("48", 6d)]
    [InlineData("NaN", null)]
    [InlineData("Infinity", null)]
    [InlineData("7", null)]
    [InlineData("49", null)]
    public void GridScaleIsAbsoluteAndBounded(string grid, double? expected)
        => Assert.Equal(expected, UbuntuTouchPlatform.GetScale(grid));

    [Theory]
    [InlineData(TextInputContentType.Normal)]
    [InlineData(TextInputContentType.Password)]
    [InlineData(TextInputContentType.Pin)]
    public void SensitiveStateOmitsTextAndOffsets(TextInputContentType type)
    {
        var state = new MaliitWidgetState(new TextInputOptions { ContentType = type, IsSensitive = type == TextInputContentType.Normal }, "secret", 6, 0, 123, default);
        var values = state.ToProperties();
        Assert.False(values.ContainsKey("surroundingText"));
        Assert.False(values.ContainsKey("cursorPosition"));
        Assert.False(values.ContainsKey("anchorPosition"));
        Assert.False(values["predictionEnabled"].GetBool());
        Assert.Equal("", state.SelectedText);
    }

    [Theory]
    [InlineData(TextInputContentType.Number, 0x20000)]
    [InlineData(TextInputContentType.Digits, 0x10000)]
    [InlineData(TextInputContentType.Pin, 0x10000)]
    [InlineData(TextInputContentType.Email, 0x200000)]
    [InlineData(TextInputContentType.Url, 0x400000)]
    public void OptionsUseQtInputHints(TextInputContentType type, int expected)
    {
        var state = new MaliitWidgetState(new TextInputOptions { ContentType = type, Lowercase = true }, "", 0, 0, 1, default);
        var hints = state.ToProperties()["maliit-inputmethod-hints"].GetInt32();
        Assert.Equal(expected, hints & 0xffff0000);
        Assert.NotEqual(0, hints & 0x20);
        Assert.Equal(0, hints & 0x8);
    }

    [Theory]
    [InlineData(0x01000003, 0xff08)]
    [InlineData(0x01000004, 0xff0d)]
    [InlineData(0x01000007, 0xffff)]
    [InlineData(0x01000012, 0xff51)]
    public void QtKeysAreConvertedToX11(int qt, int x11)
    {
        Assert.True(MaliitKeyMapper.TryMap(new InputKey(6, qt, 0x04000000, "", false, 1, 0), out var key));
        Assert.Equal(x11, key.KeyVal);
        Assert.Equal(KeyModifiers.Control, key.Modifiers);
        Assert.False(MaliitKeyMapper.TryMap(new InputKey(6, qt, 0, "", false, 1, 1), out _));
    }
}
