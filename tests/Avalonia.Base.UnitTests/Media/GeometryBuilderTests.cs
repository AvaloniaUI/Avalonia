using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GeometryBuilderTests
    {
        [Theory]
        [InlineData(20.0, 10.0)]
        [InlineData(10.0, 5.0)]
        [InlineData(2.0, 1.0)]
        [InlineData(1.0, 0.0)]
        public void CalculateRoundedCornersRectangleWinUI_InnerBorderEdge_Borders_Larger_Than_Corners_Test(
            double uniformBorders,
            double uniformCorners)
        {
            var bounds = new Rect(new Size(100, 100));
            var borderThickness = new Thickness(uniformBorders);
            var cornerRadius = new CornerRadius(uniformCorners);

            var points = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(bounds, borderThickness, cornerRadius, BackgroundSizing.InnerBorderEdge);

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

        [Theory]
        [InlineData(20.0, 10.0)]
        [InlineData(10.0, 5.0)]
        [InlineData(2.0, 1.0)]
        public void CalculateRoundedCornersRectangleWinUI_OuterBorderEdge_Borders_Larger_Than_Corners_Test(
            double uniformBorders,
            double uniformCorners)
        {
            var bounds = new Rect(new Size(100, 100));
            var borderThickness = new Thickness(uniformBorders);
            var cornerRadius = new CornerRadius(uniformCorners);

            var points = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(bounds, borderThickness, cornerRadius, BackgroundSizing.OuterBorderEdge);

            Assert.Equal(new Point(0, uniformBorders), points.LeftTop);
            Assert.Equal(new Point(uniformBorders, 0), points.TopLeft);
            Assert.Equal(new Point(100 - uniformBorders, 0), points.TopRight);
            Assert.Equal(new Point(100, uniformBorders), points.RightTop);
            Assert.Equal(new Point(100, 100 - uniformBorders), points.RightBottom);
            Assert.Equal(new Point(100 - uniformBorders, 100), points.BottomRight);
            Assert.Equal(new Point(uniformBorders, 100), points.BottomLeft);
            Assert.Equal(new Point(0, 100 - uniformBorders), points.LeftBottom);

            Assert.True(points.IsRounded);
        }
    }
}
