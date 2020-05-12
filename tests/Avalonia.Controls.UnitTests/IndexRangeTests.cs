using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class IndexRangeTests
    {
        [Fact]
        public void Add_Should_Add_Range_To_Empty_List()
        {
            var ranges = new List<IndexRange>();
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(0, 4), selected);

            Assert.Equal(5, result);
            Assert.Equal(new[] { new IndexRange(0, 4) }, ranges);
            Assert.Equal(new[] { new IndexRange(0, 4) }, selected);
        }

        [Fact]
        public void Add_Should_Add_Non_Intersecting_Range_At_End()
        {
            var ranges = new List<IndexRange> { new IndexRange(0, 4) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(8, 10), selected);

            Assert.Equal(3, result);
            Assert.Equal(new[] { new IndexRange(0, 4), new IndexRange(8, 10) }, ranges);
            Assert.Equal(new[] { new IndexRange(8, 10) }, selected);
        }

        [Fact]
        public void Add_Should_Add_Non_Intersecting_Range_At_Beginning()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(0, 4), selected);

            Assert.Equal(5, result);
            Assert.Equal(new[] { new IndexRange(0, 4), new IndexRange(8, 10) }, ranges);
            Assert.Equal(new[] { new IndexRange(0, 4) }, selected);
        }

        [Fact]
        public void Add_Should_Add_Non_Intersecting_Range_In_Middle()
        {
            var ranges = new List<IndexRange> { new IndexRange(0, 4), new IndexRange(14, 16) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(8, 10), selected);

            Assert.Equal(3, result);
            Assert.Equal(new[] { new IndexRange(0, 4), new IndexRange(8, 10), new IndexRange(14, 16) }, ranges);
            Assert.Equal(new[] { new IndexRange(8, 10) }, selected);
        }

        [Fact]
        public void Add_Should_Add_Intersecting_Range_Start()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(6, 9), selected);

            Assert.Equal(2, result);
            Assert.Equal(new[] { new IndexRange(6, 10) }, ranges);
            Assert.Equal(new[] { new IndexRange(6, 7) }, selected);
        }

        [Fact]
        public void Add_Should_Add_Intersecting_Range_End()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(9, 12), selected);

            Assert.Equal(2, result);
            Assert.Equal(new[] { new IndexRange(8, 12) }, ranges);
            Assert.Equal(new[] { new IndexRange(11, 12) }, selected);
        }

        [Fact]
        public void Add_Should_Add_Intersecting_Range_Both()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(6, 12), selected);

            Assert.Equal(4, result);
            Assert.Equal(new[] { new IndexRange(6, 12) }, ranges);
            Assert.Equal(new[] { new IndexRange(6, 7), new IndexRange(11, 12) }, selected);
        }

        [Fact]
        public void Add_Should_Join_Two_Intersecting_Ranges()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10), new IndexRange(12, 14) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(8, 14), selected);

            Assert.Equal(1, result);
            Assert.Equal(new[] { new IndexRange(8, 14) }, ranges);
            Assert.Equal(new[] { new IndexRange(11, 11) }, selected);
        }

        [Fact]
        public void Add_Should_Join_Two_Intersecting_Ranges_And_Add_Ranges()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10), new IndexRange(12, 14) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(6, 18), selected);

            Assert.Equal(7, result);
            Assert.Equal(new[] { new IndexRange(6, 18) }, ranges);
            Assert.Equal(new[] { new IndexRange(6, 7), new IndexRange(11, 11), new IndexRange(15, 18) }, selected);
        }

        [Fact]
        public void Add_Should_Not_Add_Already_Selected_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var selected = new List<IndexRange>();
            var result = IndexRange.Add(ranges, new IndexRange(9, 10), selected);

            Assert.Equal(0, result);
            Assert.Equal(new[] { new IndexRange(8, 10) }, ranges);
            Assert.Empty(selected);
        }

        [Fact]
        public void Remove_Should_Remove_Entire_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(8, 10), deselected);

            Assert.Equal(3, result);
            Assert.Empty(ranges);
            Assert.Equal(new[] { new IndexRange(8, 10) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Start_Of_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 12) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(8, 10), deselected);

            Assert.Equal(3, result);
            Assert.Equal(new[] { new IndexRange(11, 12) }, ranges);
            Assert.Equal(new[] { new IndexRange(8, 10) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_End_Of_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 12) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(10, 12), deselected);

            Assert.Equal(3, result);
            Assert.Equal(new[] { new IndexRange(8, 9) }, ranges);
            Assert.Equal(new[] { new IndexRange(10, 12) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Overlapping_End_Of_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 12) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(10, 14), deselected);

            Assert.Equal(3, result);
            Assert.Equal(new[] { new IndexRange(8, 9) }, ranges);
            Assert.Equal(new[] { new IndexRange(10, 12) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Middle_Of_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(10, 20) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(12, 16), deselected);

            Assert.Equal(5, result);
            Assert.Equal(new[] { new IndexRange(10, 11), new IndexRange(17, 20) }, ranges);
            Assert.Equal(new[] { new IndexRange(12, 16) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Multiple_Ranges()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10), new IndexRange(12, 14), new IndexRange(16, 18) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(6, 15), deselected);

            Assert.Equal(6, result);
            Assert.Equal(new[] { new IndexRange(16, 18) }, ranges);
            Assert.Equal(new[] { new IndexRange(8, 10), new IndexRange(12, 14) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Multiple_And_Partial_Ranges_1()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10), new IndexRange(12, 14), new IndexRange(16, 18) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(9, 15), deselected);

            Assert.Equal(5, result);
            Assert.Equal(new[] { new IndexRange(8, 8), new IndexRange(16, 18) }, ranges);
            Assert.Equal(new[] { new IndexRange(9, 10), new IndexRange(12, 14) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Multiple_And_Partial_Ranges_2()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10), new IndexRange(12, 14), new IndexRange(16, 18) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(8, 13), deselected);

            Assert.Equal(5, result);
            Assert.Equal(new[] { new IndexRange(14, 14), new IndexRange(16, 18) }, ranges);
            Assert.Equal(new[] { new IndexRange(8, 10), new IndexRange(12, 13) }, deselected);
        }

        [Fact]
        public void Remove_Should_Remove_Multiple_And_Partial_Ranges_3()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10), new IndexRange(12, 14), new IndexRange(16, 18) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(9, 13), deselected);

            Assert.Equal(4, result);
            Assert.Equal(new[] { new IndexRange(8, 8), new IndexRange(14, 14), new IndexRange(16, 18) }, ranges);
            Assert.Equal(new[] { new IndexRange(9, 10), new IndexRange(12, 13) }, deselected);
        }

        [Fact]
        public void Remove_Should_Do_Nothing_For_Unselected_Range()
        {
            var ranges = new List<IndexRange> { new IndexRange(8, 10) };
            var deselected = new List<IndexRange>();
            var result = IndexRange.Remove(ranges, new IndexRange(2, 4), deselected);

            Assert.Equal(0, result);
            Assert.Equal(new[] { new IndexRange(8, 10) }, ranges);
            Assert.Empty(deselected);
        }

        [Fact]
        public void Stress_Test()
        {
            const int iterations = 100;
            var random = new Random(0);
            var selection = new List<IndexRange>();
            var expected = new List<int>();

            IndexRange Generate()
            {
                var start = random.Next(100);
                return new IndexRange(start, start + random.Next(20));
            }

            for (var i = 0; i < iterations; ++i)
            {
                var toAdd = random.Next(5);

                for (var j = 0; j < toAdd; ++j)
                {
                    var range = Generate();
                    IndexRange.Add(selection, range);

                    for (var k = range.Begin; k <= range.End; ++k)
                    {
                        if (!expected.Contains(k))
                        {
                            expected.Add(k);
                        }
                    }

                    var actual = IndexRange.EnumerateIndices(selection).ToList();
                    expected.Sort();
                    Assert.Equal(expected, actual);
                }

                var toRemove = random.Next(5);

                for (var j = 0; j < toRemove; ++j)
                {
                    var range = Generate();
                    IndexRange.Remove(selection, range);

                    for (var k = range.Begin; k <= range.End; ++k)
                    {
                        expected.Remove(k);
                    }

                    var actual = IndexRange.EnumerateIndices(selection).ToList();
                    Assert.Equal(expected, actual);
                }

                selection.Clear();
                expected.Clear();
            }
        }
    }
}
