// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    using Controls = Perspex.Controls.Controls;

    /// <summary>
    /// The default style for the <see cref="RadioButton"/> control.
    /// </summary>
    public class RadioButtonStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RadioButtonStyle"/> class.
        /// </summary>
        public RadioButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<RadioButton>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty,  new ControlTemplate<RadioButton>(Template)),
                    },
                },
                new Style(x => x.OfType<RadioButton>().Template().Name("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, false),
                    },
                },
                new Style(x => x.OfType<RadioButton>().Class(":checked").Template().Name("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, true),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="RadioButton"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(RadioButton control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~RadioButton.BackgroundProperty],
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    },
                    Children = new Controls
                    {
                        new Ellipse
                        {
                            Name = "checkBorder",
                            Stroke = Brushes.Black,
                            StrokeThickness = 2,
                            Width = 18,
                            Height = 18,
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 0,
                        },
                        new Ellipse
                        {
                            Name = "checkMark",
                            Fill = Brushes.Black,
                            Width = 10,
                            Height = 10,
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 0,
                        },
                        new ContentPresenter
                        {
                            Name = "contentPresenter",
                            Margin = new Thickness(4, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            [~ContentPresenter.ContentProperty] = control[~RadioButton.ContentProperty],
                            [Grid.ColumnProperty] = 1,
                        },
                    },
                },
            };

            return result;
        }
    }
}
