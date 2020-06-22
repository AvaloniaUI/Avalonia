using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class MatrixTests
    {
        [Fact]
        public void Can_Parse()
        {
            var matrix = Matrix.Parse("1,2,3,-4,5 6");
            var expected = new Matrix(1, 2, 3, -4, 5, 6);
            Assert.Equal(expected, matrix);
        }

        [Fact]
        public void Singular_Has_No_Inverse()
        {
            var matrix = new Matrix(0, 0, 0, 0, 0, 0);

            Assert.False(matrix.HasInverse);
        }

        [Fact]
        public void Identity_Has_Inverse()
        {
            var matrix = Matrix.Identity;

            Assert.True(matrix.HasInverse);
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

        [Theory]
        [InlineData(30d)]
        [InlineData(0d)]
        [InlineData(90d)]
        [InlineData(270d)]
        public void Can_Decompose_Angle(double angleDeg)
        {
            var angleRad = MathUtilities.Deg2Rad(angleDeg);

            var matrix = Matrix.CreateRotation(angleRad);

            var result = Matrix.TryDecomposeTransform(matrix, out Matrix.Decomposed decomposed);

            Assert.Equal(true, result);

            var expected = NormalizeAngle(angleRad);
            var actual = NormalizeAngle(decomposed.Angle);

            Assert.Equal(expected, actual, 4);
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

        [Theory]
        [InlineData(10d, 10d)]
        [InlineData(-10d, 10d)]
        [InlineData(10d, -10d)]
        [InlineData(50d, 100d)]
        public void CreateRotation_With_Center_Point_Equals_TransformGroup(double x, double y)
        {
            var angle = Matrix.ToRadians(42);

            var expectedMatrix = Matrix.CreateTranslation(-x, -y)
                               * Matrix.CreateRotation(angle)
                               * Matrix.CreateTranslation(x, y);

            var actualMatrix = Matrix.CreateRotation(angle, x, y);

            Assert.True(MathUtilities.AreClose(expectedMatrix.M11, actualMatrix.M11));
            Assert.True(MathUtilities.AreClose(expectedMatrix.M21, actualMatrix.M21));
            Assert.True(MathUtilities.AreClose(expectedMatrix.M31, actualMatrix.M31));
            Assert.True(MathUtilities.AreClose(expectedMatrix.M12, actualMatrix.M12));
            Assert.True(MathUtilities.AreClose(expectedMatrix.M22, actualMatrix.M22));
            Assert.True(MathUtilities.AreClose(expectedMatrix.M32, actualMatrix.M32));
        }

        private static double NormalizeAngle(double rad)
        {
            double twoPi = 2 * Math.PI;

            while (rad < 0)
            {
                rad += twoPi;
            }

            while (rad > twoPi)
            {
                rad -= twoPi;
            }

            return rad;
        }
    }
}
