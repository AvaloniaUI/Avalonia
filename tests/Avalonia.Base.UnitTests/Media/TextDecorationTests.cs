using Avalonia.Media;
using Xunit;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Media
{
    public class TextDecorationTests : ScopedTestBase
    {
        [Fact]
        public void Should_Parse_TextDecorations()
        {
            var baseline = TextDecorationCollection.Parse("baseline");

            Assert.Equal(TextDecorationLocation.Baseline, baseline[0].Location);

            var underline = TextDecorationCollection.Parse("underline");

            Assert.Equal(TextDecorationLocation.Underline, underline[0].Location);

            var overline = TextDecorationCollection.Parse("overline");

            Assert.Equal(TextDecorationLocation.Overline, overline[0].Location);

            var strikethrough = TextDecorationCollection.Parse("strikethrough");

            Assert.Equal(TextDecorationLocation.Strikethrough, strikethrough[0].Location);
        }
    }
}
