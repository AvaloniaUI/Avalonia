using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class SafeEnumerateCollectionTests
    {
        private class ViewModel1
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private class ViewModel2
        {
            public string CarProducer { get; set; }
            public string CarModel { get; set; }
        }

        [Fact]
        public void NoExceptionWhenDetachingTabControlWithTemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                TabControl tabControl;
                IDataTemplate contentTemplate = new FuncDataTemplate<object>(
                    (i, ns) =>
                    {
                        return
                        new Border()
                        {
                            BorderBrush = Brushes.Red,
                            BorderThickness = new Thickness(4),
                            Padding = new Thickness(3),
                            Child = new ContentControl
                            {
                                Content = i,
                            }
                        };
                    });
                /*IDataTemplate windowTemplate = new FuncDataTemplate<object?>((i, ns) =>
                {

                });*/
                var window = new Window
                {
                    /*DataTemplates =
                    {
                        new FuncDataTemplate<ViewModel1>(
                            (vm1,ns) =>
                            {
                                return new Grid
                                {
                                    ColumnDefinitions = { new(GridLength.Auto), new(1, GridUnitType.Star) },
                                    RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(new GridLength(1, GridUnitType.Star)) },
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = "FirstName",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            [Grid.ColumnProperty] = 0,
                                            [Grid.RowProperty] = 0,
                                        },
                                        new TextBox
                                        {
                                            [TextBox.TextProperty.Bind()] = new Binding(nameof(ViewModel1.FirstName)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                            [Grid.ColumnProperty] = 1,
                                            [Grid.RowProperty] = 0,
                                        },
                                        new TextBlock
                                        {
                                            Text = "LastName",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            [Grid.ColumnProperty] = 0,
                                            [Grid.RowProperty] = 1,
                                        },
                                        new TextBox
                                        {
                                            [TextBox.TextProperty.Bind()] = new Binding(nameof(ViewModel1.LastName)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                            [Grid.ColumnProperty] = 1,
                                            [Grid.RowProperty] = 1,
                                        },
                                    }
                                };
                            }),
                        new FuncDataTemplate<ViewModel2>(
                            (vm2,ns) =>
                            {
                                return new Grid
                                {
                                    ColumnDefinitions = { new(GridLength.Auto), new(1, GridUnitType.Star) },
                                    RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(new GridLength(1, GridUnitType.Star)) },
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = "Car",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Margin = new Thickness(24, 12),
                                            FontWeight = FontWeight.Bold,
                                            FontSize = 24,
                                            [Grid.ColumnProperty] = 0,
                                            [Grid.ColumnSpanProperty] = 2,
                                            [Grid.RowProperty] = 0,
                                        },
                                        new TextBlock
                                        {
                                            Text = "Producer",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            [Grid.ColumnProperty] = 0,
                                            [Grid.RowProperty] = 1,
                                        },
                                        new TextBox
                                        {
                                            [TextBox.TextProperty.Bind()] = new Binding(nameof(ViewModel2.CarProducer)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                            [Grid.ColumnProperty] = 1,
                                            [Grid.RowProperty] = 1,
                                        },
                                        new TextBlock
                                        {
                                            Text = "Model",
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            [Grid.ColumnProperty] = 0,
                                            [Grid.RowProperty] = 2,
                                        },
                                        new TextBox
                                        {
                                            [TextBox.TextProperty.Bind()] = new Binding(nameof(ViewModel2.CarModel)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                            [Grid.ColumnProperty] = 1,
                                            [Grid.RowProperty] = 2,
                                        },
                                    }
                                };
                            }),
                    },*/
                    Content = tabControl = new TabControl
                    {
                        ItemsSource = new object[]
                        {
                            new ViewModel1
                            {
                                FirstName = "Vasily",
                                LastName = "Pupkin",
                            },
                            new ViewModel2
                            {
                                CarProducer = "Fiat",
                                CarModel = "Uno",
                            },
                        },
                        SelectedIndex = 0,
                    },
                    Styles=
                    {
                        new Style(x => x.Is<TabItem>())
                        {
                            Setters =
                            {
                                new Setter{ Property = TabItem.ContentTemplateProperty,
                                Value = contentTemplate,
                                }
                            }
                        }
                    },
                    Width = 640,
                    Height = 480,
                };
                window.Show();
                window.Close();
            }
        }
    }
}
