using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GeometryBuilderTests
    {
        [Fact]
        public void RoundedRectKeypoints_InnerBorderEdge_Borders_Larger_Than_Corners_Test()
        {
            var bounds = new Rect(new Size(100, 100));
            var uniformBorders = 20.0;
            var uniformCorners = 10.0;
            var borderThickness = new Thickness(uniformBorders);
            var cornerRadius = new CornerRadius(uniformCorners);

            var points = GeometryBuilder.CalculateRoundedCornersRectangleV2(bounds, borderThickness, cornerRadius, BackgroundSizing.InnerBorderEdge);

            // Note the corner radius is smaller than the border thickness
            // This means they will be crushed to zero so only a simple rectangle is left

            Assert.Equal(new Point(uniformBorders, uniformBorders), points.LeftTop);
            Assert.Equal(new Point(uniformBorders, uniformBorders), points.TopLeft);
            Assert.Equal(new Point(100 - uniformBorders, uniformBorders), points.TopRight);
            Assert.Equal(new Point(100 - uniformBorders, uniformBorders), points.RightTop);
            Assert.Equal(new Point(100 - uniformBorders, 100 - uniformBorders), points.RightBottom);
            Assert.Equal(new Point(100 - uniformBorders, 100 - uniformBorders), points.BottomRight);
            Assert.Equal(new Point(uniformBorders, 100 - uniformBorders), points.BottomLeft);
            Assert.Equal(new Point(uniformBorders, 100 - uniformBorders), points.LeftBottom);

            Assert.False(points.IsRounded);
        }

        [Fact]
        public void RoundedRectKeypoints_OuterBorderEdge_Test()
        {
            var bounds = new Rect(new Size(100, 100));
            var uniformBorders = 20.0;
            var uniformCorners = 10.0;
            var borderThickness = new Thickness(uniformBorders);
            var cornerRadius = new CornerRadius(uniformCorners);

            var points = GeometryBuilder.CalculateRoundedCornersRectangleV2(bounds, borderThickness, cornerRadius, BackgroundSizing.OuterBorderEdge);

            Assert.Equal(new Point(0, uniformCorners), points.LeftTop);
            Assert.Equal(new Point(uniformCorners, 0), points.TopLeft);
            Assert.Equal(new Point(100 - uniformCorners, 0), points.TopRight);
            Assert.Equal(new Point(100, uniformCorners), points.RightTop);
            Assert.Equal(new Point(100, 100 - uniformCorners), points.RightBottom);
            Assert.Equal(new Point(100 - uniformCorners, 100), points.BottomRight);
            Assert.Equal(new Point(uniformCorners, 100), points.BottomLeft);
            Assert.Equal(new Point(0, 100 - uniformCorners), points.LeftBottom);

            Assert.True(points.IsRounded);
        }
    }
}
