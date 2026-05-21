using System;
using Avalonia.Media.Fonts.Tables.Colr;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables.Colr
{
    public class DeltaSetIndexMapTests
    {
        [Fact]
        public void Load_Format0_WithValidData_ShouldSucceed()
        {
            // DeltaSetIndexMap Format 0:
            // uint8 format = 0
            // uint8 entryFormat = 0x00 (1 byte inner, 1 byte outer)
            // uint16 mapCount = 3
            // Map data: [outer0, inner0], [outer1, inner1], [outer2, inner2]
            var data = new byte[]
            {
                0, // format = 0
                0x00, // entryFormat = 0x00 (1 byte each)
                0, 3, // mapCount = 3 (uint16 big-endian)
                // Map entries (outer, inner):
                0, 0, // entry 0: outer=0, inner=0
                0, 1, // entry 1: outer=0, inner=1
                1, 0  // entry 2: outer=1, inner=0
            };

            var map = DeltaSetIndexMap.Load(data, 0);

            Assert.NotNull(map);
            Assert.Equal(0, map!.Format);
            Assert.Equal(3u, map.MapCount);
        }

        [Fact]
        public void Load_Format1_WithValidData_ShouldSucceed()
        {
            // DeltaSetIndexMap Format 1:
            // uint8 format = 1
            // uint8 entryFormat = 0x11 (2 bytes inner, 2 bytes outer)
            // uint32 mapCount = 2
            var data = new byte[]
            {
                1, // format = 1
                0x11, // entryFormat = 0x11 (2 bytes each)
                0, 0, 0, 2, // mapCount = 2 (uint32 big-endian)
                // Map entries (outer, inner):
                0, 5, 0, 10, // entry 0: outer=5, inner=10
                0, 6, 0, 20  // entry 1: outer=6, inner=20
            };

            var map = DeltaSetIndexMap.Load(data, 0);

            Assert.NotNull(map);
            Assert.Equal(1, map!.Format);
            Assert.Equal(2u, map.MapCount);
        }

        [Fact]
        public void TryGetDeltaSetIndex_WithFormat0_ShouldReturnCorrectIndices()
        {
            var data = new byte[]
            {
                0, // format = 0
                0x00, // entryFormat = 0x00
                0, 3, // mapCount = 3
                0, 0, // entry 0: outer=0, inner=0
                0, 1, // entry 1: outer=0, inner=1
                1, 0  // entry 2: outer=1, inner=0
            };

            var map = DeltaSetIndexMap.Load(data, 0);
            Assert.NotNull(map);

            // Test entry 0
            Assert.True(map!.TryGetDeltaSetIndex(0, out var outer0, out var inner0));
            Assert.Equal(0, outer0);
            Assert.Equal(0, inner0);

            // Test entry 1
            Assert.True(map.TryGetDeltaSetIndex(1, out var outer1, out var inner1));
            Assert.Equal(0, outer1);
            Assert.Equal(1, inner1);

            // Test entry 2
            Assert.True(map.TryGetDeltaSetIndex(2, out var outer2, out var inner2));
            Assert.Equal(1, outer2);
            Assert.Equal(0, inner2);
        }

        [Fact]
        public void TryGetDeltaSetIndex_WithFormat1And2ByteEntries_ShouldReturnCorrectIndices()
        {
            var data = new byte[]
            {
                1, // format = 1
                0x11, // entryFormat = 0x11 (2 bytes each)
                0, 0, 0, 2, // mapCount = 2
                0, 5, 0, 10, // entry 0: outer=5, inner=10
                0, 6, 0, 20  // entry 1: outer=6, inner=20
            };

            var map = DeltaSetIndexMap.Load(data, 0);
            Assert.NotNull(map);

            // Test entry 0
            Assert.True(map!.TryGetDeltaSetIndex(0, out var outer0, out var inner0));
            Assert.Equal(5, outer0);
            Assert.Equal(10, inner0);

            // Test entry 1
            Assert.True(map.TryGetDeltaSetIndex(1, out var outer1, out var inner1));
            Assert.Equal(6, outer1);
            Assert.Equal(20, inner1);
        }

        [Fact]
        public void TryGetDeltaSetIndex_WithOutOfRangeIndex_ShouldReturnFalse()
        {
            var data = new byte[]
            {
                0, // format = 0
                0x00, // entryFormat = 0x00
                0, 2, // mapCount = 2
                0, 0,
                0, 1
            };

            var map = DeltaSetIndexMap.Load(data, 0);
            Assert.NotNull(map);

            // Try to access index 2, which is out of range (map has only 2 entries: 0 and 1)
            Assert.False(map!.TryGetDeltaSetIndex(2, out _, out _));
        }

        [Fact]
        public void Load_WithInvalidFormat_ShouldReturnNull()
        {
            var data = new byte[]
            {
                2, // format = 2 (invalid)
                0x00,
                0, 1
            };

            var map = DeltaSetIndexMap.Load(data, 0);

            Assert.Null(map);
        }

        [Fact]
        public void Load_WithInsufficientData_ShouldReturnNull()
        {
            var data = new byte[] { 0 }; // Only format byte, missing entryFormat and mapCount

            var map = DeltaSetIndexMap.Load(data, 0);

            Assert.Null(map);
        }

        [Fact]
        public void TryGetDeltaSetIndex_WithMixedEntryFormat_ShouldReturnCorrectIndices()
        {
            // entryFormat = 0x10 means 1-byte inner, 2-byte outer (3 bytes per entry)
            var data = new byte[]
            {
                0, // format = 0
                0x10, // entryFormat = 0x10
                0, 2, // mapCount = 2
                0, 5, 10, // entry 0: outer=5 (2 bytes), inner=10 (1 byte)
                0, 6, 20  // entry 1: outer=6 (2 bytes), inner=20 (1 byte)
            };

            var map = DeltaSetIndexMap.Load(data, 0);
            Assert.NotNull(map);

            // Test entry 0
            Assert.True(map!.TryGetDeltaSetIndex(0, out var outer0, out var inner0));
            Assert.Equal(5, outer0);
            Assert.Equal(10, inner0);

            // Test entry 1
            Assert.True(map.TryGetDeltaSetIndex(1, out var outer1, out var inner1));
            Assert.Equal(6, outer1);
            Assert.Equal(20, inner1);
        }

        [Fact]
        public void Load_ShouldReturnSameInstance_WhenCalledMultipleTimes()
        {
            // This test verifies that the same byte array always produces consistent results
            // The DeltaSetIndexMap.Load method should be deterministic
            var data = new byte[]
            {
                0, // format = 0
                0x00, // entryFormat = 0x00
                0, 2, // mapCount = 2
                0, 0,
                0, 1
            };

            var map1 = DeltaSetIndexMap.Load(data, 0);
            var map2 = DeltaSetIndexMap.Load(data, 0);

            Assert.NotNull(map1);
            Assert.NotNull(map2);
            
            // Verify both instances return the same data
            Assert.True(map1!.TryGetDeltaSetIndex(0, out var outer1, out var inner1));
            Assert.True(map2!.TryGetDeltaSetIndex(0, out var outer2, out var inner2));
            
            Assert.Equal(outer1, outer2);
            Assert.Equal(inner1, inner2);
        }

        [Fact]
        public void TryGetDeltaSetIndex_WithLargeIndices_ShouldHandleCorrectly()
        {
            // Test with larger outer/inner indices to verify 2-byte reading
            var data = new byte[]
            {
                0, // format = 0
                0x11, // entryFormat = 0x11 (2 bytes each)
                0, 2, // mapCount = 2
                0x01, 0x00, 0x02, 0x00, // entry 0: outer=256, inner=512
                0xFF, 0xFF, 0xFF, 0xFF  // entry 1: outer=65535, inner=65535
            };

            var map = DeltaSetIndexMap.Load(data, 0);
            Assert.NotNull(map);

            // Test entry 0
            Assert.True(map!.TryGetDeltaSetIndex(0, out var outer0, out var inner0));
            Assert.Equal(256, outer0);
            Assert.Equal(512, inner0);

            // Test entry 1
            Assert.True(map.TryGetDeltaSetIndex(1, out var outer1, out var inner1));
            Assert.Equal(65535, outer1);
            Assert.Equal(65535, inner1);
        }
    }
}
