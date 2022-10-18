using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class RichTextBlockTests
    {
        [Fact]
        public void Changing_InlinesCollection_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new RichTextBlock();

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines.Add(new Run("Hello"));

                Assert.False(target.IsMeasureValid);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Changing_Inlines_Properties_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new RichTextBlock();

                var inline = new Run("Hello");

                target.Inlines.Add(inline);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                inline.Foreground = Brushes.Green;

                Assert.False(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new RichTextBlock();

                var inlines = new InlineCollection { new Run("Hello") };

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines = inlines;

                Assert.False(target.IsMeasureValid);
            }
        }

        [Fact]
        public void Changing_Inlines_Should_Reset_Inlines_Parent()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new RichTextBlock();

                var run = new Run("Hello");

                target.Inlines.Add(run);

                target.Measure(Size.Infinity);

                Assert.True(target.IsMeasureValid);

                target.Inlines = null;

                Assert.Null(run.Parent);

                target.Inlines = new InlineCollection { run };

                Assert.Equal(target, run.Parent);
            }
        }

        [Fact]
        public void InlineUIContainer_Child_Schould_Be_Arranged()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new RichTextBlock();

                var button = new Button { Content = "12345678" };

                button.Template = new FuncControlTemplate<Button>((parent, scope) =>           
                        new TextBlock
                        {
                            Name = "PART_ContentPresenter",
                            [!TextBlock.TextProperty] = parent[!ContentControl.ContentProperty],
                        }.RegisterInNameScope(scope)                                       
                );

                target.Inlines!.Add("123456");
                target.Inlines.Add(new InlineUIContainer(button));
                target.Inlines.Add("123456");

                target.Measure(Size.Infinity);

                Assert.True(button.IsMeasureValid);
                Assert.Equal(80, button.DesiredSize.Width);

                target.Arrange(new Rect(new Size(200, 50)));

                Assert.True(button.IsArrangeValid);

                Assert.Equal(60, button.Bounds.Left);
            }
        }
    }
}
