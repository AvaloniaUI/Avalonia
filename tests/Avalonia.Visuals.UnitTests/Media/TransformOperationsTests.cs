using Avalonia.Media.Transformation;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class TransformOperationsTests
    {
        [Fact]
        public void Can_Parse_Compound_Operations()
        {
            var data = "scale(1,2) translate(3px,4px) rotate(5deg) skew(6deg,7deg)";

            var transform = TransformOperations.Parse(data);

            var operations = transform.Operations;

            Assert.Equal(TransformOperation.OperationType.Scale, operations[0].Type);
            Assert.Equal(1, operations[0].Data.Scale.X);
            Assert.Equal(2, operations[0].Data.Scale.Y);

            Assert.Equal(TransformOperation.OperationType.Translate, operations[1].Type);
            Assert.Equal(3, operations[1].Data.Translate.X);
            Assert.Equal(4, operations[1].Data.Translate.Y);

            Assert.Equal(TransformOperation.OperationType.Rotate, operations[2].Type);
            Assert.Equal(MathUtilities.Deg2Rad(5), operations[2].Data.Rotate.Angle);

            Assert.Equal(TransformOperation.OperationType.Skew, operations[3].Type);
            Assert.Equal(MathUtilities.Deg2Rad(6), operations[3].Data.Skew.X);
            Assert.Equal(MathUtilities.Deg2Rad(7), operations[3].Data.Skew.Y);
        }

        [Fact]
        public void Can_Parse_Matrix_Operation()
        {
            var data = "matrix(1,2,3,4,5,6)";

            var transform = TransformOperations.Parse(data);
        }

        [Theory]
        [InlineData(0d, 10d, 0d)]
        [InlineData(0.5d, 5d, 10d)]
        [InlineData(1d, 0d, 20d)]
        public void Can_Interpolate_Translation(double progress, double x, double y)
        {
            var from = TransformOperations.Parse("translateX(10px)");
            var to = TransformOperations.Parse("translateY(20px)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Translate, operations[0].Type);
            Assert.Equal(x, operations[0].Data.Translate.X);
            Assert.Equal(y, operations[0].Data.Translate.Y);
        }

        [Theory]
        [InlineData(0d, 10d, 0d)]
        [InlineData(0.5d, 5d, 10d)]
        [InlineData(1d, 0d, 20d)]
        public void Can_Interpolate_Scale(double progress, double x, double y)
        {
            var from = TransformOperations.Parse("scaleX(10)");
            var to = TransformOperations.Parse("scaleY(20)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Scale, operations[0].Type);
            Assert.Equal(x, operations[0].Data.Scale.X);
            Assert.Equal(y, operations[0].Data.Scale.Y);
        }

        [Theory]
        [InlineData(0d, 10d, 0d)]
        [InlineData(0.5d, 5d, 10d)]
        [InlineData(1d, 0d, 20d)]
        public void Can_Interpolate_Skew(double progress, double x, double y)
        {
            var from = TransformOperations.Parse("skewX(10deg)");
            var to = TransformOperations.Parse("skewY(20deg)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Skew, operations[0].Type);
            Assert.Equal(MathUtilities.Deg2Rad(x), operations[0].Data.Skew.X);
            Assert.Equal(MathUtilities.Deg2Rad(y), operations[0].Data.Skew.Y);
        }

        [Theory]
        [InlineData(0d, 10d)]
        [InlineData(0.5d, 15d)]
        [InlineData(1d,20d)]
        public void Can_Interpolate_Rotation(double progress, double angle)
        {
            var from = TransformOperations.Parse("rotate(10deg)");
            var to = TransformOperations.Parse("rotate(20deg)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Rotate, operations[0].Type);
            Assert.Equal(MathUtilities.Deg2Rad(angle), operations[0].Data.Rotate.Angle);
        }

        [Fact]
        public void Interpolation_Fallback_To_Matrix()
        {
            double progress = 0.5d;

            var from = TransformOperations.Parse("rotate(45deg)");
            var to = TransformOperations.Parse("translate(100px, 100px) rotate(1215deg)");

            var interpolated = TransformOperations.Interpolate(from, to, progress);

            var operations = interpolated.Operations;

            Assert.Single(operations);
            Assert.Equal(TransformOperation.OperationType.Matrix, operations[0].Type);
        }
    }
}
