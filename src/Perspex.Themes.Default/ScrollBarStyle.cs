// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
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
            AddRange(new[]
            {
                new Style(x => x.OfType<ScrollBar>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ScrollBar>(Template)),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Horizontal))
                {
                    Setters = new[]
                    {
                        new Setter(Layoutable.HeightProperty, 10.0),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Horizontal).Template().Name("thumb"))
                {
                    Setters = new[]
                    {
                        new Setter(Layoutable.MinWidthProperty, 10.0),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Vertical))
                {
                    Setters = new[]
                    {
                        new Setter(Layoutable.WidthProperty, 10.0),
                    },
                },
                new Style(x => x.OfType<ScrollBar>().PropertyEquals(ScrollBar.OrientationProperty, Orientation.Vertical).Template().Name("thumb"))
                {
                    Setters = new[]
                    {
                        new Setter(Layoutable.MinHeightProperty, 10.0),
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
                    [!Track.MinimumProperty] = control[!RangeBase.MinimumProperty],
                    [!Track.MaximumProperty] = control[!RangeBase.MaximumProperty],
                    [!!Track.ValueProperty] = control[!!RangeBase.ValueProperty],
                    [!Track.ViewportSizeProperty] = control[!ScrollBar.ViewportSizeProperty],
                    [!Track.OrientationProperty] = control[!ScrollBar.OrientationProperty],
                    Thumb = new Thumb
                    {
                        Name = "thumb",
                        Template = new FuncControlTemplate<Thumb>(ThumbTemplate),
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
