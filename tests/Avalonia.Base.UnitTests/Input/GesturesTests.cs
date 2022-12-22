using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class GesturesTests
    {
        private MouseTestHelper _mouse = new MouseTestHelper();
        
        [Fact]
        public void Tapped_Should_Follow_Pointer_Pressed_Released()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(decorator, border, result, false);

            _mouse.Click(border);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
        }

        [Fact]
        public void Tapped_Should_Be_Raised_Even_When_Pressed_Released_Handled()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(decorator, border, result, true);

            _mouse.Click(border);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
        }

        [Fact]
        public void Tapped_Should_Not_Be_Raised_For_Middle_Button()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.TappedEvent, (s, e) => raised = true);

            _mouse.Click(border, MouseButton.Middle);

            Assert.False(raised);
        }

        [Fact]
        public void Tapped_Should_Not_Be_Raised_For_Right_Button()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.TappedEvent, (s, e) => raised = true);

            _mouse.Click(border, MouseButton.Right);

            Assert.False(raised);
        }

        [Fact]
        public void RightTapped_Should_Be_Raised_For_Right_Button()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.RightTappedEvent, (s, e) => raised = true);

            _mouse.Click(border, MouseButton.Right);

            Assert.True(raised);
        }

        [Fact]
        public void DoubleTapped_Should_Follow_Pointer_Pressed_Released_Pressed()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(decorator, border, result, false);

            _mouse.Click(border);
            _mouse.Down(border, clickCount: 2);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
        }

        [Fact]
        public void DoubleTapped_Should_Be_Raised_Even_When_Pressed_Released_Handled()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(decorator, border, result, true);

            _mouse.Click(border);
            _mouse.Down(border, clickCount: 2);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
        }

        [Fact]
        public void DoubleTapped_Should_Not_Be_Raised_For_Middle_Button()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.DoubleTappedEvent, (s, e) => raised = true);

            _mouse.Click(border, MouseButton.Middle);
            _mouse.Down(border, MouseButton.Middle, clickCount: 2);

            Assert.False(raised);
        }

        [Fact]
        public void DoubleTapped_Should_Not_Be_Raised_For_Right_Button()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.DoubleTappedEvent, (s, e) => raised = true);

            _mouse.Click(border, MouseButton.Right);
            _mouse.Down(border, MouseButton.Right, clickCount: 2);

            Assert.False(raised);
        }

        private static void AddHandlers(
            Decorator decorator,
            Border border,
            IList<string> result,
            bool markHandled)
        {
            decorator.AddHandler(Border.PointerPressedEvent, (s, e) =>
            {
                result.Add("dp");

                if (markHandled)
                {
                    e.Handled = true;
                }
            });

            decorator.AddHandler(Border.PointerReleasedEvent, (s, e) =>
            {
                result.Add("dr");

                if (markHandled)
                {
                    e.Handled = true;
                }
            });

            border.AddHandler(Border.PointerPressedEvent, (s, e) => result.Add("bp"));
            border.AddHandler(Border.PointerReleasedEvent, (s, e) => result.Add("br"));

            decorator.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("dt"));
            decorator.AddHandler(Gestures.DoubleTappedEvent, (s, e) => result.Add("ddt"));
            border.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("bt"));
            border.AddHandler(Gestures.DoubleTappedEvent, (s, e) => result.Add("bdt"));
        }

        [Fact]
        public void Pinched_Should_Not_Be_Raised_For_Same_Pointer()
        {
            var touch = new TouchTestHelper();

            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new PinchGestureRecognizer());
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.PinchEvent, (s, e) => raised = true);

            var firstPoint = new Point(5, 5);
            var secondPoint = new Point(10, 10);

            touch.Down(border, position: firstPoint);
            touch.Down(border, position: secondPoint);
            touch.Down(border, position: new Point(20, 20));

            Assert.False(raised);
        }

        [Fact]
        public void Pinched_Should_Be_Raised_For_Two_Pointers_Moving()
        {
            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new PinchGestureRecognizer());
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.PinchEvent, (s, e) => raised = true);

            var firstPoint = new Point(5, 5);
            var secondPoint = new Point(10, 10);

            var firstTouch = new TouchTestHelper();
            var secondTouch = new TouchTestHelper();

            firstTouch.Down(border, position: firstPoint);
            secondTouch.Down(border, position: secondPoint);
            secondTouch.Move(border, position: new Point(20, 20));

            Assert.True(raised);
        }

        [Fact]
        public void Scrolling_Should_Start_After_Start_Distance_Is_Exceded()
        {
            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new ScrollGestureRecognizer()
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                ScrollStartDistance = 50
            });
            var decorator = new Decorator
            {
                Child = border
            };
            var raised = false;

            decorator.AddHandler(Gestures.ScrollGestureEvent, (s, e) => raised = true);

            var firstTouch = new TouchTestHelper();

            firstTouch.Down(border, position: new Point(5, 5));
            firstTouch.Move(border, position: new Point(20, 20));

            Assert.False(raised);

            firstTouch.Move(border, position: new Point(70, 20));

            Assert.True(raised);
        }
    }
}
