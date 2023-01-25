using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.VisualTree
{
    public class VisualExtensions_GetVisualsAt
    {
        [Fact]
        public void Should_Find_Control()
        {
            Border target;
            using var services = new CompositorTestServices(new Size(200, 200))
            {
                TopLevel =
                {
                    Content = new StackPanel
                    {
                        Background = null,
                        Children =
                        {
                            (target = new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Red,
                            }),
                            new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Green,
                            }
                        },
                        Orientation = Orientation.Horizontal,
                    }
                }
            };

            services.RunJobs();
            var result = target.GetVisualsAt(new Point(50, 50));

            Assert.Same(target, result.Single());

        }

        [Fact]
        public void Should_Not_Find_Sibling_Control()
        {
            Border target;
            using var services = new CompositorTestServices(new Size(200, 200))
            {
                TopLevel =
                {
                    Content = new StackPanel
                    {
                        Background = Brushes.White,
                        Children =
                        {
                            (target = new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Red,
                            }),
                            new Border
                            {
                                Width = 100,
                                Height = 200,
                                Background = Brushes.Green,
                            }
                        },
                        Orientation = Orientation.Horizontal,
                    }
                }
            };
            services.RunJobs();
            var result = target.GetVisualsAt(new Point(150, 50));

            Assert.Empty(result);
        }
    }
}
