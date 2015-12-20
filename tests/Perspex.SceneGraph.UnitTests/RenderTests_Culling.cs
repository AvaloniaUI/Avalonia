// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Perspex.Controls;
using Perspex.Media;
using Perspex.Rendering;
using Xunit;

namespace Perspex.SceneGraph.UnitTests
{
    public class RenderTests_Culling
    {
        [Fact]
        public void In_Bounds_Control_Should_Be_Rendered()
        {
            TestControl target;
            var container = new Canvas
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Children = new Controls.Controls
                {
                    (target = new TestControl
                    {
                        Width = 10,
                        Height = 10,
                        [Canvas.LeftProperty] = 98,
                        [Canvas.TopProperty] = 98,
                    })
                }
            };

            Render(container);

            Assert.True(target.Rendered);
        }

        [Fact]
        public void Out_Of_Bounds_Control_Should_Not_Be_Rendered()
        {
            TestControl target;
            var container = new Canvas
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Children = new Controls.Controls
                {
                    (target = new TestControl
                    {
                        Width = 10,
                        Height = 10,
                        [Canvas.LeftProperty] = 110,
                        [Canvas.TopProperty] = 110,
                    })
                }
            };

            Render(container);

            Assert.False(target.Rendered);
        }

        [Fact]
        public void Out_Of_Bounds_Child_Control_Should_Not_Be_Rendered()
        {
            TestControl target;
            var container = new Canvas
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Children = new Controls.Controls
                {
                    new Canvas
                    {
                        Width = 100,
                        Height = 100,
                        [Canvas.LeftProperty] = 50,
                        [Canvas.TopProperty] = 50,
                        Children = new Controls.Controls
                        {
                            (target = new TestControl
                            {
                                Width = 10,
                                Height = 10,
                                [Canvas.LeftProperty] = 50,
                                [Canvas.TopProperty] = 50,
                            })
                        }
                    }
                }
            };

            Render(container);

            Assert.False(target.Rendered);
        }


        [Fact]
        public void Nested_ClipToBounds_Should_Be_Respected()
        {
            TestControl target;
            var container = new Canvas
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Children = new Controls.Controls
                {
                    new Canvas
                    {
                        Width = 50,
                        Height = 50,
                        ClipToBounds = true,
                        Children = new Controls.Controls
                        {
                            (target = new TestControl
                            {
                                Width = 10,
                                Height = 10,
                                [Canvas.LeftProperty] = 50,
                                [Canvas.TopProperty] = 50,
                            })
                        }
                    }
                }
            };

            Render(container);

            Assert.False(target.Rendered);
        }

        [Fact]
        public void RenderTransform_Should_Be_Respected()
        {
            TestControl target;
            var container = new Canvas
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Children = new Controls.Controls
                {
                    (target = new TestControl
                    {
                        Width = 10,
                        Height = 10,
                        [Canvas.LeftProperty] = 110,
                        [Canvas.TopProperty] = 110,
                        RenderTransform = new TranslateTransform(-100, -100),
                    })
                }
            };

            Render(container);

            Assert.True(target.Rendered);
        }

        private void Render(IControl control)
        {
            var ctx = CreateDrawingContext();
            control.Measure(Size.Infinity);
            control.Arrange(new Rect(control.DesiredSize));
            ctx.Render(control);
        }

        private DrawingContext CreateDrawingContext()
        {
            return new DrawingContext(Mock.Of<IDrawingContextImpl>());
        }

        private class TestControl : Control
        {
            public bool Rendered { get; private set; }

            public override void Render(DrawingContext context)
            {
                Rendered = true;
            }
        }
    }
}
