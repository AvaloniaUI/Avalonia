// -----------------------------------------------------------------------
// <copyright file="DropDownStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    public class DropDownStyle : Styles
    {
        public DropDownStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<DropDown>())
                {
                    Setters = new[]
                    {
                        new Setter(DropDown.TemplateProperty, ControlTemplate.Create<DropDown>(this.Template)),
                        new Setter(DropDown.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(DropDown.BorderThicknessProperty, 2.0),
                        new Setter(DropDown.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(DropDown.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
            });
        }

        private Control Template(DropDown control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~DropDown.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~DropDown.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~DropDown.BorderThicknessProperty],
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        new ContentPresenter
                        {
                            Id = "contentPresenter",
                            Margin = new Thickness(3),
                            [~ContentPresenter.ContentProperty] = control[~DropDown.ContentProperty],
                            [~ContentPresenter.HorizontalAlignmentProperty] = control[~DropDown.HorizontalContentAlignmentProperty],
                            [~ContentPresenter.VerticalAlignmentProperty] = control[~DropDown.VerticalContentAlignmentProperty],
                        },
                        new ToggleButton
                        {
                            Id = "toggle",
                            BorderThickness = 0,
                            Background = Brushes.Transparent,
                            Content = new Path
                            {
                                Id = "checkMark",
                                Fill = Brushes.Black,
                                Width = 8,
                                Height = 4,
                                Stretch = Stretch.Uniform,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Data = StreamGeometry.Parse("F1 M 301.14,-189.041L 311.57,-189.041L 306.355,-182.942L 301.14,-189.041 Z"),
                                [Grid.ColumnProperty] = 0,
                            },
                            [~~ToggleButton.IsCheckedProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [Grid.ColumnProperty] = 1,
                        },
                        new Popup
                        {
                            Child = new ItemsControl
                            {
                                [~ListBox.ItemsProperty] = control[~DropDown.ItemsProperty],
                            },
                            PlacementTarget = control,
                            [~Popup.IsOpenProperty] = control[~DropDown.IsDropDownOpenProperty],
                        }
                    },
                },
            };

            return result;
        }
    }
}
