using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Shapes
{
    public class PathTests
    {
        [Fact]
        public void Path_With_Null_Data_Does_Not_Throw_On_Measure()
        {
            var target = new Path();

            target.Measure(Size.Infinity);
        }

        [Fact]
        public void Subscribes_To_Geometry_Changes()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var target = new Path { Data = geometry };

            var root = new TestRoot(target);

            target.Measure(Size.Infinity);
            Assert.True(target.IsMeasureValid);

            geometry.Rect = new Rect(0, 0, 20, 20);

            Assert.False(target.IsMeasureValid);

            root.Child = null;
        }
    }
}
