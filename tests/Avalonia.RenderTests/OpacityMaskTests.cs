using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Threading.Tasks;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class OpacityMaskTests : TestBase
    {
        public OpacityMaskTests()
            : base("OpacityMask")
        {
        }

        [Fact]
        public async Task Opacity_Mask_Masks_Element()
        {
            var target = new Canvas
            {
                OpacityMask = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromUInt32(0xffffffff), 0),
                        new GradientStop(Color.FromUInt32(0x00ffffff), 1)
                    }
                },
                Width = 76,
                Height = 76,
                Children =
                {
                    new Path
                    {
                        Width = 32,
                        Height = 40,
                        [Canvas.LeftProperty] = 23,
                        [Canvas.TopProperty] = 18,
                        Stretch = Stretch.Fill,
                        Fill = Brushes.Red,
                        Data = StreamGeometry.Parse("F1 M 27,18L 23,26L 33,30L 24,38L 33,46L 23,50L 27,58L 45,58L 55,38L 45,18L 27,18 Z")
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task RenderTransform_Applies_To_Opacity_Mask()
        {
            var target = new Canvas
            {
                OpacityMask = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromUInt32(0xffffffff), 0),
                        new GradientStop(Color.FromUInt32(0x00ffffff), 1)
                    }
                },
                RenderTransform = new RotateTransform(90),
                Width = 76,
                Height = 76,
                Children =
                {
                    new Path
                    {
                        Width = 32,
                        Height = 40,
                        [Canvas.LeftProperty] = 23,
                        [Canvas.TopProperty] = 18,
                        Stretch = Stretch.Fill,
                        Fill = Brushes.Red,
                        Data = StreamGeometry.Parse("F1 M 27,18L 23,26L 33,30L 24,38L 33,46L 23,50L 27,58L 45,58L 55,38L 45,18L 27,18 Z")
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
