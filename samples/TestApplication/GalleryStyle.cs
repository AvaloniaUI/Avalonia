using Perspex;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Media;
using Perspex.Styling;
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
                new Style (s => s.Class(":container").OfType<TabControl> ())
                {
                    Setters = new[]
                    {
                        new Setter (TemplatedControl.TemplateProperty, new ControlTemplate<TabControl> (TabControlTemplate))
                    }
                },

                new Style(s => s.Class(":container").OfType<TabControl>().Child().Child().Child().Child().Child().OfType<TabItem>())
                {
                    Setters = new[]
                    {
                        new Setter (TemplatedControl.TemplateProperty, new ControlTemplate<TabItem> (TabItemTemplate)),
                    }
                },

                new Style(s => s.Name("internalStrip").OfType<TabStrip>().Child().OfType<TabItem>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.FontSizeProperty, 14.0),
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
                    }
                },

                new Style(s => s.Name("internalStrip").OfType<TabStrip>().Child().OfType<TabItem>().Class(":selected"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White),
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Colors.White) { Opacity = 0.1 }),
                    },
                },
            });
        }

        public static Control TabItemTemplate(TabItem control)
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
                            VerticalAlignment = Perspex.Layout.VerticalAlignment.Center,
                            Text = x
                        }
                    })
                },
                Name = "headerPresenter",
                [~ContentPresenter.ContentProperty] = control[~HeaderedContentControl.HeaderProperty],
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
                        Background = SolidColorBrush.Parse("#1976D2"),
                        Child = new ScrollViewer
                        {
                            Content = new TabStrip
                            {
                                ItemsPanel = new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Vertical, Gap = 4 }),
                                Margin = new Thickness(0, 10, 0, 0),
                                Name = "internalStrip",
                                [!ItemsControl.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                                [!!SelectingItemsControl.SelectedItemProperty] = control[!!SelectingItemsControl.SelectedItemProperty],
                            }
                        }
                    },
                    new Deck
                    {
                        Name = "deck",
                        MemberSelector = control.ContentSelector,
                        [~Deck.TransitionProperty] = control[~TabControl.TransitionProperty],
                        [!Deck.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                        [!Deck.SelectedItemProperty] = control[!SelectingItemsControl.SelectedItemProperty],
                        [Grid.ColumnProperty] = 1,
                    }
                }
            };
        }
    }
}