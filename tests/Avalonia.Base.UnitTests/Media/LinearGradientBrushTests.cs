using Avalonia.Media;
using Avalonia.Media.Imaging;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class LinearGradientBrushTests
    {
        [Fact]
        public void Changing_StartPoint_Raises_Invalidated()
        {
            var target = new LinearGradientBrush();

            target.StartPoint = new RelativePoint();

            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.StartPoint = new RelativePoint(10, 10, RelativeUnit.Absolute);
            });
        }

        [Fact]
        public void Changing_EndPoint_Raises_Invalidated()
        {
            var target = new LinearGradientBrush();

            target.EndPoint = new RelativePoint();
            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.EndPoint = new RelativePoint(10, 10, RelativeUnit.Absolute);
            });
        }

        [Fact]
        public void Changing_GradientStops_Raises_Invalidated()
        {
            var target = new LinearGradientBrush();

            target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.GradientStops = new GradientStops { new GradientStop(Colors.Green, 0) };
            });
        }

        [Fact]
        public void Adding_GradientStop_Raises_Invalidated()
        {
            var target = new LinearGradientBrush();

            target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.GradientStops.Add(new GradientStop(Colors.Green, 1));
            });
        }

        [Fact]
        public void Changing_GradientStop_Offset_Raises_Invalidated()
        {
            var target = new LinearGradientBrush();

            target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
            RenderResourceTestHelper.AssertResourceInvalidation(target, () =>
            {
                target.GradientStops[0].Offset = 0.5;
            });
        }
    }
}
