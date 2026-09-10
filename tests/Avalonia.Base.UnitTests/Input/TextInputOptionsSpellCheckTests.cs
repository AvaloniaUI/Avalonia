using Avalonia.Input.TextInput;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class TextInputOptionsSpellCheckTests : ScopedTestBase
    {
        [Fact]
        public void Unset_Is_Allowed_For_Platform_Keyboards_But_Not_Requested_From_The_Framework()
        {
            var options = new TextInputOptions();

            Assert.True(options.IsSpellCheckAllowed());

            Assert.False(options.IsSpellCheckRequested());
        }

        [Fact]
        public void Explicit_True_Requests_The_Framework_Checker()
        {
            var options = new TextInputOptions { IsSpellCheckEnabled = true };

            Assert.True(options.IsSpellCheckAllowed());
            Assert.True(options.IsSpellCheckRequested());
        }

        [Fact]
        public void Explicit_False_Disables_Both()
        {
            var options = new TextInputOptions { IsSpellCheckEnabled = false };

            Assert.False(options.IsSpellCheckAllowed());
            Assert.False(options.IsSpellCheckRequested());
        }

        [Theory]
        [InlineData(TextInputContentType.Password)]
        [InlineData(TextInputContentType.Pin)]
        [InlineData(TextInputContentType.Number)]
        [InlineData(TextInputContentType.Digits)]
        [InlineData(TextInputContentType.Url)]
        [InlineData(TextInputContentType.Email)]
        public void Non_Natural_Content_Types_Are_Never_Allowed_Even_When_Enabled(TextInputContentType contentType)
        {
            var options = new TextInputOptions { IsSpellCheckEnabled = true, ContentType = contentType };

            Assert.False(options.IsSpellCheckAllowed());
            Assert.False(options.IsSpellCheckRequested());
        }

        [Theory]
        [InlineData(TextInputContentType.Normal)]
        [InlineData(TextInputContentType.Alpha)]
        [InlineData(TextInputContentType.Name)]
        [InlineData(TextInputContentType.Search)]
        [InlineData(TextInputContentType.Social)]
        public void Natural_Content_Types_Are_Allowed(TextInputContentType contentType)
        {
            var options = new TextInputOptions { IsSpellCheckEnabled = true, ContentType = contentType };

            Assert.True(options.IsSpellCheckAllowed());
            Assert.True(options.IsSpellCheckRequested());
        }

        [Fact]
        public void Sensitive_Input_And_Password_Char_Are_Never_Allowed()
        {
            Assert.False(new TextInputOptions { IsSpellCheckEnabled = true, IsSensitive = true }.IsSpellCheckAllowed());
            Assert.False(new TextInputOptions { IsSpellCheckEnabled = true }.IsSpellCheckAllowed(hasPasswordChar: true));
        }
    }
}
