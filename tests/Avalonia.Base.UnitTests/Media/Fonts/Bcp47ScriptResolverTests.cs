using System.Globalization;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class Bcp47ScriptResolverTests
    {
        [Theory]
        [InlineData("ja", "Jpan")]
        [InlineData("ja-JP", "Jpan")]
        [InlineData("ko", "Kore")]
        [InlineData("ko-KR", "Kore")]
        [InlineData("zh", "Hans")]
        [InlineData("zh-CN", "Hans")]
        [InlineData("zh-SG", "Hans")]
        [InlineData("zh-TW", "Hant")]
        [InlineData("zh-HK", "Hant")]
        [InlineData("zh-MO", "Hant")]
        [InlineData("zh-Hans-CN", "Hans")]
        [InlineData("zh-Hant-TW", "Hant")]
        [InlineData("ru", "Cyrl")]
        [InlineData("ru-RU", "Cyrl")]
        [InlineData("en", "Latn")]
        [InlineData("en-US", "Latn")]
        [InlineData("de-DE", "Latn")]
        [InlineData("ar", "Arab")]
        [InlineData("he", "Hebr")]
        [InlineData("th", "Thai")]
        [InlineData("el", "Grek")]
        [InlineData("sr-Cyrl", "Cyrl")]
        [InlineData("sr-Latn-RS", "Latn")]
        public void Resolves_Expected_Script_Subtag(string cultureName, string expected)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);

            Assert.Equal(expected, Bcp47ScriptResolver.GetScriptSubtag(culture));
        }

        [Fact]
        public void Invariant_Culture_Returns_Null()
        {
            Assert.Null(Bcp47ScriptResolver.GetScriptSubtag(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Null_Culture_Returns_Null()
        {
            Assert.Null(Bcp47ScriptResolver.GetScriptSubtag(null));
        }

        [Fact]
        public void Unknown_Language_Returns_Null()
        {
            // 'xx' is reserved as a private-use language tag; no canonical script.
            var culture = CultureInfo.GetCultureInfo("xx");

            Assert.Null(Bcp47ScriptResolver.GetScriptSubtag(culture));
        }
    }
}
