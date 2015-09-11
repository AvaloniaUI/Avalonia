// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    /// <summary>
    /// The default style for the <see cref="TextBox"/> control.
    /// </summary>
    public class TextBoxStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextBoxStyle"/> class.
        /// </summary>
        public TextBoxStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<TextBox>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<TextBox>(Template)),
                        new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(TemplatedControl.BorderThicknessProperty, 2.0),
                        new Setter(Control.FocusAdornerProperty, null),
                    },
                },
                new Style(x => x.OfType<TextBox>().Class(":focus").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BorderBrushProperty, Brushes.Black),
                    },
                }
            });
        }

        /// <summary>
        /// The default template for the <see cref="TextBox"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TextBox control)
        {
            Border result = new Border
            {
                Name = "border",
                Padding = new Thickness(2),
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],
                Child = new ScrollViewer
                {
                    [~ScrollViewer.CanScrollHorizontallyProperty] = control[~ScrollViewer.CanScrollHorizontallyProperty],
                    [~ScrollViewer.HorizontalScrollBarVisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                    [~ScrollViewer.VerticalScrollBarVisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                    Content = new TextPresenter
                    {
                        Name = "textPresenter",
                        [~TextPresenter.CaretIndexProperty] = control[~TextBox.CaretIndexProperty],
                        [~TextPresenter.SelectionStartProperty] = control[~TextBox.SelectionStartProperty],
                        [~TextPresenter.SelectionEndProperty] = control[~TextBox.SelectionEndProperty],
                        [~TextBlock.TextProperty] = control[~TextBox.TextProperty],
                        [~TextBlock.TextWrappingProperty] = control[~TextBox.TextWrappingProperty],
                    }
                }
            };

            return result;
        }
    }
}
