// -----------------------------------------------------------------------
// <copyright file="CheckBoxStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Shapes;
    using Perspex.Controls.Templates;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="CheckBox"/> control.
    /// </summary>
    public class CheckBoxStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckBoxStyle"/> class.
        /// </summary>
        public CheckBoxStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<CheckBox>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, new ControlTemplate<CheckBox>(Template)),
                    },
                },
                new Style(x => x.OfType<CheckBox>().Template().Name("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, false),
                    },
                },
                new Style(x => x.OfType<CheckBox>().Class(":checked").Template().Name("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, true),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for a <see cref="CheckBox"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(CheckBox control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~CheckBox.BackgroundProperty],
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    },
                    Children = new Controls
                    {
                        new Border
                        {
                            Name = "checkBorder",
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Width = 18,
                            Height = 18,
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 0,
                        },
                        new Path
                        {
                            Name = "checkMark",
                            Fill = Brushes.Black,
                            Width = 11,
                            Height = 10,
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Data = StreamGeometry.Parse("M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z"),
                            [Grid.ColumnProperty] = 0,
                        },
                        new ContentPresenter
                        {
                            Name = "contentPresenter",
                            Margin = new Thickness(4, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            [~ContentPresenter.ContentProperty] = control[~CheckBox.ContentProperty],
                            [Grid.ColumnProperty] = 1,
                        },
                    },
                },
            };

            return result;
        }
    }
}
