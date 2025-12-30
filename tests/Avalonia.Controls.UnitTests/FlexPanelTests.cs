using System;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class FlexPanelTests : ScopedTestBase
    {
        [Fact]
        public void Lays_Items_In_A_Single_Row()
        {
            var target = new FlexPanel()
            {
                Width = 200,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 50), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Items_In_A_Single_Column()
        {
            var target = new FlexPanel()
            {
                Direction = FlexDirection.Column,
                Height = 120,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Row()
        {
            var target = new FlexPanel()
            {
                Width = 100,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                },
                Wrap = FlexWrap.Wrap
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Row_In_Reverse_Wrap()
        {
            var target = new FlexPanel()
            {
                Width = 100,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                },
                Wrap = FlexWrap.WrapReverse
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Column()
        {
            var target = new FlexPanel()
            {
                Height = 60,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                },
                Wrap = FlexWrap.Wrap,
                Direction = FlexDirection.Column
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 60), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Column_In_Reverse_Wrap()
        {
            var target = new FlexPanel()
            {
                Height = 60,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                },
                Wrap = FlexWrap.WrapReverse,
                Direction = FlexDirection.Column
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 60), target.Bounds.Size);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[1].Bounds);
        }

        public static TheoryData<FlexDirection, AlignItems> GetAlignItemsValues()
        {
            var data = new TheoryData<FlexDirection, AlignItems>();
            foreach (var direction in Enum.GetValues<FlexDirection>())
            {
                foreach (var alignment in Enum.GetValues<AlignItems>())
                {
                    data.Add(direction, alignment);
                }
            }
            return data;
        }

        public static TheoryData<FlexDirection, JustifyContent> GetJustifyContentValues()
        {
            var data = new TheoryData<FlexDirection, JustifyContent>();
            foreach (var direction in Enum.GetValues<FlexDirection>())
            {
                foreach (var justify in Enum.GetValues<JustifyContent>())
                {
                    data.Add(direction, justify);
                }
            }
            return data;
        }

        [Theory, MemberData(nameof(GetAlignItemsValues))]
        public void Lays_Out_With_Items_Alignment(FlexDirection direction, AlignItems itemsAlignment)
        {
            var target = new FlexPanel()
            {
                Width = 200,
                Height = 200,
                Direction = direction,
                AlignItems = itemsAlignment,
                Children =
                {
                    new Border { Height = 50, Width = 50 },
                    new Border { Height = 50, Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.Bounds.Size);

            var rowBounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);

            Assert.Equal(direction switch
            {
                FlexDirection.Row => new(100, 50),
                FlexDirection.RowReverse => new(100, 50),
                FlexDirection.Column => new(50, 100),
                FlexDirection.ColumnReverse => new(50, 100),
                _ => throw new NotImplementedException()
            }, rowBounds.Size);

            Assert.Equal((direction, itemsAlignment) switch
            {
                (FlexDirection.Row, AlignItems.FlexStart) => new(0, 0),
                (FlexDirection.Column, AlignItems.FlexStart) => new(0, 0),
                (FlexDirection.Row, AlignItems.Center) => new(0, 75),
                (FlexDirection.Column, AlignItems.Center) => new(75, 0),
                (FlexDirection.Row, AlignItems.FlexEnd) => new(0, 150),
                (FlexDirection.Column, AlignItems.FlexEnd) => new(150, 0),
                (FlexDirection.Row, AlignItems.Stretch) => new(0, 75),
                (FlexDirection.Column, AlignItems.Stretch) => new(75, 0),
                (FlexDirection.RowReverse, AlignItems.FlexStart) => new(100, 0),
                (FlexDirection.ColumnReverse, AlignItems.FlexStart) => new(0, 100),
                (FlexDirection.RowReverse, AlignItems.Center) => new(100, 75),
                (FlexDirection.ColumnReverse, AlignItems.Center) => new(75, 100),
                (FlexDirection.RowReverse, AlignItems.FlexEnd) => new(100, 150),
                (FlexDirection.ColumnReverse, AlignItems.FlexEnd) => new(150, 100),
                (FlexDirection.RowReverse, AlignItems.Stretch) => new(100, 75),
                (FlexDirection.ColumnReverse, AlignItems.Stretch) => new(75, 100),
                _ => throw new NotImplementedException(),
            }, rowBounds.Position);
        }

        [Theory, MemberData(nameof(GetJustifyContentValues))]
        public void Lays_Out_With_Justify_Content(FlexDirection direction, JustifyContent justify)
        {
            var target = new FlexPanel()
            {
                Width = 200,
                Height = 200,
                Direction = direction,
                JustifyContent = justify,
                AlignItems = AlignItems.FlexStart,
                Children =
                {
                    new Border { Height = 50, Width = 50 },
                    new Border { Height = 50, Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.Bounds.Size);

            var rowBounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);

            Assert.Equal((direction, justify) switch
            {
                (FlexDirection.Row, JustifyContent.FlexStart) => new(0, 0),
                (FlexDirection.Column, JustifyContent.FlexStart) => new(0, 0),
                (FlexDirection.Row, JustifyContent.Center) => new(50, 0),
                (FlexDirection.Column, JustifyContent.Center) => new(0, 50),
                (FlexDirection.Row, JustifyContent.FlexEnd) => new(100, 0),
                (FlexDirection.Column, JustifyContent.FlexEnd) => new(0, 100),
                (FlexDirection.Row, JustifyContent.SpaceAround) => new(25, 0),
                (FlexDirection.Column, JustifyContent.SpaceAround) => new(0, 25),
                (FlexDirection.Row, JustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.Column, JustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.Row, JustifyContent.SpaceEvenly) => new(33, 0),
                (FlexDirection.Column, JustifyContent.SpaceEvenly) => new(0, 33),
                (FlexDirection.RowReverse, JustifyContent.FlexStart) => new(100, 0),
                (FlexDirection.ColumnReverse, JustifyContent.FlexStart) => new(0, 100),
                (FlexDirection.RowReverse, JustifyContent.Center) => new(50, 0),
                (FlexDirection.ColumnReverse, JustifyContent.Center) => new(0, 50),
                (FlexDirection.RowReverse, JustifyContent.FlexEnd) => new(0, 0),
                (FlexDirection.ColumnReverse, JustifyContent.FlexEnd) => new(0, 0),
                (FlexDirection.RowReverse, JustifyContent.SpaceAround) => new(25, 0),
                (FlexDirection.ColumnReverse, JustifyContent.SpaceAround) => new(0, 25),
                (FlexDirection.RowReverse, JustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.ColumnReverse, JustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.RowReverse, JustifyContent.SpaceEvenly) => new(33, 0),
                (FlexDirection.ColumnReverse, JustifyContent.SpaceEvenly) => new(0, 33),
                _ => throw new NotImplementedException(),
            }, rowBounds.Position);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Row_With_Spacing()
        {
            var target = new FlexPanel()
            {
                Width = 100,
                ColumnSpacing = 10,
                RowSpacing = 20,
                Children =
                {
                    new Border { Height = 50, Width = 60 }, // line 0
                    new Border { Height = 50, Width = 30 }, // line 0
                    new Border { Height = 50, Width = 70 }, // line 1
                    new Border { Height = 50, Width = 30 }, // line 2
                },
                Wrap = FlexWrap.Wrap
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 190), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(70, 0, 30, 50), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 70, 50), target.Children[2].Bounds);
            Assert.Equal(new Rect(0, 140, 30, 50), target.Children[3].Bounds);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Row_With_Spacing_And_Invisible_Content()
        {
            var target = new FlexPanel()
            {
                ColumnSpacing = 10,
                Children =
                {
                    new Border { Height = 50, Width = 60 }, // line 0
                    new Border { Height = 50, Width = 30 , IsVisible = false }, // line 0
                    new Border { Height = 50, Width = 50 }, // line 0
                },
                Wrap = FlexWrap.Wrap
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 50), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Column_With_Spacing()
        {
            var target = new FlexPanel()
            {
                Height = 100,
                RowSpacing = 10,
                ColumnSpacing = 20,
                Children =
                {
                    new Border { Width = 50, Height = 60 }, // line 0
                    new Border { Width = 50, Height = 30 }, // line 0
                    new Border { Width = 50, Height = 70 }, // line 1
                    new Border { Width = 50, Height = 30 }, // line 2
                },
                Wrap = FlexWrap.Wrap,
                Direction = FlexDirection.Column
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(190, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 50, 60), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 70, 50, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 70), target.Children[2].Bounds);
            Assert.Equal(new Rect(140, 0, 50, 30), target.Children[3].Bounds);
        }

        [Fact]
        public void Applies_Absolute_FlexBasis_Properties()
        {
            var target = new FlexPanel()
            {
                Width = 50,
                Children =
                {
                    new Border()
                    {
                        [Flex.BasisProperty] = new FlexBasis(20),
                        Height = 15
                    },
                    new Border()
                    {
                        [Flex.BasisProperty] = new FlexBasis(20),
                        Height = 15
                    }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(50, 15), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 15), target.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 20, 15), target.Children[1].Bounds);
        }

        [Fact]
        public void Applies_Relative_FlexBasis_Properties()
        {
            var target = new FlexPanel()
            {
                Width = 50,
                Children =
                {
                    new Border()
                    {
                        [Flex.BasisProperty] = new FlexBasis(50, FlexBasisKind.Relative),
                        Height = 15
                    },
                    new Border()
                    {
                        [Flex.BasisProperty] = new FlexBasis(50, FlexBasisKind.Relative),
                        Height = 15
                    }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(50, 15), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 25, 15), target.Children[0].Bounds);
            Assert.Equal(new Rect(25, 0, 25, 15), target.Children[1].Bounds);
        }
    }
}
