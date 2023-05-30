using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
    }
}
