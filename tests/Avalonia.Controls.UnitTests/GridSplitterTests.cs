#nullable enable

using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridSplitterTests : ScopedTestBase
    {
        public GridSplitterTests()
        {
            var cursorFactoryImpl = new Mock<ICursorFactory>();
            AvaloniaLocator.CurrentMutable.Bind<ICursorFactory>().ToConstant(cursorFactoryImpl.Object);
        }

        [Fact]
        public void Detects_Horizontal_Orientation()
        {
            GridSplitter splitter;

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children =
                {
                    new Border { [Grid.RowProperty] = 0 },
                    (splitter = new GridSplitter { [Grid.RowProperty] = 1 }),
                    new Border { [Grid.RowProperty] = 2 }
                }
            };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            Assert.Equal(GridResizeDirection.Rows, splitter.GetEffectiveResizeDirection());
        }

        [Fact]
        public void Detects_Vertical_Orientation()
        {
            GridSplitter splitter;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
                RowDefinitions = new RowDefinitions("*,*"),
                Children =
                {
                    new Border { [Grid.ColumnProperty] = 0 },
                    (splitter = new GridSplitter { [Grid.ColumnProperty] = 1 }),
                    new Border { [Grid.ColumnProperty] = 2 },
                }
            };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            Assert.Equal(GridResizeDirection.Columns, splitter.GetEffectiveResizeDirection());
        }

        [Fact]
        public void Detects_With_Both_Auto()
        {
            GridSplitter splitter;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                Children =
                {
                    new Border { [Grid.ColumnProperty] = 0 },
                    (splitter = new GridSplitter { [Grid.ColumnProperty] = 1 }),
                    new Border { [Grid.ColumnProperty] = 2 },
                }
            };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            Assert.Equal(GridResizeDirection.Columns, splitter.GetEffectiveResizeDirection());
        }

        [Fact]
        public void In_First_Position_Doesnt_Throw_Exception()
        {
            GridSplitter splitter;
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,*"),
                RowDefinitions = new RowDefinitions("*,*"),
                Children =
                {
                    (splitter = new GridSplitter { [Grid.ColumnProperty] = 0 }),
                    new Border { [Grid.ColumnProperty] = 1 },
                    new Border { [Grid.ColumnProperty] = 2 },
                }
            };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));

            splitter.RaiseEvent(
                new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(100, 1000)
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Horizontal_Stays_Within_Constraints(bool showsPreview)
        {
            var control1 = new Border { [Grid.RowProperty] = 0 };
            var splitter = new GridSplitter { [Grid.RowProperty] = 1, ShowsPreview = showsPreview};
            var control2 = new Border { [Grid.RowProperty] = 2 };

            var rowDefinitions = new RowDefinitions
            {
                new RowDefinition(1, GridUnitType.Star) { MinHeight = 70, MaxHeight = 110 },
                new RowDefinition(GridLength.Auto),
                new RowDefinition(1, GridUnitType.Star) { MinHeight = 10, MaxHeight = 140 },
            };

            var grid = new Grid { RowDefinitions = rowDefinitions, Children = { control1, splitter, control2 } };

            var root = new TestRoot
            {
                Child = new VisualLayerManager
                {
                    Child = grid
                }
            };

            root.Measure(new Size(100, 200));
            root.Arrange(new Rect(0, 0, 100, 200));

            splitter.RaiseEvent(
                new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(0, -100)
            });

            if (showsPreview)
            {
                Assert.Equal(rowDefinitions[0].Height, new GridLength(1, GridUnitType.Star));
                Assert.Equal(rowDefinitions[2].Height, new GridLength(1, GridUnitType.Star));
            }
            else
            {
                Assert.Equal(rowDefinitions[0].Height, new GridLength(70, GridUnitType.Star));
                Assert.Equal(rowDefinitions[2].Height, new GridLength(130, GridUnitType.Star));
            }

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(0, 100)
            });

            if (showsPreview)
            {
                Assert.Equal(rowDefinitions[0].Height, new GridLength(1, GridUnitType.Star));
                Assert.Equal(rowDefinitions[2].Height, new GridLength(1, GridUnitType.Star));
            }
            else
            {
                Assert.Equal(rowDefinitions[0].Height, new GridLength(110, GridUnitType.Star));
                Assert.Equal(rowDefinitions[2].Height, new GridLength(90, GridUnitType.Star));
            }

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragCompletedEvent
            });

            Assert.Equal(rowDefinitions[0].Height, new GridLength(110, GridUnitType.Star));
            Assert.Equal(rowDefinitions[2].Height, new GridLength(90, GridUnitType.Star));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Vertical_Stays_Within_Constraints(bool showsPreview)
        {
            var control1 = new Border { [Grid.ColumnProperty] = 0 };
            var splitter = new GridSplitter { [Grid.ColumnProperty] = 1, ShowsPreview = showsPreview};
            var control2 = new Border { [Grid.ColumnProperty] = 2 };

            var columnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 10, MaxWidth = 190 },
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 80, MaxWidth = 120 },
            };

            var grid = new Grid { ColumnDefinitions = columnDefinitions, Children = { control1, splitter, control2 } };

            var root = new TestRoot
            {
                Child = new VisualLayerManager
                {
                    Child = grid
                }
            };

            root.Measure(new Size(200, 100));
            root.Arrange(new Rect(0, 0, 200, 100));

            splitter.RaiseEvent(
                new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(-100, 0)
            });

            if (showsPreview)
            {
                Assert.Equal(columnDefinitions[0].Width, new GridLength(1, GridUnitType.Star));
                Assert.Equal(columnDefinitions[2].Width, new GridLength(1, GridUnitType.Star));
            }
            else
            {
                Assert.Equal(columnDefinitions[0].Width, new GridLength(80, GridUnitType.Star));
                Assert.Equal(columnDefinitions[2].Width, new GridLength(120, GridUnitType.Star));
            }

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(100, 0)
            });

            if (showsPreview)
            {
                Assert.Equal(columnDefinitions[0].Width, new GridLength(1, GridUnitType.Star));
                Assert.Equal(columnDefinitions[2].Width, new GridLength(1, GridUnitType.Star));
            }
            else
            {
                Assert.Equal(columnDefinitions[0].Width, new GridLength(120, GridUnitType.Star));
                Assert.Equal(columnDefinitions[2].Width, new GridLength(80, GridUnitType.Star));
            }

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragCompletedEvent
            });

            Assert.Equal(columnDefinitions[0].Width, new GridLength(120, GridUnitType.Star));
            Assert.Equal(columnDefinitions[2].Width, new GridLength(80, GridUnitType.Star));
        }

        [Theory]
        [InlineData(Key.Up, 90, 110)]
        [InlineData(Key.Down, 110, 90)]
        public void Vertical_Keyboard_Input_Can_Move_Splitter(Key key, double expectedHeightFirst, double expectedHeightSecond)
        {
            var control1 = new Border { [Grid.RowProperty] = 0 };
            var splitter = new GridSplitter { [Grid.RowProperty] = 1, KeyboardIncrement = 10d };
            var control2 = new Border { [Grid.RowProperty] = 2 };

            var rowDefinitions = new RowDefinitions
            {
                new RowDefinition(1, GridUnitType.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(1, GridUnitType.Star)
            };

            var grid = new Grid { RowDefinitions = rowDefinitions, Children = { control1, splitter, control2 } };

            var root = new TestRoot
            {
                Child = grid
            };

            root.Measure(new Size(200, 200));
            root.Arrange(new Rect(0, 0, 200, 200));

            splitter.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = key
            });

            Assert.Equal(rowDefinitions[0].Height, new GridLength(expectedHeightFirst, GridUnitType.Star));
            Assert.Equal(rowDefinitions[2].Height, new GridLength(expectedHeightSecond, GridUnitType.Star));
        }

        [Theory]
        [InlineData(Key.Left, 90, 110)]
        [InlineData(Key.Right, 110, 90)]
        public void Horizontal_Keyboard_Input_Can_Move_Splitter(Key key, double expectedWidthFirst, double expectedWidthSecond)
        {
            var control1 = new Border { [Grid.ColumnProperty] = 0 };
            var splitter = new GridSplitter { [Grid.ColumnProperty] = 1, KeyboardIncrement = 10d };
            var control2 = new Border { [Grid.ColumnProperty] = 2 };

            var columnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(1, GridUnitType.Star)
            };

            var grid = new Grid { ColumnDefinitions = columnDefinitions, Children = { control1, splitter, control2 } };

            var root = new TestRoot
            {
                Child = grid
            };

            root.Measure(new Size(200, 200));
            root.Arrange(new Rect(0, 0, 200, 200));

            splitter.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = key
            });

            Assert.Equal(columnDefinitions[0].Width, new GridLength(expectedWidthFirst, GridUnitType.Star));
            Assert.Equal(columnDefinitions[2].Width, new GridLength(expectedWidthSecond, GridUnitType.Star));
        }

        [Fact]
        public void Pressing_Escape_Key_Cancels_Resizing()
        {
            var control1 = new Border { [Grid.ColumnProperty] = 0 };
            var splitter = new GridSplitter { [Grid.ColumnProperty] = 1, KeyboardIncrement = 10d };
            var control2 = new Border { [Grid.ColumnProperty] = 2 };

            var columnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(1, GridUnitType.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(1, GridUnitType.Star)
            };

            var grid = new Grid { ColumnDefinitions = columnDefinitions, Children = { control1, splitter, control2 } };

            var root = new TestRoot
            {
                Child = grid
            };

            root.Measure(new Size(200, 200));
            root.Arrange(new Rect(0, 0, 200, 200));

            splitter.RaiseEvent(
                new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(-100, 0)
            });

            Assert.Equal(columnDefinitions[0].Width, new GridLength(0, GridUnitType.Star));
            Assert.Equal(columnDefinitions[2].Width, new GridLength(200, GridUnitType.Star));

            splitter.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Escape
            });

            Assert.Equal(columnDefinitions[0].Width, new GridLength(1, GridUnitType.Star));
            Assert.Equal(columnDefinitions[2].Width, new GridLength(1, GridUnitType.Star));
        }
  
        [Fact]
        public void Works_In_ItemsControl_ItemsSource()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"<ItemsControl xmlns='https://github.com/avaloniaui'
                                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                  xmlns:local='clr-namespace:Avalonia.Controls.UnitTests'>
    <ItemsControl.Resources>
      <ControlTheme x:Key='{x:Type ItemsControl}' TargetType='ItemsControl'>
        <Setter Property='Template'>
          <ControlTemplate>
            <Border Background='{TemplateBinding Background}'
                    BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='{TemplateBinding BorderThickness}'
                    CornerRadius='{TemplateBinding CornerRadius}'
                    Padding='{TemplateBinding Padding}'>
              <ItemsPresenter Name='PART_ItemsPresenter'
                              ItemsPanel='{TemplateBinding ItemsPanel}'/>
            </Border>
          </ControlTemplate>
        </Setter>
      </ControlTheme>
    </ItemsControl.Resources>
    <ItemsControl.Styles>
        <Style Selector='ItemsControl > ContentPresenter'>
            <Setter Property='(Grid.Column)' Value='{Binding Column}'/>
        </Style>
    </ItemsControl.Styles>
    <ItemsControl.DataTemplates>
        <DataTemplate DataType='local:TextItem'>
            <Border><TextBlock Text='{Binding Text}'/></Border>
        </DataTemplate>
        <DataTemplate DataType='local:SplitterItem'>
            <GridSplitter ResizeDirection='Columns'/>
        </DataTemplate>
    </ItemsControl.DataTemplates>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Grid ColumnDefinitions='*,10,*'/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>";

            var itemsControl = AvaloniaRuntimeXamlLoader.Parse<ItemsControl>(xaml);
            itemsControl.ItemsSource = new List<IGridItem>
            {
                new TextItem { Column = 0, Text = "A" },
                new SplitterItem { Column = 1 },
                new TextItem { Column = 2, Text = "B" },
            };

            var root = new TestRoot { Child = itemsControl };
            root.Measure(new Size(200, 100));
            root.Arrange(new Rect(0, 0, 200, 100));

            var panel = Assert.IsType<Grid>(itemsControl.ItemsPanelRoot);
            var cp = Assert.IsType<ContentPresenter>(panel.Children[1]);
            cp.UpdateChild();
            var splitter = Assert.IsType<GridSplitter>(cp.Child);

            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(-20, 0) });
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

            Assert.NotEqual(panel.ColumnDefinitions[0].Width, panel.ColumnDefinitions[2].Width);
        }

        [Fact]
        public void Works_In_ItemsControl_Items()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"<ItemsControl xmlns='https://github.com/avaloniaui'
                                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ItemsControl.Resources>
      <ControlTheme x:Key='{x:Type ItemsControl}' TargetType='ItemsControl'>
        <Setter Property='Template'>
          <ControlTemplate>
            <Border Background='{TemplateBinding Background}'
                    BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='{TemplateBinding BorderThickness}'
                    CornerRadius='{TemplateBinding CornerRadius}'
                    Padding='{TemplateBinding Padding}'>
              <ItemsPresenter Name='PART_ItemsPresenter'
                              ItemsPanel='{TemplateBinding ItemsPanel}'/>
            </Border>
          </ControlTemplate>
        </Setter>
      </ControlTheme>
    </ItemsControl.Resources>
    <ItemsControl.Items>
        <Border Grid.Column='0'/>
        <GridSplitter Grid.Column='1' ResizeDirection='Columns'/>
        <Border Grid.Column='2'/>
    </ItemsControl.Items>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Grid ColumnDefinitions='*,10,*'/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>";

            var itemsControl = AvaloniaRuntimeXamlLoader.Parse<ItemsControl>(xaml);
            var root = new TestRoot { Child = itemsControl };
            root.Measure(new Size(200, 100));
            root.Arrange(new Rect(0, 0, 200, 100));

            var panel = Assert.IsType<Grid>(itemsControl.ItemsPanelRoot);
            var splitter = Assert.IsType<GridSplitter>(panel.Children[1]);

            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(-20, 0) });
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

            Assert.NotEqual(panel.ColumnDefinitions[0].Width, panel.ColumnDefinitions[2].Width);
        }

        [Fact]
        public void Works_In_Grid()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var xaml = @"<Grid xmlns='https://github.com/avaloniaui' ColumnDefinitions='*,10,*'>
    <Border Grid.Column='0'/>
    <GridSplitter Grid.Column='1' ResizeDirection='Columns'/>
    <Border Grid.Column='2'/>
</Grid>";

            var grid = AvaloniaRuntimeXamlLoader.Parse<Grid>(xaml);
            var root = new TestRoot { Child = grid };
            root.Measure(new Size(200, 100));
            root.Arrange(new Rect(0, 0, 200, 100));

            var splitter = Assert.IsType<GridSplitter>(grid.Children[1]);

            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(-20, 0) });
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

            Assert.NotEqual(grid.ColumnDefinitions[0].Width, grid.ColumnDefinitions[2].Width);
        }
    }

    public interface IGridItem
    {
        int Column { get; set; }
    }

    public class TextItem : IGridItem
    {
        public int Column { get; set; }
        public string? Text { get; set; }
    }

    public class SplitterItem : IGridItem
    {
        public int Column { get; set; }
    }
}
