// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
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
    using Collections;
    using Controls = Controls.Controls;

    /// <summary>
    /// The default style for the <see cref="TreeViewItem"/> control.
    /// </summary>
    public class TreeViewItemStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewItemStyle"/> class.
        /// </summary>
        public TreeViewItemStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<TreeViewItem>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<TreeViewItem>(Template)),
                        new Setter(Control.FocusAdornerProperty, null),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Template().Name("header"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.PaddingProperty, new Thickness(2)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Class(":selected").Template().Name("header"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xfff0f0f0)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Class(":selected").Class(":focus").Template().Name("header"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xff086f9e)),
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Template().OfType<ToggleButton>().Class("expander"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<ToggleButton>(ToggleButtonTemplate)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Template().OfType<ToggleButton>().Class("expander").Class(":checked").Template().OfType<Path>())
                {
                    Setters = new[]
                    {
                        new Setter(Visual.RenderTransformProperty, new RotateTransform(45)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Class(":empty").Template().OfType<ToggleButton>().Class("expander"))
                {
                    Setters = new[]
                    {
                        new Setter(Visual.IsVisibleProperty, false),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="TreeViewItem"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TreeViewItem control)
        {
            return new StackPanel
            {
                Children = new Controls
                {
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions
                        {
                            new ColumnDefinition(new GridLength(16, GridUnitType.Pixel)),
                            new ColumnDefinition(GridLength.Auto),
                        },
                        Children = new Controls
                        {
                            new ToggleButton
                            {
                                Classes = new Classes("expander"),
                                Focusable = false,
                                [~~ToggleButton.IsCheckedProperty] = control[~TreeViewItem.IsExpandedProperty],
                            },
                            new Border
                            {
                                Name = "header",
                                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                                [Grid.ColumnProperty] = 1,
                                Child = new ContentPresenter
                                {
                                    [~ContentPresenter.ContentProperty] = control[~HeaderedItemsControl.HeaderProperty],
                                },
                            },
                            new Rectangle
                            {
                                Name = "focus",
                                Stroke = Brushes.Black,
                                StrokeThickness = 1,
                                StrokeDashArray = new PerspexList<double>(1, 2),
                                [Grid.ColumnProperty] = 1,
                                [!Rectangle.IsVisibleProperty] = control[!TreeViewItem.IsFocusedProperty],
                            }
                        }
                    },
                    new ItemsPresenter
                    {
                        Name = "itemsPresenter",
                        Margin = new Thickness(24, 0, 0, 0),
                        [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                        [~Visual.IsVisibleProperty] = control[~TreeViewItem.IsExpandedProperty],
                    }
                }
            };
        }

        private Control ToggleButtonTemplate(ToggleButton control)
        {
            return new Border
            {
                Width = 12,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new Path
                {
                    Fill = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z"),
                }
            };
        }
    }
}
