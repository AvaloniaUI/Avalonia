using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;

using Moq;

using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridSplitterTests
    {
        public GridSplitterTests()
        {
            var cursorFactoryImpl = new Mock<IStandardCursorFactory>();
            AvaloniaLocator.CurrentMutable.Bind<IStandardCursorFactory>().ToConstant(cursorFactoryImpl.Object);
        }

        [Fact]
        public void Detects_Horizontal_Orientation()
        {
            var grid = new Grid()
                       {
                           RowDefinitions = new RowDefinitions("*,Auto,*"),
                           ColumnDefinitions = new ColumnDefinitions("*,*"),
                           Children = new Controls()
                                      {
                                          new Border { [Grid.RowProperty] = 0 },
                                          new GridSplitter { [Grid.RowProperty] = 1, Name = "splitter" },
                                          new Border { [Grid.RowProperty] = 2 }
                                      }
                       };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            Assert.Contains(grid.FindControl<GridSplitter>("splitter").Classes, ":horizontal".Equals);
        }

        [Fact]
        public void Detects_Vertical_Orientation()
        {
            var grid = new Grid()
                       {
                           ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
                           RowDefinitions = new RowDefinitions("*,*"),
                           Children = new Controls()
                                      {
                                          new Border { [Grid.ColumnProperty] = 0 },
                                          new GridSplitter { [Grid.ColumnProperty] = 1, Name = "splitter" },
                                          new Border { [Grid.ColumnProperty] = 2 },
                                      }
                       };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            Assert.Contains(grid.FindControl<GridSplitter>("splitter").Classes, ":vertical".Equals);
        }

        [Fact]
        public void Detects_With_Both_Auto()
        {
            var grid = new Grid()
                       {
                           ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
                           RowDefinitions = new RowDefinitions("Auto,Auto"),
                           Children = new Controls()
                                      {
                                          new Border { [Grid.ColumnProperty] = 0 },
                                          new GridSplitter { [Grid.ColumnProperty] = 1, Name = "splitter" },
                                          new Border { [Grid.ColumnProperty] = 2 },
                                      }
                       };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            Assert.Contains(grid.FindControl<GridSplitter>("splitter").Classes, ":vertical".Equals);
        }

        [Fact]
        public void Horizontal_Stays_Within_Constraints()
        {
            var control1 = new Border { [Grid.RowProperty] = 0 };
            var splitter = new GridSplitter
                           {
                               [Grid.RowProperty] = 1,
                           };
            var control2 = new Border { [Grid.RowProperty] = 2 };

            var rowDefinitions = new RowDefinitions()
                                 {
                                     new RowDefinition(1, GridUnitType.Star) { MinHeight = 70, MaxHeight = 110 },
                                     new RowDefinition(GridLength.Auto),
                                     new RowDefinition(1, GridUnitType.Star) { MinHeight = 10, MaxHeight = 140 },
                                 };

            var grid = new Grid()
                       {
                           RowDefinitions = rowDefinitions,
                           Children = new Controls()
                                      {
                                          control1, splitter, control2
                                      }
                       };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 200));
            root.Arrange(new Rect(0, 0, 100, 200));

            splitter.RaiseEvent(new VectorEventArgs
                                {
                                    RoutedEvent = Thumb.DragDeltaEvent,
                                    Vector = new Vector(0, -100)
                                });
            Assert.Equal(rowDefinitions[0].Height, new GridLength(70, GridUnitType.Star));
            Assert.Equal(rowDefinitions[2].Height, new GridLength(130, GridUnitType.Star));
            splitter.RaiseEvent(new VectorEventArgs
                                {
                                    RoutedEvent = Thumb.DragDeltaEvent,
                                    Vector = new Vector(0, 100)
                                });
            Assert.Equal(rowDefinitions[0].Height, new GridLength(110, GridUnitType.Star));
            Assert.Equal(rowDefinitions[2].Height, new GridLength(90, GridUnitType.Star));
        }

        [Fact]
        public void In_First_Position_Doesnt_Throw_Exception()
        {
            var grid = new Grid()
                       {
                           ColumnDefinitions = new ColumnDefinitions("Auto,*,*"),
                           RowDefinitions = new RowDefinitions("*,*"),
                           Children = new Controls()
                                      {
                                          new GridSplitter { [Grid.ColumnProperty] = 0, Name = "splitter" },
                                          new Border { [Grid.ColumnProperty] = 1 },
                                          new Border { [Grid.ColumnProperty] = 2 },
                                      }
                       };

            var root = new TestRoot { Child = grid };
            root.Measure(new Size(100, 300));
            root.Arrange(new Rect(0, 0, 100, 300));
            var splitter = grid.FindControl<GridSplitter>("splitter");
            splitter.RaiseEvent(new VectorEventArgs
                                {
                                    RoutedEvent = Thumb.DragDeltaEvent,
                                    Vector = new Vector(100, 1000)
                                });
        }

        [Fact]
        public void Vertical_Stays_Within_Constraints()
        {
            var control1 = new Border { [Grid.ColumnProperty] = 0 };
            var splitter = new GridSplitter
                           {
                               [Grid.ColumnProperty] = 1,
                           };
            var control2 = new Border { [Grid.ColumnProperty] = 2 };

            var columnDefinitions = new ColumnDefinitions()
                                    {
                                        new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 10, MaxWidth = 190 },
                                        new ColumnDefinition(GridLength.Auto),
                                        new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 80, MaxWidth = 120 },
                                    };

            var grid = new Grid()
                       {
                           ColumnDefinitions = columnDefinitions,
                           Children = new Controls()
                                      {
                                          control1, splitter, control2
                                      }
                       };

            var root = new TestRoot { Child = grid };

            root.Measure(new Size(200, 100));
            root.Arrange(new Rect(0, 0, 200, 100));

            splitter.RaiseEvent(new VectorEventArgs
                                {
                                    RoutedEvent = Thumb.DragDeltaEvent,
                                    Vector = new Vector(-100, 0)
                                });
            Assert.Equal(columnDefinitions[0].Width, new GridLength(80, GridUnitType.Star));
            Assert.Equal(columnDefinitions[2].Width, new GridLength(120, GridUnitType.Star));
            splitter.RaiseEvent(new VectorEventArgs
                                {
                                    RoutedEvent = Thumb.DragDeltaEvent,
                                    Vector = new Vector(100, 0)
                                });
            Assert.Equal(columnDefinitions[0].Width, new GridLength(120, GridUnitType.Star));
            Assert.Equal(columnDefinitions[2].Width, new GridLength(80, GridUnitType.Star));
        }
    }
}