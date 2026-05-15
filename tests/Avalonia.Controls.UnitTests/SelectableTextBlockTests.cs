using System;
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
        [Theory]
        [InlineData(TextAlignment.Center)]
        [InlineData(TextAlignment.Right)]
        public void Dragging_Selection_Should_Reach_End_Of_Text_When_Text_Is_Aligned(TextAlignment textAlignment)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    Width = 200,
                    Text = "Aligned text",
                    TextAlignment = textAlignment
                };

                var root = new TestRoot(target)
                {
                    ClientSize = new Size(300, 100)
                };

                root.Measure(root.ClientSize);
                root.Arrange(new Rect(root.ClientSize));
                root.ExecuteInitialLayoutPass();

                var line = target.TextLayout.TextLines[0];
                var mouse = new MouseTestHelper();
                var y = target.Bounds.Height / 2;

                mouse.Down(target, position: new Point(line.Start, y));
                mouse.Move(target, new Point(target.Bounds.Width - 1, y));

                Assert.Equal(target.Text!.Length, Math.Max(target.SelectionStart, target.SelectionEnd));
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
