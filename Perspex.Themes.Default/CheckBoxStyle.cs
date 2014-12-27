// -----------------------------------------------------------------------
// <copyright file="CheckBoxStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Controls.Shapes;
    using Perspex.Styling;
    using Perspex.Controls.Presenters;

    public class CheckBoxStyle : Styles
    {
        public CheckBoxStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<CheckBox>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<CheckBox>(this.Template)),
                    },
                },
                new Style(x => x.OfType<CheckBox>().Template().Id("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, false),
                    },
                },
                new Style(x => x.OfType<CheckBox>().Class(":checked").Template().Id("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, true),
                    },
                },
            });
        }

        private Control Template(CheckBox control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~CheckBox.BackgroundProperty],
                Content = new Grid
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
                            Id = "checkBorder",
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Width = 18,
                            Height = 18,
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 0,
                        },
                        new Path
                        {
                            Id = "checkMark",
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
                            Id = "contentPresenter",
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
