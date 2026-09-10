using Avalonia.Input.TextInput;
using NWayland.Protocols.TextInputUnstableV3;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

using Hint = ZwpTextInputV3.ContentHintEnum;
using Purpose = ZwpTextInputV3.ContentPurposeEnum;

public class TextInputOptionsConverterTests
{
    [Fact]
    public void Default_IsNoneNormal()
    {
        var (h, p) = TextInputOptionsConverter.Convert(new TextInputOptions());
        Assert.Equal(Hint.None, h);
        Assert.Equal(Purpose.Normal, p);
    }

    [Theory]
    [InlineData(TextInputContentType.Digits, Purpose.Digits)]
    [InlineData(TextInputContentType.Pin, Purpose.Pin)]
    [InlineData(TextInputContentType.Number, Purpose.Number)]
    [InlineData(TextInputContentType.Email, Purpose.Email)]
    [InlineData(TextInputContentType.Url, Purpose.Url)]
    [InlineData(TextInputContentType.Name, Purpose.Name)]
    public void ContentType_MapsPurpose(TextInputContentType ct, Purpose expected)
    {
        var (_, p) = TextInputOptionsConverter.Convert(new TextInputOptions { ContentType = ct });
        Assert.Equal(expected, p);
    }

    [Fact]
    public void Alpha_AddsLatinHint()
    {
        var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions { ContentType = TextInputContentType.Alpha });
        Assert.True((h & Hint.Latin) != 0);
    }

    [Fact]
    public void Password_ForcesHiddenAndSensitive_EvenWithShowSuggestions()
    {
        var (h, p) = TextInputOptionsConverter.Convert(new TextInputOptions
        {
            ContentType = TextInputContentType.Password,
            ShowSuggestions = true
        });
        Assert.Equal(Purpose.Password, p);
        Assert.True((h & Hint.HiddenText) != 0);
        Assert.True((h & Hint.SensitiveData) != 0);
        Assert.False((h & Hint.Completion) != 0);
        Assert.False((h & Hint.Spellcheck) != 0);
    }

    [Fact]
    public void Flags_ComposeIntoHint()
    {
        var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions
        {
            Multiline = true,
            IsSensitive = true,
            Lowercase = true,
            Uppercase = true,
            AutoCapitalization = true,
            ShowSuggestions = true,
        });
        Assert.True((h & Hint.Multiline) != 0);
        Assert.True((h & Hint.SensitiveData) != 0);
        Assert.True((h & Hint.Lowercase) != 0);
        Assert.True((h & Hint.Uppercase) != 0);
        Assert.True((h & Hint.AutoCapitalization) != 0);
        Assert.True((h & Hint.Completion) != 0);
        Assert.False((h & Hint.Spellcheck) != 0);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Spellcheck_Respects_Explicit_OptOut_Without_Disabling_Completion(bool? enabled, bool expected)
    {
        var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions
        {
            IsSpellCheckEnabled = enabled,
            ShowSuggestions = true,
        });
        Assert.Equal(expected, (h & Hint.Spellcheck) != 0);
        Assert.True((h & Hint.Completion) != 0);
    }

    [Theory]
    [InlineData(TextInputContentType.Password)]
    [InlineData(TextInputContentType.Pin)]
    [InlineData(TextInputContentType.Number)]
    [InlineData(TextInputContentType.Digits)]
    [InlineData(TextInputContentType.Url)]
    [InlineData(TextInputContentType.Email)]
    public void Non_Natural_Language_Content_Never_Requests_Spellcheck(TextInputContentType contentType)
    {
        var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions
        {
            ContentType = contentType,
            IsSpellCheckEnabled = true,
            ShowSuggestions = true,
        });
        Assert.False((h & Hint.Spellcheck) != 0);
    }

    [Fact]
    public void ShowSuggestionsFalse_OmitsCompletion()
    {
        var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions { ShowSuggestions = false });
        Assert.False((h & Hint.Completion) != 0);
        Assert.False((h & Hint.Spellcheck) != 0);
    }
}
