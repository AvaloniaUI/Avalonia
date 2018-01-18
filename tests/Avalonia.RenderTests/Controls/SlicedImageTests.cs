// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
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
    public class SlicedImageTests : TestBase
    {
        private readonly Bitmap _bitmap;

        public SlicedImageTests()
            : base(@"Controls\SlicedImage")
        {
            _bitmap = new Bitmap(Path.Combine(OutputPath, "slicedtest.png"));
        }

        [Fact]
        public async Task Sliced_Image()
        {
            Decorator target = new Decorator
            {
                Width = 100,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = new SlicedImage
                    {
                        Width = 100,
                        Height = 200,
                        Source = _bitmap,
                        Stretch = Stretch.Fill,
                        Right = 10,
                        Left = 10,
                        Top = 10,
                        Bottom = 10,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
