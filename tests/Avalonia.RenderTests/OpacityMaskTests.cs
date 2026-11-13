using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Threading.Tasks;

namespace Avalonia.Skia.RenderTests
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
                Background = Brushes.Transparent,
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
                Background = Brushes.Transparent,
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
        public async Task Nested_Opacity_Masks_Are_Combined()
        {
            var target = new Canvas
            {
                // Fades out from left to right.
                OpacityMask = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromUInt32(0xffffffff), 0),
                        new GradientStop(Color.FromUInt32(0x00ffffff), 1)
                    }
                },
                Width = 120,
                Height = 120,
                Background = Brushes.Transparent,
                Children =
                {
                    new Border
                    {
                        // Fades out from top to bottom, on top of the parent's mask.
                        OpacityMask = new LinearGradientBrush
                        {
                            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                            GradientStops =
                            {
                                new GradientStop(Color.FromUInt32(0xffffffff), 0),
                                new GradientStop(Color.FromUInt32(0x00ffffff), 1)
                            }
                        },
                        Width = 80,
                        Height = 50,
                        [Canvas.LeftProperty] = 20,
                        [Canvas.TopProperty] = 10,
                        Background = Brushes.Red,
                        Child = new Border
                        {
                            // Hides the left half of its content, on top of both ancestor masks.
                            OpacityMask = new LinearGradientBrush
                            {
                                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                                GradientStops =
                                {
                                    new GradientStop(Color.FromUInt32(0x00ffffff), 0.5),
                                    new GradientStop(Color.FromUInt32(0xffffffff), 0.5)
                                }
                            },
                            Margin = new Thickness(10),
                            Background = Brushes.Blue
                        }
                    },
                    new Border
                    {
                        // Drawn after the nested masks are popped: only the parent's mask applies.
                        Width = 80,
                        Height = 40,
                        [Canvas.LeftProperty] = 20,
                        [Canvas.TopProperty] = 70,
                        Background = Brushes.Green
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
