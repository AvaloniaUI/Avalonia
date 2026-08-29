using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class TextPresenter_Tests : ScopedTestBase
    {
        [Fact]
        public void TextPresenter_Can_Contain_Null_With_Password_Char_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextPresenter
                {
                    PasswordChar = '*'
                };

                Assert.NotNull(target.TextLayout);
            }
        }

        [Fact]
        public void TextPresenter_Can_Contain_Null_WithOut_Password_Char_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {

                var target = new TextPresenter();

                Assert.NotNull(target.TextLayout);
            }
        }

        [Fact]
        public void Text_Presenter_Replaces_Formatted_Text_With_Password_Char()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {

                var target = new TextPresenter { PasswordChar = '*', Text = "Test" };

                target.Measure(Size.Infinity);

                Assert.NotNull(target.TextLayout);

                var actual = string.Join(null,
                    target.TextLayout.TextLines.SelectMany(x => x.TextRuns).Select(x => x.Text.ToString()));

                Assert.Equal("****", actual);
            }
        }
        
        [Theory]
        [InlineData(FontStretch.Condensed)]
        [InlineData(FontStretch.Expanded)]
        [InlineData(FontStretch.Normal)]
        [InlineData(FontStretch.ExtraCondensed)]
        [InlineData(FontStretch.SemiCondensed)]
        [InlineData(FontStretch.ExtraExpanded)]
        [InlineData(FontStretch.SemiExpanded)]
        [InlineData(FontStretch.UltraCondensed)]
        [InlineData(FontStretch.UltraExpanded)]
        public void TextPresenter_Should_Use_FontStretch_Property(FontStretch fontStretch)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter { FontStretch = fontStretch, Text = "test" };
                Assert.NotNull(presenter.TextLayout);
                Assert.Equal(1, presenter.TextLayout.TextLines.Count);
                Assert.Equal(1, presenter.TextLayout.TextLines[0].TextRuns.Count);
                Assert.NotNull(presenter.TextLayout.TextLines[0].TextRuns[0].Properties);
                Assert.Equal(fontStretch, presenter.TextLayout.TextLines[0].TextRuns[0].Properties!.Typeface.Stretch);
            }
        }

        [Fact]
        public void Measure_And_Arrange_Should_Use_WidthIncludingTrailingWhitespace_For_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "fy",
                    FontStyle = FontStyle.Italic,
                    FontSize = 48,
                    UseLayoutRounding = false
                };

                presenter.Measure(Size.Infinity);

                var expectedSize = new Size(presenter.TextLayout.WidthIncludingTrailingWhitespace, presenter.TextLayout.Height);

                Assert.Equal(expectedSize, presenter.DesiredSize);

                presenter.Arrange(new Rect(default, presenter.DesiredSize));

                Assert.Equal(new Rect(default, expectedSize), presenter.Bounds);
            }
        }
    }
}
