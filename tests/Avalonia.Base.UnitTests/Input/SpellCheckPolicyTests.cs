using Avalonia.Input.TextInput;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public class SpellCheckPolicyTests
{
    [Theory]
    [InlineData(TextInputContentType.Normal, true)]
    [InlineData(TextInputContentType.Alpha, true)]
    [InlineData(TextInputContentType.Name, true)]
    [InlineData(TextInputContentType.Search, true)]
    [InlineData(TextInputContentType.Social, true)]
    [InlineData(TextInputContentType.Digits, false)]
    [InlineData(TextInputContentType.Number, false)]
    [InlineData(TextInputContentType.Password, false)]
    [InlineData(TextInputContentType.Pin, false)]
    [InlineData(TextInputContentType.Url, false)]
    [InlineData(TextInputContentType.Email, false)]
    public void Content_Type_Decides_Whether_Spell_Checking_Is_Allowed(TextInputContentType contentType, bool expected)
    {
        Assert.Equal(expected, SpellCheckPolicy.IsAllowed(contentType, isSensitive: false));
    }

    [Fact]
    public void Sensitive_Input_Is_Never_Spell_Checked()
    {
        Assert.False(SpellCheckPolicy.IsAllowed(TextInputContentType.Normal, isSensitive: true));
    }

    [Fact]
    public void Password_Characters_Disable_Spell_Checking()
    {
        Assert.False(SpellCheckPolicy.IsAllowed(TextInputContentType.Normal, isSensitive: false, hasPasswordChar: true));
    }
}
