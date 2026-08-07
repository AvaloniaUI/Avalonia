using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
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

        [Fact]
        public void TextPresenter_Should_Use_Preedit_Segment_Decorations()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    PreeditText = "abcdef",
                    PreeditTextSegments =
                    [
                        new TextInputMethodPreeditSegment(0, 2, TextInputMethodPreeditSegmentKind.InactiveClause),
                        new TextInputMethodPreeditSegment(2, 2, TextInputMethodPreeditSegmentKind.ActiveClause),
                        new TextInputMethodPreeditSegment(4, 2, TextInputMethodPreeditSegmentKind.InactiveClause)
                    ]
                };

                presenter.Measure(Size.Infinity);

                var runs = presenter.TextLayout.TextLines
                    .SelectMany(x => x.TextRuns)
                    .Where(x => x.Length > 0)
                    .ToArray();

                Assert.Equal(3, runs.Length);
                AssertPreeditDecoration(runs[0].Properties!.TextDecorations, 1, true);
                AssertPreeditDecoration(runs[1].Properties!.TextDecorations, 2, false);
                AssertPreeditDecoration(runs[2].Properties!.TextDecorations, 1, true);
            }
        }

        [Fact]
        public void TextPresenter_Should_Fall_Back_To_Default_Preedit_Underline_When_Segments_Are_Missing()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    PreeditText = "abcdef"
                };

                presenter.Measure(Size.Infinity);

                var run = presenter.TextLayout.TextLines
                    .SelectMany(x => x.TextRuns)
                    .Single(x => x.Length > 0);

                var decoration = Assert.Single(run.Properties!.TextDecorations!);
                Assert.Equal(1, decoration.StrokeThickness);
                Assert.Null(decoration.StrokeDashArray);
            }
        }

        [Fact]
        public void TextPresenter_Should_Move_Caret_Within_Preedit_Text()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var presenter = new TextPresenter
                {
                    Text = "zz",
                    CaretIndex = 1,
                    PreeditText = "abcdef",
                    PreeditTextCursorPosition = 1
                };

                presenter.Measure(Size.Infinity);

                var firstCursorX = presenter.GetCursorRectangle().X;

                presenter.PreeditTextCursorPosition = 4;

                var secondCursorX = presenter.GetCursorRectangle().X;

                Assert.True(secondCursorX > firstCursorX);
            }
        }

        private static void AssertPreeditDecoration(TextDecorationCollection? decorations, double strokeThickness, bool dotted)
        {
            var decoration = Assert.Single(decorations!);

            Assert.Equal(strokeThickness, decoration.StrokeThickness);

            if (dotted)
            {
                Assert.Equal(new[] { 1d, 2d }, decoration.StrokeDashArray!);
            }
            else
            {
                Assert.Null(decoration.StrokeDashArray);
            }
        }
    }
}
