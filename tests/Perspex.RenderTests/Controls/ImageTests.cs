





#if PERSPEX_CAIRO
namespace Perspex.Cairo.RenderTests.Controls
#else
namespace Perspex.Direct2D1.RenderTests.Controls
#endif
{
    using System.IO;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Media.Imaging;
    using Xunit;

    public class ImageTests : TestBase
    {
        private Bitmap bitmap;

        public ImageTests()
            : base(@"Controls\Image")
        {
            this.bitmap = new Bitmap(Path.Combine(this.OutputPath, "test.png"));
        }

        [Fact]
        public void Image_Stretch_None()
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
                        Source = this.bitmap,
                        Stretch = Stretch.None,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void Image_Stretch_Fill()
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
                        Source = this.bitmap,
                        Stretch = Stretch.Fill,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void Image_Stretch_Uniform()
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
                        Source = this.bitmap,
                        Stretch = Stretch.Uniform,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void Image_Stretch_UniformToFill()
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
                        Source = this.bitmap,
                        Stretch = Stretch.UniformToFill,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
