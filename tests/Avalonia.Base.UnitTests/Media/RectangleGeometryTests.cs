using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class RectangleGeometryTests
    {
        [Fact]
        public void Rectangle_With_Transform_Can_Be_Changed()
        {
            var target = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 100, 100),
                Transform = new RotateTransform(45),
            };

            target.Rect = new Rect(50, 50, 150, 150);
        }
    }
}
