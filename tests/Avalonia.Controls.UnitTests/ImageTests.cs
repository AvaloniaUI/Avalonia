using Moq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ImageTests
    {
        [Fact]
        public void Measure_Should_Return_Correct_Size_For_No_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.None;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(50, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Fill_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.Fill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(50, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Uniform_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.Uniform;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(25, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_UniformToFill_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.UniformToFill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(50, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_With_StretchDirection_DownOnly()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.StretchDirection = StretchDirection.DownOnly;
            target.Source = bitmap;

            target.Measure(new Size(150, 150));

            Assert.Equal(new Size(50, 100), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Infinite_Height()
        {
            var bitmap = CreateBitmap(50, 100);
            var image = new Image();
            image.Source = bitmap;

            image.Measure(new Size(200, double.PositiveInfinity));

            Assert.Equal(new Size(200, 400), image.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Infinite_Width()
        {
            var bitmap = CreateBitmap(50, 100);
            var image = new Image();
            image.Source = bitmap;

            image.Measure(new Size(double.PositiveInfinity, 400));

            Assert.Equal(new Size(200, 400), image.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Infinite_Width_Height()
        {
            var bitmap = CreateBitmap(50, 100);
            var image = new Image();
            image.Source = bitmap;

            image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(50, 100), image.DesiredSize);
        }

        [Fact]
        public void Arrange_Should_Return_Correct_Size_For_No_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.None;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));
            target.Arrange(new Rect(0, 0, 100, 400));

            Assert.Equal(new Size(50, 100), target.Bounds.Size);
        }

        [Fact]
        public void Arrange_Should_Return_Correct_Size_For_Fill_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.Fill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));
            target.Arrange(new Rect(0, 0, 25, 100));

            Assert.Equal(new Size(25, 100), target.Bounds.Size);
        }

        [Fact]
        public void Arrange_Should_Return_Correct_Size_For_Uniform_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.Uniform;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));
            target.Arrange(new Rect(0, 0, 25, 100));

            Assert.Equal(new Size(25, 50), target.Bounds.Size);
        }

        [Fact]
        public void Arrange_Should_Return_Correct_Size_For_UniformToFill_Stretch()
        {
            var bitmap = CreateBitmap(50, 100);
            var target = new Image();
            target.Stretch = Stretch.UniformToFill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));
            target.Arrange(new Rect(0, 0, 25, 100));

            Assert.Equal(new Size(25, 100), target.Bounds.Size);
        }

        private static IBitmap CreateBitmap(int width, int height)
        {
            return Mock.Of<IBitmap>(x => x.Size == new Size(width, height));
        }
    }
}
