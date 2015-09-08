// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    /// <summary>
    /// The default style for the <see cref="Button"/> control.
    /// </summary>
    public class ButtonStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonStyle"/> class.
        /// </summary>
        public ButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Button>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(Button.BorderThicknessProperty, 2),
                        new Setter(Button.ForegroundProperty, new SolidColorBrush(0xff000000)),
                        new Setter(Button.FocusAdornerProperty, new FuncTemplate<IControl>(FocusAdornerTemplate)),
                        new Setter(Button.TemplateProperty, new ControlTemplate<Button>(Template)),
                        new Setter(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(Button.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffc4e5f6)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xffff628b)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":disabled").Template().Name("contentPresenter"))
                {
                    Setters = new[]
                    {
                        new Setter(ContentPresenter.OpacityProperty, 0.5),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="Button"/> control's focus adorner.
        /// </summary>
        /// <returns>The root of the instantiated template.</returns>
        public static Control FocusAdornerTemplate()
        {
            return new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new PerspexList<double>(1, 2),
                Margin = new Thickness(3.5),
            };
        }

        /// <summary>
        /// The default template for the <see cref="Button"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(Button control)
        {
            Border border = new Border
            {
                Name = "border",
                Padding = new Thickness(3),
                Child = new ContentPresenter
                {
                    Name = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~Button.ContentProperty],
                    [~TextBlock.ForegroundProperty] = control[~Button.ForegroundProperty],
                    [~ContentPresenter.HorizontalAlignmentProperty] = control[~Button.HorizontalContentAlignmentProperty],
                    [~ContentPresenter.VerticalAlignmentProperty] = control[~Button.VerticalContentAlignmentProperty],
                },
                [~Border.BackgroundProperty] = control[~Button.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~Button.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~Button.BorderThicknessProperty],
            };

            return border;
        }
    }
}
