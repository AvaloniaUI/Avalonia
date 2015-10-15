// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
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
            AddRange(new[]
            {
                new Style(x => x.OfType<Button>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xffaaaaaa)),
                        new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(0xffaaaaaa)),
                        new Setter(TemplatedControl.BorderThicknessProperty, 2),
                        new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(0xff000000)),
                        new Setter(Control.FocusAdornerProperty, new FuncTemplate<IControl>(FocusAdornerTemplate)),
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>(Template)),
                        new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(0xff888888)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(0xff888888)),
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xff888888)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":disabled").Template().Name("contentPresenter"))
                {
                    Setters = new[]
                    {
                        new Setter(Visual.OpacityProperty, 0.5),
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
                    [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                    [~TextBlock.ForegroundProperty] = control[~TemplatedControl.ForegroundProperty],
                    [~Layoutable.HorizontalAlignmentProperty] = control[~ContentControl.HorizontalContentAlignmentProperty],
                    [~Layoutable.VerticalAlignmentProperty] = control[~ContentControl.VerticalContentAlignmentProperty],
                },
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],
            };

            return border;
        }
    }
}
