// -----------------------------------------------------------------------
// <copyright file="ScrollBarStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Media;
    using Perspex.Styling;

    public class ScrollBarStyle : Styles
    {
        public ScrollBarStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ScrollBar>())
                {
                    Setters = new[]
                    {
                        new Setter(ScrollBar.TemplateProperty, new ControlTemplate<ScrollBar>(this.Template)),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Horizontal))
                {
                    Setters = new[]
                    {
                        new Setter(ScrollBar.TemplateProperty, new ControlTemplate<ScrollBar>(this.Template)),
                        new Setter(ScrollBar.HeightProperty, 10.0),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Horizontal).Template().Name("thumb"))
                {
                    Setters = new[]
                    {
                        new Setter(Thumb.MinWidthProperty, 10.0),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Vertical))
                {
                    Setters = new[]
                    {
                        new Setter(ScrollBar.TemplateProperty, new ControlTemplate<ScrollBar>(this.Template)),
                        new Setter(ScrollBar.WidthProperty, 10.0),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Vertical).Template().Name("thumb"))
                {
                    Setters = new[]
                    {
                        new Setter(Thumb.MinHeightProperty, 10.0),
                    },
                },
            });
        }

        private Control Template(ScrollBar control)
        {
            return new Border
            {
                Background = Brushes.Silver,
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
                        Name = "thumb",
                        Template = new ControlTemplate<Thumb>(this.ThumbTemplate),
                    },
                },
            };
        }

        private Control ThumbTemplate(Thumb control)
        {
            return new Border
            {
                Background = Brushes.Gray,
            };
        }
    }
}
