using System.Globalization;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class MatrixTests
    {
        [Fact]
        public void Parse_Parses()
        {
            var matrix = Matrix.Parse("1,2,3,-4,5 6");
            var expected = new Matrix(1, 2, 3, -4, 5, 6);
            Assert.Equal(expected, matrix);
        }

        [Fact]
        public void Can_Decompose_Translation()
        {
            var matrix = Matrix.CreateTranslation(5, 10);

            var result = Matrix.TryDecomposeTransform(matrix, out Matrix.Decomposed decomposed);

            Assert.Equal(true, result);
            Assert.Equal(5, decomposed.Translate.X);
            Assert.Equal(10, decomposed.Translate.Y);
        }

        [Fact]
        public void Can_Decompose_Angle()
        {
            var angleRad = MathUtilities.Deg2Rad(30);

            var matrix = Matrix.CreateRotation(angleRad);

            var result = Matrix.TryDecomposeTransform(matrix, out Matrix.Decomposed decomposed);

            Assert.Equal(true, result);
            Assert.Equal(angleRad, decomposed.Angle);
        }

        [Theory]
        [InlineData(1d, 1d)]
        [InlineData(-1d, 1d)]
        [InlineData(1d, -1d)]
        [InlineData(5d, 10d)]
        public void Can_Decompose_Scale(double x, double y)
        {
            var matrix = Matrix.CreateScale(x, y);

            var result = Matrix.TryDecomposeTransform(matrix, out Matrix.Decomposed decomposed);

            Assert.Equal(true, result);
            Assert.Equal(x, decomposed.Scale.X);
            Assert.Equal(y, decomposed.Scale.Y);
        }
    }
}
