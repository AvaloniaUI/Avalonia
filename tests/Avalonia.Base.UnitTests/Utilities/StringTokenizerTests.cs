using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class StringTokenizerTests
    {
        [Fact]
        public void ReadInt32_Reads_Values()
        {
            var target = new StringTokenizer("123,456");

            Assert.Equal(123, target.ReadInt32());
            Assert.Equal(456, target.ReadInt32());
            Assert.Throws<FormatException>(() => target.ReadInt32());
        }

        [Fact]
        public void ReadDouble_Reads_Values()
        {
            var target = new StringTokenizer("12.3,45.6");

            Assert.Equal(12.3, target.ReadDouble());
            Assert.Equal(45.6, target.ReadDouble());
            Assert.Throws<FormatException>(() => target.ReadDouble());
        }

        [Fact]
        public void TryReadInt32_Reads_Values()
        {
            var target = new StringTokenizer("123,456");

            Assert.True(target.TryReadInt32(out var value));
            Assert.Equal(123, value);
            Assert.True(target.TryReadInt32(out value));
            Assert.Equal(456, value);
            Assert.False(target.TryReadInt32(out value));
        }

        [Fact]
        public void TryReadInt32_Doesnt_Throw()
        {
            var target = new StringTokenizer("abc");

            Assert.False(target.TryReadInt32(out var value));
        }

        [Fact]
        public void TryReadDouble_Reads_Values()
        {
            var target = new StringTokenizer("12.3,45.6");

            Assert.True(target.TryReadDouble(out var value));
            Assert.Equal(12.3, value);
            Assert.True(target.TryReadDouble(out value));
            Assert.Equal(45.6, value);
            Assert.False(target.TryReadDouble(out value));
        }

        [Fact]
        public void TryReadDouble_Doesnt_Throw()
        {
            var target = new StringTokenizer("abc");

            Assert.False(target.TryReadDouble(out var value));
        }
    }
}
