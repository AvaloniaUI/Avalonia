// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Layout;
using Xunit;

namespace Perspex.Input.UnitTests
{
    public class InputElement_HitTesting
    {
        [Fact]
        public void InputHitTest_Should_Find_Control_At_Point()
        {
            var container = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            container.Measure(Size.Infinity);
            container.Arrange(new Rect(container.DesiredSize));

            var result = container.InputHitTest(new Point(100, 100));

            Assert.Equal(container.Child, result);
        }

        [Fact]
        public void InputHitTest_Should_Not_Find_Control_Outside_Point()
        {
            var container = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            container.Measure(Size.Infinity);
            container.Arrange(new Rect(container.DesiredSize));

            var result = container.InputHitTest(new Point(10, 10));

            Assert.Equal(container, result);
        }

        [Fact]
        public void InputHitTest_Should_Find_Top_Control_At_Point()
        {
            var container = new Panel
            {
                Width = 200,
                Height = 200,
                Children = new Controls.Controls
                {
                    new Border
                    {
                        Width = 100,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Border
                    {
                        Width = 50,
                        Height = 50,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            container.Measure(Size.Infinity);
            container.Arrange(new Rect(container.DesiredSize));

            var result = container.InputHitTest(new Point(100, 100));

            Assert.Equal(container.Children[1], result);
        }

        [Fact]
        public void InputHitTest_Should_Find_Top_Control_At_Point_With_ZOrder()
        {
            var container = new Panel
            {
                Width = 200,
                Height = 200,
                Children = new Controls.Controls
                {
                    new Border
                    {
                        Width = 100,
                        Height = 100,
                        ZIndex = 1,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Border
                    {
                        Width = 50,
                        Height = 50,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            container.Measure(Size.Infinity);
            container.Arrange(new Rect(container.DesiredSize));

            var result = container.InputHitTest(new Point(100, 100));

            Assert.Equal(container.Children[0], result);
        }
    }
}
