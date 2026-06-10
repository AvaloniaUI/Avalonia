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

        [Fact]
        public void SelectedText_Should_Reflect_Current_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    Text = "Hello World!"
                };

                Assert.Equal(string.Empty, target.SelectedText);

                target.SelectionStart = 0;
                target.SelectionEnd = 5;
                Assert.Equal("Hello", target.SelectedText);

                target.SelectionStart = 6;
                target.SelectionEnd = 11;
                Assert.Equal("World", target.SelectedText);

                target.ClearSelection();
                Assert.Equal(string.Empty, target.SelectedText);
            }
        }

        [Fact]
        public void SelectedText_Should_Update_When_Text_Change()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    Text = "Hello World!"
                };

                target.SelectionStart = 0;
                target.SelectionEnd = 5;
                Assert.Equal("Hello", target.SelectedText);

                target.Text = "Goodbye World!";
                Assert.Equal("Goodb", target.SelectedText);

                target.Text = "Good";
                Assert.Equal("Good", target.SelectedText);
            }
        }

        [Fact]
        public void SelectedText_Should_Update_When_Inlines_Change()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock();

                var inlines1 = new InlineCollection();
                inlines1.Add(new Run("Foo"));
                inlines1.Add(new Run("Bar"));
                target.Inlines = inlines1;

                target.SelectionStart = 0;
                target.SelectionEnd = 6;
                Assert.Equal("FooBar", target.SelectedText);


                var inlines2 = new InlineCollection();
                inlines2.Add(new Run("Abc"));
                target.Inlines = inlines2;
                Assert.Equal("Abc", target.SelectedText);

                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                Assert.Equal("bc", target.SelectedText);

                target.Inlines.Add(new Run("Def"));
                Assert.Equal("bc", target.SelectedText);

                target.SelectionEnd = 6;
                Assert.Equal("bcDef", target.SelectedText);


                target.Inlines.RemoveAt(1);
                Assert.Equal(3, target.SelectionEnd);
                Assert.Equal("bc", target.SelectedText);

                target.Inlines.Clear();
                Assert.Equal(0, target.SelectionEnd);
                Assert.Equal(string.Empty, target.SelectedText);
            }
        }
    }
}
