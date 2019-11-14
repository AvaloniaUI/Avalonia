using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class VisualExtensionsTests
    {
        [Fact]
        public void FindCommonVisualAncestor_Two_Subtrees_Uniform_Height()
        {
            Control left, right;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = left = new Decorator()
                            }
                        },
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = right = new Decorator()
                            }
                        }
                    }
                }
            };

            var ancestor = left.FindCommonVisualAncestor(right);
            Assert.Equal(root.Child, ancestor);

            ancestor = right.FindCommonVisualAncestor(left);
            Assert.Equal(root.Child, ancestor);
        }

        [Fact]
        public void FindCommonVisualAncestor_Two_Subtrees_NonUniform_Height()
        {
            Control left, right;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = left = new Decorator()
                            }
                        },
                        new Decorator
                        {
                            Child = new Decorator
                            {
                                Child = new Decorator
                                {
                                    Child = right = new Decorator()
                                }
                            }
                        }
                    }
                }
            };

            var ancestor = left.FindCommonVisualAncestor(right);
            Assert.Equal(root.Child, ancestor);

            ancestor = right.FindCommonVisualAncestor(left);
            Assert.Equal(root.Child, ancestor);
        }

        [Fact]
        public void TranslatePoint_Should_Respect_RenderTransforms()
        {
            Border target;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Decorator
                {
                    Width = 50,
                    Height = 50,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = new TranslateTransform(25, 25),
                    Child = target = new Border(),
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var result = target.TranslatePoint(new Point(0, 0), root);

            Assert.Equal(new Point(50, 50), result);
        }
    }
}
