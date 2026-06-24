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
        [Fact]
        public void Selection_Spanning_InlineUIContainer_Returns_Correct_Text()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock();

                target.Inlines!.Add(new Run("foo"));
                target.Inlines!.Add(new InlineUIContainer(new Border()));
                target.Inlines!.Add(new Run("bar"));

                target.Measure(Size.Infinity);

                // In TextLayout, EmbeddedControlRun (from InlineUIContainer) occupies 1 character
                // position. After "foo" (3 chars) + InlineUIContainer (1 char), "bar" starts at
                // position 4. SelectionStart/End come from TextLayout.HitTestPoint so they must
                // align with the text returned by Inlines.Text.
                target.SelectionStart = 4;
                target.SelectionEnd = 7;

                Assert.Equal("bar", target.SelectedText);
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
