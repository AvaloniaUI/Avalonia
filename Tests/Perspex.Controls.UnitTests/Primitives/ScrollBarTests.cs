// -----------------------------------------------------------------------
// <copyright file="ScrollBarTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Primitives
{
    using System;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Media;
    using Xunit;

    public class ScrollBarTests
    {
        [Fact]
        public void Setting_Value_Should_Update_Track_Value()
        {
            var target = new ScrollBar
            {
                Template = new ControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = target.GetTemplateChild<Track>("track");
            target.Value = 50;

            Assert.Equal(track.Value, 50);
        }

        [Fact]
        public void Setting_Track_Value_Should_Update_Value()
        {
            var target = new ScrollBar
            {
                Template = new ControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = target.GetTemplateChild<Track>("track");
            track.Value = 50;

            Assert.Equal(target.Value, 50);
        }

        [Fact]
        public void Setting_Track_Value_After_Setting_Value_Should_Update_Value()
        {
            var target = new ScrollBar
            {
                Template = new ControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = target.GetTemplateChild<Track>("track");
            target.Value = 25;
            track.Value = 50;

            Assert.Equal(target.Value, 50);
        }

        [Fact]
        public void ScrollBar_Can_AutoHide()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Auto;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = 100;

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

        private static Control Template(ScrollBar control)
        {
            return new Border
            {
                Child = new Track
                {
                    Name = "track",
                    [!Track.MinimumProperty] = control[!ScrollBar.MinimumProperty],
                    [!Track.MaximumProperty] = control[!ScrollBar.MaximumProperty],
                    [!!Track.ValueProperty] = control[!!ScrollBar.ValueProperty],
                    [!Track.ViewportSizeProperty] = control[!ScrollBar.ViewportSizeProperty],
                    [!Track.OrientationProperty] = control[!ScrollBar.OrientationProperty],
                    Thumb = new Thumb
                    {
                        Template = new ControlTemplate<Thumb>(ThumbTemplate),
                    },
                },
            };
        }

        private static Control ThumbTemplate(Thumb control)
        {
            return new Border
            {
                Background = Brushes.Gray,
            };
        }
    }
}