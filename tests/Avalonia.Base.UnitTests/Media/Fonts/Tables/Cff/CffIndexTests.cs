using System;
using Avalonia.Media.Fonts.Tables.Cff;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// INDEX offsets are 1-based, relative to the byte before the object data: the first offset is
    /// always 1 and no offset can be 0. Hand-built blobs with offSize 1.
    /// </summary>
    public class CffIndexTests
    {
        [Fact]
        public void Well_Formed_Index_Reads_Its_Entries()
        {
            // count 2, offSize 1, offsets 1 3 4, data "ab" "c"
            byte[] blob = { 0, 2, 1, 1, 3, 4, (byte)'a', (byte)'b', (byte)'c' };

            var index = CffIndex.Read(blob, 0);

            Assert.Equal(2, index.Count);
            Assert.Equal(new[] { (byte)'a', (byte)'b' }, index[0].ToArray());
            Assert.Equal(new[] { (byte)'c' }, index[1].ToArray());
            Assert.Equal(blob.Length, index.EndOffset);
        }

        [Fact]
        public void First_Offset_Other_Than_One_Is_Rejected()
        {
            // count 1, offSize 1, offsets 2 3: the entry would start one byte into the data.
            byte[] blob = { 0, 1, 1, 2, 3, (byte)'a', (byte)'b' };

            Assert.Throws<ArgumentException>(() => CffIndex.Read(blob, 0));
        }

        [Fact]
        public void Zero_Offset_Is_Rejected()
        {
            // count 2, offSize 1, offsets 1 0 3: entry 1 would start on the last offset byte.
            byte[] blob = { 0, 2, 1, 1, 0, 3, (byte)'a', (byte)'b' };

            var index = CffIndex.Read(blob, 0);

            Assert.Throws<ArgumentException>(() => index[1]);
        }
    }
}
