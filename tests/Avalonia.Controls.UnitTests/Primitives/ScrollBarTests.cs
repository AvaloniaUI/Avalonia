using System;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class ScrollBarTests : ScopedTestBase
    {
        [Fact]
        public void Setting_Value_Should_Update_Track_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = (Track)target.GetTemplateDescendants().First(x => x.Name == "track");
            target.Value = 50;

            Assert.Equal(50, track.Value);
        }

        [Fact]
        public void Setting_Track_Value_Should_Update_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = (Track)target.GetTemplateDescendants().First(x => x.Name == "track");
            track.Value = 50;

            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Setting_Track_Value_After_Setting_Value_Should_Update_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = (Track)target.GetTemplateDescendants().First(x => x.Name == "track");
            target.Value = 25;
            track.Value = 50;

            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Thumb_DragDelta_Event_Should_Raise_Scroll_Event()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = (Track)target.GetTemplateDescendants().First(x => x.Name == "track");

            var raisedEvent = Assert.Raises<ScrollEventArgs>(
                handler => target.Scroll += handler,
                handler => target.Scroll -= handler,
                () =>
                {
                    var ev = new VectorEventArgs
                    {
                        RoutedEvent = Thumb.DragDeltaEvent,
                        Vector = new Vector(0, 0)
                    };

                    track.Thumb!.RaiseEvent(ev);
                });

            Assert.Equal(ScrollEventType.ThumbTrack, raisedEvent.Arguments.ScrollEventType);
        }

        [Fact]
        public void Thumb_DragComplete_Event_Should_Raise_Scroll_Event()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = (Track)target.GetTemplateDescendants().First(x => x.Name == "track");

            var raisedEvent = Assert.Raises<ScrollEventArgs>(
                handler => target.Scroll += handler,
                handler => target.Scroll -= handler,
                () =>
                {
                    var ev = new VectorEventArgs
                    {
                        RoutedEvent = Thumb.DragCompletedEvent,
                        Vector = new Vector(0, 0)
                    };

                    track.Thumb!.RaiseEvent(ev);
                });

            Assert.Equal(ScrollEventType.EndScroll, raisedEvent.Arguments.ScrollEventType);
        }

        [Fact]
        public void ScrollBar_Can_AutoHide()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Auto;
            target.ViewportSize = 1;
            target.Maximum = 0;

            Assert.False(target.IsVisible);
        }

        [Fact]
        public void ScrollBar_Should_Not_AutoHide_When_ViewportSize_Is_NaN()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Auto;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = double.NaN;

            Assert.True(target.IsVisible);
        }

        [Fact]
        public void ScrollBar_Should_Not_AutoHide_When_Visibility_Set_To_Visible()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Visible;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = 100;

            Assert.True(target.IsVisible);
        }

        [Fact]
        public void ScrollBar_Should_Hide_When_Visibility_Set_To_Hidden()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Hidden;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = 10;

            Assert.False(target.IsVisible);
        }

        private static Control Template(ScrollBar control, INameScope scope)
        {
            return new Border
            {
                Child = new Track
                {
                    Name = "track",
                    [!Track.MinimumProperty] = control[!RangeBase.MinimumProperty],
                    [!Track.MaximumProperty] = control[!RangeBase.MaximumProperty],
                    [!!Track.ValueProperty] = control[!!RangeBase.ValueProperty],
                    [!Track.ViewportSizeProperty] = control[!ScrollBar.ViewportSizeProperty],
                    [!Track.OrientationProperty] = control[!ScrollBar.OrientationProperty],
                    Thumb = new Thumb
                    {
                        Template = new FuncControlTemplate<Thumb>(ThumbTemplate),
                    },
                }.RegisterInNameScope(scope),
            };
        }

        private static Control ThumbTemplate(Thumb control, INameScope scope)
        {
            return new Border
            {
                Background = Brushes.Gray,
            };
        }

        [Theory]
        [InlineData(Orientation.Vertical)]
        [InlineData(Orientation.Horizontal)]
        public void Orientation_Should_Set_Matching_PseudoClass(Orientation orientation)
        {
            var target = new ScrollBar { Orientation = orientation };

            Assert.Equal(orientation == Orientation.Vertical, target.Classes.Contains(":vertical"));
            Assert.Equal(orientation == Orientation.Horizontal, target.Classes.Contains(":horizontal"));
        }

        [Fact]
        public void ScrollToHome_Should_Set_Value_To_Minimum()
        {
            var target = CreateScrollBar(minimum: 10, maximum: 100, value: 50);

            target.ScrollToHome();

            Assert.Equal(10, target.Value);
        }

        [Fact]
        public void ScrollToEnd_Should_Set_Value_To_Maximum()
        {
            var target = CreateScrollBar(minimum: 0, maximum: 90, value: 50);

            target.ScrollToEnd();

            Assert.Equal(90, target.Value);
        }

        [Fact]
        public void PageUp_Should_Decrease_Value_By_LargeChange()
        {
            var target = CreateScrollBar(value: 50, largeChange: 10);

            target.PageUp();

            Assert.Equal(40, target.Value);
        }

        [Fact]
        public void PageDown_Should_Increase_Value_By_LargeChange()
        {
            var target = CreateScrollBar(value: 50, largeChange: 10);

            target.PageDown();

            Assert.Equal(60, target.Value);
        }

        [Fact]
        public void PageLeft_Should_Decrease_Value_By_LargeChange()
        {
            var target = CreateScrollBar(orientation: Orientation.Horizontal, value: 50, largeChange: 10);

            target.PageLeft();

            Assert.Equal(40, target.Value);
        }

        [Fact]
        public void PageRight_Should_Increase_Value_By_LargeChange()
        {
            var target = CreateScrollBar(orientation: Orientation.Horizontal, value: 50, largeChange: 10);

            target.PageRight();

            Assert.Equal(60, target.Value);
        }

        [Fact]
        public void LineUp_Should_Decrease_Value_By_SmallChange()
        {
            var target = CreateScrollBar(value: 50, smallChange: 5);

            target.LineUp();

            Assert.Equal(45, target.Value);
        }

        [Fact]
        public void LineDown_Should_Increase_Value_By_SmallChange()
        {
            var target = CreateScrollBar(value: 50, smallChange: 5);

            target.LineDown();

            Assert.Equal(55, target.Value);
        }

        [Fact]
        public void LineLeft_Should_Decrease_Value_By_SmallChange()
        {
            var target = CreateScrollBar(orientation: Orientation.Horizontal, value: 50, smallChange: 5);

            target.LineLeft();

            Assert.Equal(45, target.Value);
        }

        [Fact]
        public void LineRight_Should_Increase_Value_By_SmallChange()
        {
            var target = CreateScrollBar(orientation: Orientation.Horizontal, value: 50, smallChange: 5);

            target.LineRight();

            Assert.Equal(55, target.Value);
        }

        [Fact]
        public void Scroll_Methods_Should_Respect_Value_Bounds()
        {
            var target = CreateScrollBar(minimum: 20, maximum: 80, value: 25, smallChange: 10);

            target.LineUp();
            Assert.Equal(20, target.Value); // Should not go below Minimum

            target.Value = 75;
            target.LineDown();
            Assert.Equal(80, target.Value); // Should not go above Maximum
        }

        [Fact]
        public void Scroll_Methods_Should_Raise_Scroll_Event()
        {
            var target = CreateScrollBar(value: 50, smallChange: 5, largeChange: 10);
            var events = new System.Collections.Generic.List<ScrollEventType>();
            target.Scroll += (_, e) => events.Add(e.ScrollEventType);

            target.LineDown();
            target.PageDown();
            target.ScrollToHome();
            target.ScrollToEnd();

            Assert.Equal(
                new[]
                {
                    ScrollEventType.SmallIncrement,
                    ScrollEventType.LargeIncrement,
                    ScrollEventType.LargeDecrement,
                    ScrollEventType.LargeIncrement,
                },
                events);
        }

        [Fact]
        public void ScrollHere_Should_Set_Value_Within_Bounds()
        {
            var target = CreateScrollBar(minimum: 0, maximum: 100, value: 0);

            // No exception even though no pointer position was recorded yet.
            target.ScrollHere();

            Assert.InRange(target.Value, target.Minimum, target.Maximum);
        }

        [Theory]
        [InlineData(PointerType.Touch)]
        [InlineData(PointerType.Pen)]
        public void ContextRequested_Should_Be_Handled_For_Touch_Or_Pen_Input(PointerType pointerType)
        {
            var target = CreateScrollBar();

            var args = CreateContextRequested(target, pointerType);
            target.RaiseEvent(args);

            Assert.True(args.Handled);
        }

        [Fact]
        public void ContextRequested_Should_Not_Be_Handled_For_Mouse_Input()
        {
            var target = CreateScrollBar();

            var args = CreateContextRequested(target, PointerType.Mouse);
            target.RaiseEvent(args);

            Assert.False(args.Handled);
        }

        private static ContextRequestedEventArgs CreateContextRequested(ScrollBar target, PointerType pointerType)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), pointerType, true);
            var pointerArgs = new PointerPressedEventArgs(
                target,
                pointer,
                target,
                default,
                timestamp: 1,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
                KeyModifiers.None);

            return new ContextRequestedEventArgs(pointerArgs);
        }

        private static ScrollBar CreateScrollBar(
            Orientation orientation = Orientation.Vertical,
            double minimum = 0,
            double maximum = 100,
            double value = 0,
            double smallChange = 1,
            double largeChange = 10)
        {
            var target = new ScrollBar
            {
                Orientation = orientation,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                SmallChange = smallChange,
                LargeChange = largeChange,
            };

            target.ApplyTemplate();
            return target;
        }
    }
}
