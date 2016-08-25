// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests.Controls
#elif AVALONIA_SKIA
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
                        Source = _bitmap,
                        Stretch = Stretch.None,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
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
                        Source = _bitmap,
                        Stretch = Stretch.Fill,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
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
                        Source = _bitmap,
                        Stretch = Stretch.Uniform,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
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
                        Source = _bitmap,
                        Stretch = Stretch.UniformToFill,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
