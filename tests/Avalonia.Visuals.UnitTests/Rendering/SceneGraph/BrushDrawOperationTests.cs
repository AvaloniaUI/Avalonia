using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.SceneGraph;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class BrushDrawOperationTests
    {
        [Theory]
        [MemberData(nameof(BrushEqualsData))]
        public void BrushEquals_Returns_Correct_Result(IBrush left, IBrush right, double opacityBake, bool expected)
        {
            var result = BrushDrawOperation.BrushEquals(left?.ToImmutable(), right, opacityBake);
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> BrushEqualsData
        {
            get
            {
                var bitmap1 = Mock.Of<IBitmap>();
                var bitmap2 = Mock.Of<IBitmap>();
                var visual1 = Mock.Of<IVisual>();
                var visual2 = Mock.Of<IVisual>();

                return new[]
                {
                    new object[] { null, null, 1, true },
                    new object[] { new ImageBrush(bitmap1), new ImageBrush(bitmap1), 1, true },
                    new object[] { new ImageBrush(bitmap1), new ImageBrush(bitmap2), 1, false },
                    new object[] { new ImageBrush(bitmap1) { Opacity = 0.5 }, new ImageBrush(bitmap1), 0.5, true },
                    new object[] { new ImageBrush(bitmap1) { Opacity = 0.25 }, new ImageBrush(bitmap1), 0.5, false },
                    new object[]
                    {
                        new LinearGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        new LinearGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        1, true,
                    },
                    new object[]
                    {
                        new LinearGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        new LinearGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                                new GradientStop(Colors.Blue, 0),
                            }
                        },
                        1, false,
                    },
                    new object[]
                    {
                        new LinearGradientBrush
                        {
                            Opacity = 0.5,
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        new LinearGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        0.5, true,
                    },
                    new object[]
                    {
                        new RadialGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        new RadialGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        1, true,
                    },
                    new object[]
                    {
                        new RadialGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        new RadialGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                                new GradientStop(Colors.Blue, 0),
                            }
                        },
                        1, false,
                    },
                    new object[]
                    {
                        new RadialGradientBrush
                        {
                            Opacity = 0.5,
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        new RadialGradientBrush
                        {
                            GradientStops =
                            {
                                new GradientStop(Colors.Red, 0),
                                new GradientStop(Colors.Green, 0),
                            }
                        },
                        0.5, true,
                    },
                    new object[] { Brushes.Red, Brushes.Red, 1, true },
                    new object[] { Brushes.Red, Brushes.Green, 1, false },
                    new object[] { new SolidColorBrush(Colors.Red, 0.5), Brushes.Red, 0.5, true },
                    new object[] { new SolidColorBrush(Colors.Red, 0.5), Brushes.Green, 0.5, false },
                    new object[] { new SolidColorBrush(Colors.Red, 0.5), Brushes.Red, 0.25, false },
                    new object[] { new VisualBrush(visual1), new VisualBrush(visual1), 1, true },
                    new object[] { new VisualBrush(visual1), new VisualBrush(visual2), 1, false },
                    new object[] { new VisualBrush(visual1) { Opacity = 0.5 }, new VisualBrush(visual1), 0.5, true },
                    new object[] { new VisualBrush(visual1) { Opacity = 0.25 }, new VisualBrush(visual1), 0.5, false },
                };
            }
        }
    }
}
