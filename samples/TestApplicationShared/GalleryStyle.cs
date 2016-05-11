using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    internal class SampleTabStyle : Styles
    {
        public SampleTabStyle()
        {
            this.AddRange(new[]
            {
                new Style (s => s.Class("container").OfType<TabControl> ())
                {
                    Setters = new[]
                    {
                        new Setter (TemplatedControl.TemplateProperty, new FuncControlTemplate<TabControl>(TabControlTemplate))
                    }
                },

                new Style(s => s.Class("container").OfType<TabControl>().Child().Child().Child().Child().Child().OfType<TabStripItem>())
                {
                    Setters = new[]
                    {
                        new Setter (TemplatedControl.TemplateProperty, new FuncControlTemplate<TabStripItem>(TabStripItemTemplate)),
                    }
                },

                new Style(s => s.Name("PART_TabStrip").OfType<TabStrip>().Child().OfType<TabStripItem>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.FontSizeProperty, 14.0),
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
                    }
                },

                new Style(s => s.Name("PART_TabStrip").OfType<TabStrip>().Child().OfType<TabStripItem>().Class(":selected"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White),
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Colors.White, 0.1)),
                    },
                },
            });
        }

        public static Control TabStripItemTemplate(TabStripItem control)
        {
            return new ContentPresenter
            {
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<string>(x => new Border
                    {
                        [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                        Padding = new Thickness(10),
                        Child = new TextBlock
                        {
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            Text = x
                        }
                    })
                },
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
            };
        }

        public static Control TabControlTemplate(TabControl control)
        {
            return new Grid
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
                        Width = 190,
                        Background = Brush.Parse("#1976D2"),
                        Child = new ScrollViewer
                        {
                            Content = new TabStrip
                            {
                                Name = "PART_TabStrip",
                                ItemsPanel = new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Vertical, Gap = 4 }),
                                Margin = new Thickness(0, 10, 0, 0),
                                MemberSelector = TabControl.HeaderSelector,
                                [!ItemsControl.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                                [!!SelectingItemsControl.SelectedItemProperty] = control[!!SelectingItemsControl.SelectedItemProperty],
                            }
                        }
                    },
                    new Carousel
                    {
                        Name = "PART_Content",
                        MemberSelector = TabControl.ContentSelector,
                        [~Carousel.TransitionProperty] = control[~TabControl.TransitionProperty],
                        [!Carousel.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                        [!Carousel.SelectedItemProperty] = control[!SelectingItemsControl.SelectedItemProperty],
                        [Grid.ColumnProperty] = 1,
                    }
                }
            };
        }
    }
}