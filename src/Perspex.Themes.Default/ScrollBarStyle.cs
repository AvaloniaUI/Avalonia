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

    /// <summary>
    /// The default style for the <see cref="ScrollBar"/> control.
    /// </summary>
    public class ScrollBarStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollBarStyle"/> class.
        /// </summary>
        public ScrollBarStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ScrollBar>())
                {
                    Setters = new[]
                    {
                        new Setter(ScrollBar.TemplateProperty, new ControlTemplate<ScrollBar>(Template)),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Horizontal))
                {
                    Setters = new[]
                    {
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

        /// <summary>
        /// The default template for the <see cref="ScrollBar"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ScrollBar control)
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
                        Template = new ControlTemplate<Thumb>(ThumbTemplate),
                    },
                },
            };
        }

        /// <summary>
        /// The default template for the <see cref="ScrollBar"/>'s <see cref="Thumb"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control ThumbTemplate(Thumb control)
        {
            return new Border
            {
                Background = Brushes.Gray,
            };
        }
    }
}
