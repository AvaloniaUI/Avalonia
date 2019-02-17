// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
            var target = new Image();
            target.Stretch = Stretch.None;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(50, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Fill_Stretch()
        {
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
            var target = new Image();
            target.Stretch = Stretch.Fill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(50, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_Uniform_Stretch()
        {
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
            var target = new Image();
            target.Stretch = Stretch.Uniform;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(25, 50), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Correct_Size_For_UniformToFill_Stretch()
        {
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
            var target = new Image();
            target.Stretch = Stretch.UniformToFill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));

            Assert.Equal(new Size(50, 50), target.DesiredSize);
        }

        [Fact]
        public void Arrange_Should_Return_Correct_Size_For_No_Stretch()
        {
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
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
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
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
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
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
            var bitmap = Mock.Of<IBitmap>(x => x.PixelSize == new PixelSize(50, 100));
            var target = new Image();
            target.Stretch = Stretch.UniformToFill;
            target.Source = bitmap;

            target.Measure(new Size(50, 50));
            target.Arrange(new Rect(0, 0, 25, 100));

            Assert.Equal(new Size(25, 100), target.Bounds.Size);
        }
    }
}
