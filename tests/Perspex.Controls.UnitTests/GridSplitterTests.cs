using Moq;

using Perspex.Controls.Primitives;
using Perspex.Input;
using Perspex.Platform;

using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class GridSplitterTests
    {
        [Fact]
        public void Vertical_Stays_Within_Constrains()
        {
            var cursorFactoryImpl = new Mock<IStandardCursorFactory>();
            PerspexLocator.CurrentMutable.Bind<IStandardCursorFactory>().ToConstant(cursorFactoryImpl.Object);

            var control1 = new Border { [Grid.ColumnProperty] = 0 };
            var splitter = new GridSplitter { [Grid.ColumnProperty] = 1};
            var control2 = new Border { [Grid.ColumnProperty] = 2 };

            var columnDefenitions = new ColumnDefinitions()
                                    {
                                        new ColumnDefinition(1, GridUnitType.Star) {MinWidth = 10, MaxWidth = 190},
                                        new ColumnDefinition(GridLength.Auto),
                                        new ColumnDefinition(1, GridUnitType.Star) {MinWidth = 80, MaxWidth =  120},
                                    };
            var grid = new Grid()
                         {
                             ColumnDefinitions = columnDefenitions,
                             Children = new Controls()
                                        {
                                            control1, splitter, control2
                                        }
                         };

            var root = new TestRoot { Child = grid };
            Assert.Equal(splitter.Orientation, Orientation.Vertical);

            root.Measure(new Size(200, 100));
            root.Arrange(new Rect(0, 0, 200, 100));

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(-100,0)
            });
            Assert.Equal(columnDefenitions[0].Width, new GridLength(80,GridUnitType.Star));
            Assert.Equal(columnDefenitions[2].Width, new GridLength(120,GridUnitType.Star));
            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(100, 0)
            });
            Assert.Equal(columnDefenitions[0].Width, new GridLength(120, GridUnitType.Star));
            Assert.Equal(columnDefenitions[2].Width, new GridLength(80, GridUnitType.Star));
        }

        [Fact]
        public void Horizontal_Stays_Within_Constrains()
        {
            var cursorFactoryImpl = new Mock<IStandardCursorFactory>();
            PerspexLocator.CurrentMutable.Bind<IStandardCursorFactory>().ToConstant(cursorFactoryImpl.Object);

            var control1 = new Border { [Grid.RowProperty] = 0 };
            var splitter = new GridSplitter { [Grid.RowProperty] = 1 };
            var control2 = new Border { [Grid.RowProperty] = 2 };

            var rowDefenitions = new RowDefinitions()
                                    {
                                        new RowDefinition(1, GridUnitType.Star) {MinHeight = 70, MaxHeight = 110},
                                        new RowDefinition(GridLength.Auto),
                                        new RowDefinition(1, GridUnitType.Star) { MinHeight = 10, MaxHeight =  140},
                                    };
            var grid = new Grid()
            {
                RowDefinitions = rowDefenitions,
                Children = new Controls()
                                        {
                                            control1, splitter, control2
                                        }
            };

            var root = new TestRoot { Child = grid };
            Assert.Equal(splitter.Orientation, Orientation.Horizontal);
            root.Measure(new Size(100, 200));
            root.Arrange(new Rect(0, 0, 100, 200));

            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(0, -100)
            });
            Assert.Equal(rowDefenitions[0].Height, new GridLength(70, GridUnitType.Star));
            Assert.Equal(rowDefenitions[2].Height, new GridLength(130, GridUnitType.Star));
            splitter.RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = Thumb.DragDeltaEvent,
                Vector = new Vector(0, 100)
            });
            Assert.Equal(rowDefenitions[0].Height, new GridLength(110, GridUnitType.Star));
            Assert.Equal(rowDefenitions[2].Height, new GridLength(90, GridUnitType.Star));
        }
    }
}