// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class ScrollBarTests
    {
        [Fact]
        public void Setting_Value_Should_Update_Track_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");
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
            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");
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

            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");
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

            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");

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

                    track.Thumb.RaiseEvent(ev);
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

            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");

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

                    track.Thumb.RaiseEvent(ev);
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
    }
}
