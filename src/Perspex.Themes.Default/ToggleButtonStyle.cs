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
    /// The default style for the <see cref="ToggleButton"/> control.
    /// </summary>
    public class ToggleButtonStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleButtonStyle"/> class.
        /// </summary>
        public ToggleButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ToggleButton>())
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.TemplateProperty, new ControlTemplate<ToggleButton>(Template)),
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(ToggleButton.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(ToggleButton.BorderThicknessProperty, 2.0),
                        new Setter(ToggleButton.FocusAdornerProperty, new FuncTemplate<IControl>(ButtonStyle.FocusAdornerTemplate)),
                        new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(0xff000000)),
                        new Setter(ToggleButton.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(ToggleButton.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":checked").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xff7f7f7f)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":pointerover").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                        new Setter(ToggleButton.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":checked").Class(":pointerover").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffa0a0a0)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":pointerover").Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffc4e5f6)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BorderBrushProperty, new SolidColorBrush(0xffff628b)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":disabled").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(0xff7f7f7f)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="ToggleButton"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ToggleButton control)
        {
            Border border = new Border
            {
                Name = "border",
                Padding = new Thickness(3),
                [~Border.BackgroundProperty] = control[~ToggleButton.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~ToggleButton.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~ToggleButton.BorderThicknessProperty],
                Child = new ContentPresenter
                {
                    Name = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~ToggleButton.ContentProperty],
                    [~ContentPresenter.HorizontalAlignmentProperty] = control[~ToggleButton.HorizontalContentAlignmentProperty],
                    [~ContentPresenter.VerticalAlignmentProperty] = control[~ToggleButton.VerticalContentAlignmentProperty],
                },
            };

            return border;
        }
    }
}
