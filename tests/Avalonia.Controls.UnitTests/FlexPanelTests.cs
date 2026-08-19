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

        public static TheoryData<FlexDirection, FlexAlignItems> GetAlignItemsValues()
        {
            var data = new TheoryData<FlexDirection, FlexAlignItems>();
            foreach (var direction in Enum.GetValues<FlexDirection>())
            {
                foreach (var alignment in Enum.GetValues<FlexAlignItems>())
                {
                    data.Add(direction, alignment);
                }
            }
            return data;
        }

        public static TheoryData<FlexDirection, FlexJustifyContent> GetJustifyContentValues()
        {
            var data = new TheoryData<FlexDirection, FlexJustifyContent>();
            foreach (var direction in Enum.GetValues<FlexDirection>())
            {
                foreach (var justify in Enum.GetValues<FlexJustifyContent>())
                {
                    data.Add(direction, justify);
                }
            }
            return data;
        }

        [Theory, MemberData(nameof(GetAlignItemsValues))]
        public void Lays_Out_With_Items_Alignment(FlexDirection direction, FlexAlignItems itemsAlignment)
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
                (FlexDirection.Row, FlexAlignItems.FlexStart) => new(0, 0),
                (FlexDirection.Column, FlexAlignItems.FlexStart) => new(0, 0),
                (FlexDirection.Row, FlexAlignItems.Center) => new(0, 75),
                (FlexDirection.Column, FlexAlignItems.Center) => new(75, 0),
                (FlexDirection.Row, FlexAlignItems.FlexEnd) => new(0, 150),
                (FlexDirection.Column, FlexAlignItems.FlexEnd) => new(150, 0),
                (FlexDirection.Row, FlexAlignItems.Stretch) => new(0, 75),
                (FlexDirection.Column, FlexAlignItems.Stretch) => new(75, 0),
                (FlexDirection.RowReverse, FlexAlignItems.FlexStart) => new(100, 0),
                (FlexDirection.ColumnReverse, FlexAlignItems.FlexStart) => new(0, 100),
                (FlexDirection.RowReverse, FlexAlignItems.Center) => new(100, 75),
                (FlexDirection.ColumnReverse, FlexAlignItems.Center) => new(75, 100),
                (FlexDirection.RowReverse, FlexAlignItems.FlexEnd) => new(100, 150),
                (FlexDirection.ColumnReverse, FlexAlignItems.FlexEnd) => new(150, 100),
                (FlexDirection.RowReverse, FlexAlignItems.Stretch) => new(100, 75),
                (FlexDirection.ColumnReverse, FlexAlignItems.Stretch) => new(75, 100),
                _ => throw new NotImplementedException(),
            }, rowBounds.Position);
        }

        [Theory, MemberData(nameof(GetJustifyContentValues))]
        public void Lays_Out_With_Justify_Content(FlexDirection direction, FlexJustifyContent justify)
        {
            var target = new FlexPanel()
            {
                Width = 200,
                Height = 200,
                Direction = direction,
                JustifyContent = justify,
                AlignItems = FlexAlignItems.FlexStart,
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
                (FlexDirection.Row, FlexJustifyContent.FlexStart) => new(0, 0),
                (FlexDirection.Column, FlexJustifyContent.FlexStart) => new(0, 0),
                (FlexDirection.Row, FlexJustifyContent.Center) => new(50, 0),
                (FlexDirection.Column, FlexJustifyContent.Center) => new(0, 50),
                (FlexDirection.Row, FlexJustifyContent.FlexEnd) => new(100, 0),
                (FlexDirection.Column, FlexJustifyContent.FlexEnd) => new(0, 100),
                (FlexDirection.Row, FlexJustifyContent.SpaceAround) => new(25, 0),
                (FlexDirection.Column, FlexJustifyContent.SpaceAround) => new(0, 25),
                (FlexDirection.Row, FlexJustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.Column, FlexJustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.Row, FlexJustifyContent.SpaceEvenly) => new(33, 0),
                (FlexDirection.Column, FlexJustifyContent.SpaceEvenly) => new(0, 33),
                (FlexDirection.RowReverse, FlexJustifyContent.FlexStart) => new(100, 0),
                (FlexDirection.ColumnReverse, FlexJustifyContent.FlexStart) => new(0, 100),
                (FlexDirection.RowReverse, FlexJustifyContent.Center) => new(50, 0),
                (FlexDirection.ColumnReverse, FlexJustifyContent.Center) => new(0, 50),
                (FlexDirection.RowReverse, FlexJustifyContent.FlexEnd) => new(0, 0),
                (FlexDirection.ColumnReverse, FlexJustifyContent.FlexEnd) => new(0, 0),
                (FlexDirection.RowReverse, FlexJustifyContent.SpaceAround) => new(25, 0),
                (FlexDirection.ColumnReverse, FlexJustifyContent.SpaceAround) => new(0, 25),
                (FlexDirection.RowReverse, FlexJustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.ColumnReverse, FlexJustifyContent.SpaceBetween) => new(0, 0),
                (FlexDirection.RowReverse, FlexJustifyContent.SpaceEvenly) => new(33, 0),
                (FlexDirection.ColumnReverse, FlexJustifyContent.SpaceEvenly) => new(0, 33),
                _ => throw new NotImplementedException(),
            }, rowBounds.Position);
        }

        [Fact]
        public void Can_Wrap_Items_Into_Next_Row_With_Spacing()
        {
            var target = new FlexPanel()
            {
                Width = 110,
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

            Assert.Equal(new Size(110, 190), target.Bounds.Size);
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
                Height = 110,
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

            Assert.Equal(new Size(190, 110), target.Bounds.Size);
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
