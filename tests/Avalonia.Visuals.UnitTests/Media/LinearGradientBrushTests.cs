using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class LinearGradientBrushTests
    {
        [Fact]
        public void Changing_StartPoint_Raises_Changed()
        {
            var bitmap1 = Mock.Of<IBitmap>();
            var bitmap2 = Mock.Of<IBitmap>();
            var target = new LinearGradientBrush();
            var raised = false;

            target.StartPoint = new RelativePoint();
            target.Changed += (s, e) => raised = true;
            target.StartPoint = new RelativePoint(10, 10, RelativeUnit.Absolute);

            Assert.True(raised);
        }

        [Fact]
        public void Changing_EndPoint_Raises_Changed()
        {
            var bitmap1 = Mock.Of<IBitmap>();
            var bitmap2 = Mock.Of<IBitmap>();
            var target = new LinearGradientBrush();
            var raised = false;

            target.EndPoint = new RelativePoint();
            target.Changed += (s, e) => raised = true;
            target.EndPoint = new RelativePoint(10, 10, RelativeUnit.Absolute);

            Assert.True(raised);
        }

        [Fact]
        public void Changing_GradientStops_Raises_Changed()
        {
            var bitmap1 = Mock.Of<IBitmap>();
            var bitmap2 = Mock.Of<IBitmap>();
            var target = new LinearGradientBrush();
            var raised = false;

            target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
            target.Changed += (s, e) => raised = true;
            target.GradientStops = new GradientStops { new GradientStop(Colors.Green, 0) };

            Assert.True(raised);
        }

        [Fact]
        public void Adding_GradientStop_Raises_Changed()
        {
            var bitmap1 = Mock.Of<IBitmap>();
            var bitmap2 = Mock.Of<IBitmap>();
            var target = new LinearGradientBrush();
            var raised = false;

            target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
            target.Changed += (s, e) => raised = true;
            target.GradientStops.Add(new GradientStop(Colors.Green, 1));

            Assert.True(raised);
        }

        [Fact]
        public void Changing_GradientStop_Offset_Raises_Changed()
        {
            var bitmap1 = Mock.Of<IBitmap>();
            var bitmap2 = Mock.Of<IBitmap>();
            var target = new LinearGradientBrush();
            var raised = false;

            target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
            target.Changed += (s, e) => raised = true;
            target.GradientStops[0].Offset = 0.5;

            Assert.True(raised);
        }
    }
}
