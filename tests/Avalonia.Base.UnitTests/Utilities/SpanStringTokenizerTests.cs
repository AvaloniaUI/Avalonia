using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class SpanStringTokenizerTests
    {
        // Explicit delegate because C# generics do not allow ref structs.
        private delegate void TokenizerAction(SpanStringTokenizer tokenizer);

        private static TException AssertThrows<TException>(SpanStringTokenizer tokenizer, TokenizerAction action)
            where TException : Exception
        {
            try
            {
                action(tokenizer);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(TException))
                    return (TException)ex;

                throw Xunit.Sdk.ThrowsException.ForIncorrectExceptionType(typeof(TException), ex);
            }

            throw Xunit.Sdk.ThrowsException.ForNoException(typeof(TException));
        }

        [Fact]
        public void ReadInt32_Reads_Values()
        {
            var target = new SpanStringTokenizer("123,456");

            Assert.Equal(123, target.ReadInt32());
            Assert.Equal(456, target.ReadInt32());
            AssertThrows<FormatException>(target, t => t.ReadInt32());
        }

        [Fact]
        public void ReadDouble_Reads_Values()
        {
            var target = new SpanStringTokenizer("12.3,45.6");

            Assert.Equal(12.3, target.ReadDouble());
            Assert.Equal(45.6, target.ReadDouble());
            AssertThrows<FormatException>(target, t => t.ReadDouble());
        }

        [Fact]
        public void TryReadInt32_Reads_Values()
        {
            var target = new SpanStringTokenizer("123,456");

            Assert.True(target.TryReadInt32(out var value));
            Assert.Equal(123, value);
            Assert.True(target.TryReadInt32(out value));
            Assert.Equal(456, value);
            Assert.False(target.TryReadInt32(out value));
        }

        [Fact]
        public void TryReadInt32_Doesnt_Throw()
        {
            var target = new SpanStringTokenizer("abc");

            Assert.False(target.TryReadInt32(out var value));
        }

        [Fact]
        public void TryReadDouble_Reads_Values()
        {
            var target = new SpanStringTokenizer("12.3,45.6");

            Assert.True(target.TryReadDouble(out var value));
            Assert.Equal(12.3, value);
            Assert.True(target.TryReadDouble(out value));
            Assert.Equal(45.6, value);
            Assert.False(target.TryReadDouble(out value));
        }

        [Fact]
        public void TryReadDouble_Doesnt_Throw()
        {
            var target = new SpanStringTokenizer("abc");

            Assert.False(target.TryReadDouble(out var value));
        }

        [Fact]
        public void ReadSpan_And_ReadString_Reads_Same()
        {
            var target1 = new SpanStringTokenizer("abc,def");
            var target2 = new SpanStringTokenizer("abc,def");

            Assert.Equal(target1.ReadString(), target2.ReadSpan().ToString());
            Assert.True(target1.ReadSpan().SequenceEqual(target2.ReadString()));
        }
    }
}
