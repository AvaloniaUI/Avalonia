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
                Template = ControlTemplate.Create<ScrollBar>(Template),
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
                Template = ControlTemplate.Create<ScrollBar>(Template),
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
                Template = ControlTemplate.Create<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = target.GetTemplateChild<Track>("track");
            target.Value = 25;
            track.Value = 50;

            Assert.Equal(target.Value, 50);
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
                        Template = ControlTemplate.Create<Thumb>(ThumbTemplate),
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