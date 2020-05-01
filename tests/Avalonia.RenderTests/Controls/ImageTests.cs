using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class ImageTests : TestBase
    {
        private readonly Bitmap _bitmap;

        public ImageTests()
            : base(@"Controls\Image")
        {
            _bitmap = new Bitmap(Path.Combine(OutputPath, "test.png"));
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
    }
}
