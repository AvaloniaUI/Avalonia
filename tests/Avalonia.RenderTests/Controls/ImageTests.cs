using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class ImageTests : TestBase
    {
        private readonly Bitmap _bitmap;
        private readonly Bitmap _bitmap2;

        public ImageTests()
            : base(@"Controls\Image")
        {
            _bitmap = new Bitmap(Path.Combine(OutputPath, "test.png"));
            _bitmap2 = new Bitmap(Path.Combine(OutputPath, "test2.png"));
        }

        [Fact]
        public async Task Image_Stretch_None()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = new Image
                    {
                        Source = _bitmap,
                        Stretch = Stretch.None,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Image_Stretch_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = new Image
                    {
                        Source = _bitmap,
                        Stretch = Stretch.Fill,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Image_Stretch_Uniform()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = new Image
                    {
                        Source = _bitmap,
                        Stretch = Stretch.Uniform,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Image_Stretch_UniformToFill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = new Image
                    {
                        Source = _bitmap,
                        Stretch = Stretch.UniformToFill,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Image_Rotated_EdgeMode_Unspecified()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(32, 32),
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = _bitmap2,
                    Stretch = Stretch.Uniform,
                    RenderTransform = new RotateTransform(30),
                }
            };
            RenderOptions.SetEdgeMode(target, EdgeMode.Unspecified);

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Image_Rotated_EdgeMode_Antialias()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(32, 32),
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = _bitmap2,
                    Stretch = Stretch.Uniform,
                    RenderTransform = new RotateTransform(30),
                }
            };
            RenderOptions.SetEdgeMode(target, EdgeMode.Antialias);

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Image_Rotated_EdgeMode_Aliased()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(32, 32),
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = _bitmap2,
                    Stretch = Stretch.Uniform,
                    RenderTransform = new RotateTransform(30),
                }
            };
            RenderOptions.SetEdgeMode(target, EdgeMode.Aliased);

            await RenderToFile(target);
            CompareImages();
        }
    }
}
