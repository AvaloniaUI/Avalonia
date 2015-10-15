// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
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
    using Controls = Controls.Controls;

    /// <summary>
    /// The default style for the <see cref="DropDown"/> control.
    /// </summary>
    public class DropDownStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DropDownStyle"/> class.
        /// </summary>
        public DropDownStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<DropDown>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DropDown>(Template)),
                        new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(TemplatedControl.BorderThicknessProperty, 2.0),
                        new Setter(Control.FocusAdornerProperty, new FuncTemplate<IControl>(FocusAdornerTemplate)),
                        new Setter(DropDown.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(DropDown.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<DropDown>().Descendent().OfType<ListBoxItem>().Class(":pointerover"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xffbee6fd))
                    }
                }
            });
        }

        /// <summary>
        /// The default template for a <see cref="DropDown"/> control's focus adorner.
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
        /// The default template for a <see cref="DropDown"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(DropDown control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],
                Child = new Grid
                {
                    Name = "container",
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        new ContentControl
                        {
                            Name = "contentControl",
                            Margin = new Thickness(3),
                            [~ContentControl.ContentProperty] = control[~DropDown.SelectionBoxItemProperty],
                            [~Layoutable.HorizontalAlignmentProperty] = control[~DropDown.HorizontalContentAlignmentProperty],
                            [~Layoutable.VerticalAlignmentProperty] = control[~DropDown.VerticalContentAlignmentProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                            BorderThickness = 0,
                            Background = Brushes.Transparent,
                            ClickMode = ClickMode.Press,
                            Focusable = false,
                            Content = new Path
                            {
                                Name = "checkMark",
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
                            Name = "popup",
                            Child = new Border
                            {
                                BorderBrush = Brushes.Black,
                                BorderThickness = 1,
                                Padding = new Thickness(4),
                                Child = new ItemsPresenter
                                {
                                    MemberSelector = control.MemberSelector,
                                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                                }
                            },
                            PlacementTarget = control,
                            StaysOpen = false,
                            [~~Popup.IsOpenProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [~Layoutable.MinWidthProperty] = control[~Visual.BoundsProperty].Cast<Rect>().Select(x => (object)x.Width),
                        }
                    },
                },
            };

            return result;
        }
    }
}
