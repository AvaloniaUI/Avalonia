using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes
{
    public class RectangleTests
    {
        [Fact]
        public void Changing_Fill_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new Rectangle()
            {
                Fill = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.ResetCalls();

            ((SolidColorBrush)target.Fill).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }

        [Fact]
        public void Changing_Stroke_Brush_Color_Should_Invalidate_Visual()
        {
            var target = new Rectangle()
            {
                Stroke = new SolidColorBrush(Colors.Red),
            };

            var root = new TestRoot(target);
            var renderer = Mock.Get(root.Renderer);
            renderer.ResetCalls();

            ((SolidColorBrush)target.Stroke).Color = Colors.Green;

            renderer.Verify(x => x.AddDirty(target), Times.Once);
        }
    }
}
