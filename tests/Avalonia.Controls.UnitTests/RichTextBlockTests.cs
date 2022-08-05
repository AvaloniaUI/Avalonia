using Avalonia.Controls.Documents;
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
    }
}
