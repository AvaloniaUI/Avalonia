using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.VisualTree
{
    public class VisualExtensions_GetTransformedBounds
    {
        [Fact]
        public void Root()
        {
            var root = new Border
            {
                Width = 100,
                Height = 123,
            };

            Layout(root);

            Assert.Equal(
                new TransformedBounds(
                    new Rect(0, 0, 100, 123),
                    new Rect(0, 0, 100, 123),
                    Matrix.Identity),
                root.GetTransformedBounds());
        }

        [Fact]
        public void Depth_1_No_Transform_Or_Clip()
        {
            Border target;
            var root = new Border
            {
                Width = 1000,
                Height = 1000,
                Child = target = new Border
                {
                    Width = 500,
                    Height = 500,
                }
            };

            Layout(root);

            Assert.Equal(
                new TransformedBounds(
                    new Rect(0, 0, 500, 500),
                    new Rect(0, 0, 1000, 1000),
                    Matrix.CreateTranslation(250, 250)),
                target.GetTransformedBounds());
        }

        [Fact]
        public void Depth_2_No_Transform_Or_Clip()
        {
            Border target;
            var root = new Border
            {
                Width = 1000,
                Height = 1000,
                Child = new Border
                {
                    Width = 800,
                    Height = 800,
                    Child = target = new Border
                    {
                        Width = 500,
                        Height = 500,
                    }
                }
            };

            Layout(root);

            Assert.Equal(
                new TransformedBounds(
                    new Rect(0, 0, 500, 500),
                    new Rect(0, 0, 1000, 1000),
                    Matrix.CreateTranslation(250, 250)),
                target.GetTransformedBounds());
        }

        [Fact]
        public void Depth_2_No_Transform_With_Clip()
        {
            Border target;
            var root = new Border
            {
                Width = 1000,
                Height = 1000,
                Child = new Border
                {
                    Width = 800,
                    Height = 800,
                    ClipToBounds = true,
                    Child = target = new Border
                    {
                        Width = 500,
                        Height = 500,
                    }
                }
            };

            Layout(root);

            Assert.Equal(
                new TransformedBounds(
                    new Rect(0, 0, 500, 500),
                    new Rect(100, 100, 800, 800),
                    Matrix.CreateTranslation(250, 250)),
                target.GetTransformedBounds());
        }

        [Fact]
        public void Depth_2_Transformed_Clip()
        {
            Border target;
            var root = new Border
            {
                Width = 1000,
                Height = 1000,
                Child = new Border
                {
                    Width = 800,
                    Height = 800,
                    ClipToBounds = true,
                    RenderTransform = new MatrixTransform(Matrix.CreateTranslation(10, 20)),
                    Child = target = new Border
                    {
                        Width = 500,
                        Height = 500,
                    }
                }
            };

            Layout(root);

            Assert.Equal(
                new TransformedBounds(
                    new Rect(0, 0, 500, 500),
                    new Rect(110, 120, 800, 800),
                    Matrix.CreateTranslation(260, 270)),
                target.GetTransformedBounds());
        }

        private void Layout(Control c)
        {
            c.Measure(Size.Infinity);
            c.Arrange(new Rect(c.DesiredSize));
        }
    }
}
