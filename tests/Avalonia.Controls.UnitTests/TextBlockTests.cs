using System;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBlockTests
    {
        [Fact]
        public void DefaultBindingMode_Should_Be_OneWay()
        {
            Assert.Equal(
                BindingMode.OneWay,
                TextBlock.TextProperty.GetMetadata(typeof(TextBlock)).DefaultBindingMode);
        }

        [Fact]
        public void Default_Text_Value_Should_Be_EmptyString()
        {
            var textBlock = new TextBlock();

            Assert.Equal(
                "",
                textBlock.Text);
        }

        [Fact]
        public void Changing_Background_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new TextBlock()
            {
                Background = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.Invocations.Clear();

            ((SolidColorBrush)target.Background).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }

        [Fact]
        public void Changing_Foreground_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new TextBlock()
            {
                Foreground = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.Invocations.Clear();

            ((SolidColorBrush)target.Foreground).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }

        [Fact]
        public void Changing_InlinesCollection_Should_Invalidate_Measure()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new TextBlock();

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
                var target = new TextBlock();

                var inline = new Run("Hello");
                
                target.Inlines.Add(inline);

                target.Measure(Size.Infinity);
           
                Assert.True(target.IsMeasureValid);

                inline.Text = "1337";
                
                Assert.False(target.IsMeasureValid);
            }
        }
    }
}
