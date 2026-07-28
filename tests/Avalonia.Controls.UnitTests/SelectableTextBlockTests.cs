using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SelectableTextBlockTests : ScopedTestBase
    {
        // Content: Run("foo") + InlineUIContainer + Run("bar")
        // Inlines.Text after fix: "foo\uFFFCbar" (indices 0-6)
        //   0='f', 1='o', 2='o', 3='\uFFFC' (embedded control), 4='b', 5='a', 6='r'
        [Theory]
        // Entirely before InlineUIContainer
        [InlineData(0, 3, "foo")]
        // Exactly the InlineUIContainer character
        [InlineData(3, 4, "\uFFFC")]
        // Up to and including InlineUIContainer (fencepost: last char before "bar")
        [InlineData(0, 4, "foo\uFFFC")]
        // Starting exactly after InlineUIContainer (fencepost: first char of "bar")
        [InlineData(4, 7, "bar")]
        // InlineUIContainer through end
        [InlineData(3, 7, "\uFFFCbar")]
        // Spanning InlineUIContainer (one char either side)
        [InlineData(2, 5, "o\uFFFCb")]
        // Entire content
        [InlineData(0, 7, "foo\uFFFCbar")]
        public void Selection_With_InlineUIContainer_Returns_Correct_Text(int start, int end, string expected)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock();

                target.Inlines!.Add(new Run("foo"));
                target.Inlines!.Add(new InlineUIContainer(new Border()));
                target.Inlines!.Add(new Run("bar"));

                target.Measure(Size.Infinity);

                // SelectionStart/End values correspond to TextLayout character positions.
                // EmbeddedControlRun occupies 1 position (TextRun.DefaultTextSourceLength),
                // and Inlines.Text now has a matching U+FFFC placeholder, so they stay in sync.
                target.SelectionStart = start;
                target.SelectionEnd = end;

                Assert.Equal(expected, target.SelectedText);
            }
        }

        [Fact]
        public void SelectionForeground_Should_Not_Reset_Run_Typeface_And_Style()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    SelectionForegroundBrush = Brushes.Red
                };

                var run = new Run("Hello")
                {
                    FontWeight = FontWeight.Bold,
                    FontStyle = FontStyle.Italic,
                    FontSize = 20
                };

                target.Inlines!.Add(run);

                target.Measure(Size.Infinity);

                target.SelectionStart = 0;
                target.SelectionEnd = run.Text!.Length;

                target.Measure(Size.Infinity);

                var textLayout = target.TextLayout;
                Assert.NotNull(textLayout);

                var textRuns = textLayout.TextLines
                    .SelectMany(l => l.TextRuns)
                    .OfType<ShapedTextRun>()
                    .ToList();

                Assert.NotEmpty(textRuns);

                var selectedRun = textRuns[0];
                var props = selectedRun.Properties;

                Assert.Equal(FontWeight.Bold, props.Typeface.Weight);
                Assert.Equal(FontStyle.Italic, props.Typeface.Style);

                Assert.Same(target.SelectionForegroundBrush, props.ForegroundBrush);
            }
        }

    }
}
